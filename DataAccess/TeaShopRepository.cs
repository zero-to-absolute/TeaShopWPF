using System.Data;
using Microsoft.Data.SqlClient;
using TeaShopManager.Models;

namespace TeaShopManager.DataAccess;

public sealed partial class TeaShopRepository
{
    private const string LowStockNotificationType = "Минимальный остаток";
    private const string CustomerReturnSupplierName = "Возврат покупателя";
    private const string TeaCategoryName = "Чай";
    private readonly object _schemaSyncRoot = new();
    private bool _lossQuantityColumnChecked;
    private const string ExpirationNotificationType = "Срок годности";

    private const string AcceptedSupplyStatusCondition = """
                                                         ss.NAME LIKE N'Принят%'
                                                         OR ss.NAME LIKE N'Достав%'
                                                         OR ss.NAME LIKE N'Получ%'
                                                         OR ss.NAME LIKE N'Привез%'
                                                         OR ss.NAME LIKE N'Заверш%'
                                                         OR ss.NAME LIKE N'Закрыт%'
                                                         """;

    private const string CountedOrderStatusCondition = """
                                                        so.NAME LIKE N'Оплач%'
                                                        OR so.NAME LIKE N'Заверш%'
                                                        OR so.NAME LIKE N'Выдан%'
                                                        OR so.NAME LIKE N'Закрыт%'
                                                        """;

    private readonly string _connectionString;
    private readonly ApplicationSettingsStore _settingsStore;

    public TeaShopRepository(string connectionString, ApplicationSettingsStore? settingsStore = null)
    {
        _connectionString = connectionString;
        _settingsStore = settingsStore ?? new ApplicationSettingsStore();
    }

    private sealed record NotificationSnapshot(bool IsRead, DateTime CreatedAt, string Message);

    private sealed class ExpiringProductNotificationItem
    {
        public int ProductId { get; init; }

        public string ProductName { get; init; } = string.Empty;

        public int ExpiredQuantity { get; set; }

        public int ExpiringSoonQuantity { get; set; }

        public DateTime? NearestExpirationDate { get; set; }
    }

    public ApplicationSettingsModel GetApplicationSettings()
    {
        return NormalizeApplicationSettings(_settingsStore.Load());
    }

    public void SaveApplicationSettings(ApplicationSettingsModel settings)
    {
        _settingsStore.Save(NormalizeApplicationSettings(settings));
    }

    public DatabaseConnectionInfo GetDatabaseConnectionInfo()
    {
        var builder = new SqlConnectionStringBuilder(_connectionString);
        var authenticationMode = builder.IntegratedSecurity || string.Equals(builder["Trusted_Connection"]?.ToString(), "True", StringComparison.OrdinalIgnoreCase)
            ? "Windows Authentication"
            : "SQL Server Authentication";

        return new DatabaseConnectionInfo
        {
            ServerName = builder.DataSource,
            DatabaseName = builder.InitialCatalog,
            AuthenticationMode = authenticationMode
        };
    }

    public void RefreshSystemNotifications()
    {
        using var connection = OpenConnection();
        EnsureLossQuantityColumn(connection);
        SyncLowStockNotifications(connection);
    }

    private IReadOnlyList<StatusPreviewItem> GetStatuses(string tableName)
    {
        var sql = $"""
                    SELECT ID_STATUS, NAME
                    FROM {tableName}
                    ORDER BY ID_STATUS;
                    """;

        var result = new List<StatusPreviewItem>();
        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var statusId = Convert.ToInt32(reader["ID_STATUS"]);
            result.Add(new StatusPreviewItem
            {
                StatusId = statusId,
                StatusCode = statusId.ToString(),
                StatusName = reader["NAME"]?.ToString() ?? string.Empty
            });
        }

        return result;
    }

    private int GetCompletedSaleStatusId(SqlConnection connection, SqlTransaction transaction)
    {
        const string sql = """
                           SELECT TOP (1) ID_STATUS
                           FROM STATUS_ORDER
                           WHERE NAME LIKE N'Оплач%'
                              OR NAME LIKE N'Заверш%'
                              OR NAME LIKE N'Выдан%'
                              OR NAME LIKE N'Закрыт%'
                           ORDER BY CASE
                               WHEN NAME LIKE N'Оплач%' THEN 0
                               WHEN NAME LIKE N'Заверш%' THEN 1
                               WHEN NAME LIKE N'Выдан%' THEN 2
                               ELSE 3
                           END,
                           ID_STATUS;
                           """;

        using var command = new SqlCommand(sql, connection, transaction);
        var result = command.ExecuteScalar();

        if (result is null)
        {
            throw new InvalidOperationException("В STATUS_ORDER не найден статус для завершённой продажи. Нужен статус вроде \"Оплачен\" или \"Завершён\".");
        }

        return Convert.ToInt32(result);
    }

    private int GetAcceptedSupplyStatusId(SqlConnection connection, SqlTransaction transaction)
    {
        const string sql = """
                           SELECT TOP (1) ID_STATUS
                           FROM STATUS_SUPPLIES
                           WHERE NAME LIKE N'Принят%'
                              OR NAME LIKE N'Достав%'
                              OR NAME LIKE N'Получ%'
                              OR NAME LIKE N'Привез%'
                              OR NAME LIKE N'Заверш%'
                              OR NAME LIKE N'Закрыт%'
                           ORDER BY CASE
                               WHEN NAME LIKE N'Принят%' THEN 0
                               WHEN NAME LIKE N'Достав%' THEN 1
                               WHEN NAME LIKE N'Получ%' THEN 2
                               WHEN NAME LIKE N'Привез%' THEN 3
                               WHEN NAME LIKE N'Заверш%' THEN 4
                               ELSE 5
                           END,
                           ID_STATUS;
                           """;

        using var command = new SqlCommand(sql, connection, transaction);
        var result = command.ExecuteScalar();
        if (result is not null)
        {
            return Convert.ToInt32(result);
        }

        const string insertSql = """
                                 INSERT INTO STATUS_SUPPLIES (NAME)
                                 OUTPUT INSERTED.ID_STATUS
                                 VALUES (N'Принята');
                                 """;

        using var insertCommand = new SqlCommand(insertSql, connection, transaction);
        return Convert.ToInt32(insertCommand.ExecuteScalar());
    }

    private int GetAvailableStockQuantity(SqlConnection connection, SqlTransaction transaction, int productId)
    {
        EnsureLossQuantityColumn(connection, transaction);
        var sql = $"""
                    SELECT
                        COALESCE(sd.TOTAL_SUPPLIED, 0) - COALESCE(od.TOTAL_SOLD, 0) - COALESCE(ls.TOTAL_LOST, 0) AS STOCK_QUANTITY
                    FROM PRODUCTS p
                    OUTER APPLY (
                        SELECT SUM(sd.QUANTITY) AS TOTAL_SUPPLIED
                        FROM SUPPLIES_DETAILS sd
                        INNER JOIN SUPPLIES s ON s.ID_SUPPLIES = sd.ID_SUPPLIES
                        INNER JOIN STATUS_SUPPLIES ss ON ss.ID_STATUS = s.ID_STATUS
                        WHERE sd.ID_PRODUCT = p.ID_PRODUCT
                          AND ({AcceptedSupplyStatusCondition})
                    ) sd
                    OUTER APPLY (
                        SELECT SUM(od.QUANTITY) AS TOTAL_SOLD
                        FROM ORDER_DETAILS od
                        INNER JOIN ORDERS o ON o.ID_ORDER = od.ID_ORDER
                        INNER JOIN STATUS_ORDER so ON so.ID_STATUS = o.ID_STATUS
                        WHERE od.ID_PRODUCT = p.ID_PRODUCT
                          AND ({CountedOrderStatusCondition})
                    ) od
                    OUTER APPLY (
                        SELECT SUM(l.QUANTITY) AS TOTAL_LOST
                        FROM LOSSES l
                        WHERE l.ID_PRODUCT = p.ID_PRODUCT
                    ) ls
                    WHERE p.ID_PRODUCT = @productId;
                    """;

        using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@productId", SqlDbType.Int).Value = productId;

        var result = command.ExecuteScalar();
        if (result is null || result == DBNull.Value)
        {
            throw new InvalidOperationException($"Товар с идентификатором {productId} не найден.");
        }

        return Convert.ToInt32(result);
    }

    private int GetCountedSoldQuantity(SqlConnection connection, SqlTransaction? transaction, int productId)
    {
        var sql = $"""
                    SELECT COALESCE(SUM(od.QUANTITY), 0)
                    FROM ORDER_DETAILS od
                    INNER JOIN ORDERS o ON o.ID_ORDER = od.ID_ORDER
                    INNER JOIN STATUS_ORDER so ON so.ID_STATUS = o.ID_STATUS
                    WHERE od.ID_PRODUCT = @productId
                      AND ({CountedOrderStatusCondition});
                    """;

        using var command = transaction is null
            ? new SqlCommand(sql, connection)
            : new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@productId", SqlDbType.Int).Value = productId;
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private int GetCountedLossQuantity(SqlConnection connection, SqlTransaction? transaction, int productId)
    {
        EnsureLossQuantityColumn(connection, transaction);
        const string sql = """
                           SELECT COALESCE(SUM(l.QUANTITY), 0)
                           FROM LOSSES l
                           WHERE l.ID_PRODUCT = @productId;
                           """;

        using var command = transaction is null
            ? new SqlCommand(sql, connection)
            : new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@productId", SqlDbType.Int).Value = productId;
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private void SyncLowStockNotifications(SqlConnection connection, SqlTransaction? transaction = null)
    {
        var recipients = GetInventoryNotificationRecipients(connection, transaction);
        if (recipients.Count == 0)
        {
            return;
        }

        var criticalProducts = GetCriticalProducts(connection, transaction);
        var existing = GetExistingNotificationSnapshots(connection, transaction, LowStockNotificationType);

        foreach (var userId in recipients)
        {
            using var deleteCommand = transaction is null
                ? new SqlCommand("DELETE FROM NOTIFICATIONS WHERE NOTIFICATION_TYPE = @type AND ID_USER = @userId;", connection)
                : new SqlCommand("DELETE FROM NOTIFICATIONS WHERE NOTIFICATION_TYPE = @type AND ID_USER = @userId;", connection, transaction);
            deleteCommand.Parameters.Add("@type", SqlDbType.VarChar, 50).Value = LowStockNotificationType;
            deleteCommand.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
            deleteCommand.ExecuteNonQuery();

            foreach (var product in criticalProducts)
            {
                var key = (userId, product.ProductId);
                var hasExisting = existing.TryGetValue(key, out var snapshot);
                var createdAt = hasExisting && snapshot is not null ? snapshot.CreatedAt : DateTime.Now;
                var isRead = hasExisting && snapshot is not null && snapshot.IsRead;

                using var insertCommand = transaction is null
                    ? new SqlCommand("""
                                     INSERT INTO NOTIFICATIONS (NOTIFICATION_TYPE, MESSAGE, CREATED_DATE, ID_PRODUCT, ID_USER, IS_READ)
                                     VALUES (@type, @message, @createdDate, @productId, @userId, @isRead);
                                     """, connection)
                    : new SqlCommand("""
                                     INSERT INTO NOTIFICATIONS (NOTIFICATION_TYPE, MESSAGE, CREATED_DATE, ID_PRODUCT, ID_USER, IS_READ)
                                     VALUES (@type, @message, @createdDate, @productId, @userId, @isRead);
                                     """, connection, transaction);

                insertCommand.Parameters.Add("@type", SqlDbType.VarChar, 50).Value = LowStockNotificationType;
                insertCommand.Parameters.Add("@message", SqlDbType.VarChar, -1).Value =
                    $"Товар \"{product.ProductName}\" ниже минимального остатка. Остаток: {product.StockQuantity}, минимум: {product.MinStockQuantity}.";
                insertCommand.Parameters.Add("@createdDate", SqlDbType.DateTime).Value = createdAt;
                insertCommand.Parameters.Add("@productId", SqlDbType.Int).Value = product.ProductId;
                insertCommand.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                insertCommand.Parameters.Add("@isRead", SqlDbType.Bit).Value = isRead;
                insertCommand.ExecuteNonQuery();
            }
        }

        SyncExpirationNotifications(connection, transaction, recipients);
    }

    private void SyncExpirationNotifications(SqlConnection connection, SqlTransaction? transaction, IReadOnlyList<int> recipients)
    {
        var warningDays = GetCurrentApplicationSettings().ExpirationWarningDays;
        var expiringProducts = GetExpiringProducts(connection, transaction, warningDays);
        var existing = GetExistingNotificationSnapshots(connection, transaction, ExpirationNotificationType);

        foreach (var userId in recipients)
        {
            using var deleteCommand = transaction is null
                ? new SqlCommand("DELETE FROM NOTIFICATIONS WHERE NOTIFICATION_TYPE = @type AND ID_USER = @userId;", connection)
                : new SqlCommand("DELETE FROM NOTIFICATIONS WHERE NOTIFICATION_TYPE = @type AND ID_USER = @userId;", connection, transaction);
            deleteCommand.Parameters.Add("@type", SqlDbType.VarChar, 50).Value = ExpirationNotificationType;
            deleteCommand.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
            deleteCommand.ExecuteNonQuery();

            foreach (var product in expiringProducts)
            {
                var message = BuildExpirationNotificationMessage(product, warningDays);
                var key = (userId, product.ProductId);
                var hasExisting = existing.TryGetValue(key, out var snapshot);
                var createdAt = hasExisting && snapshot is not null ? snapshot.CreatedAt : DateTime.Now;
                var isRead = hasExisting
                             && snapshot is not null
                             && snapshot.IsRead
                             && string.Equals(snapshot.Message, message, StringComparison.Ordinal);

                using var insertCommand = transaction is null
                    ? new SqlCommand("""
                                     INSERT INTO NOTIFICATIONS (NOTIFICATION_TYPE, MESSAGE, CREATED_DATE, ID_PRODUCT, ID_USER, IS_READ)
                                     VALUES (@type, @message, @createdDate, @productId, @userId, @isRead);
                                     """, connection)
                    : new SqlCommand("""
                                     INSERT INTO NOTIFICATIONS (NOTIFICATION_TYPE, MESSAGE, CREATED_DATE, ID_PRODUCT, ID_USER, IS_READ)
                                     VALUES (@type, @message, @createdDate, @productId, @userId, @isRead);
                                     """, connection, transaction);

                insertCommand.Parameters.Add("@type", SqlDbType.VarChar, 50).Value = ExpirationNotificationType;
                insertCommand.Parameters.Add("@message", SqlDbType.VarChar, -1).Value = message;
                insertCommand.Parameters.Add("@createdDate", SqlDbType.DateTime).Value = createdAt;
                insertCommand.Parameters.Add("@productId", SqlDbType.Int).Value = product.ProductId;
                insertCommand.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                insertCommand.Parameters.Add("@isRead", SqlDbType.Bit).Value = isRead;
                insertCommand.ExecuteNonQuery();
            }
        }
    }

    private Dictionary<(int UserId, int ProductId), NotificationSnapshot> GetExistingNotificationSnapshots(
        SqlConnection connection,
        SqlTransaction? transaction,
        string notificationType)
    {
        const string sql = """
                           SELECT ID_USER, ID_PRODUCT, IS_READ, CREATED_DATE, MESSAGE
                           FROM NOTIFICATIONS
                           WHERE NOTIFICATION_TYPE = @type;
                           """;

        using var command = transaction is null
            ? new SqlCommand(sql, connection)
            : new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@type", SqlDbType.VarChar, 50).Value = notificationType;

        using var reader = command.ExecuteReader();
        var result = new Dictionary<(int UserId, int ProductId), NotificationSnapshot>();
        while (reader.Read())
        {
            result[(Convert.ToInt32(reader["ID_USER"]), Convert.ToInt32(reader["ID_PRODUCT"]))] =
                new NotificationSnapshot(
                    Convert.ToBoolean(reader["IS_READ"]),
                    Convert.ToDateTime(reader["CREATED_DATE"]),
                    reader["MESSAGE"]?.ToString() ?? string.Empty);
        }

        return result;
    }

    private IReadOnlyList<ExpiringProductNotificationItem> GetExpiringProducts(SqlConnection connection, SqlTransaction? transaction, int warningDays)
    {
        warningDays = Math.Clamp(warningDays, 1, 90);

        var incomingSql = $"""
                           SELECT
                               sd.ID_PRODUCT,
                               p.PRODUCT_NAME,
                               p.EXPIRATION_DATE,
                               sd.PRODUCTION_DATE,
                               sd.QUANTITY,
                               COALESCE(s.DELIVERY_DATE, s.CREATION_DATE) AS RECEIVED_DATE,
                               sd.ID_SUPPLIES
                           FROM SUPPLIES_DETAILS sd
                           INNER JOIN SUPPLIES s ON s.ID_SUPPLIES = sd.ID_SUPPLIES
                           INNER JOIN STATUS_SUPPLIES ss ON ss.ID_STATUS = s.ID_STATUS
                           INNER JOIN PRODUCTS p ON p.ID_PRODUCT = sd.ID_PRODUCT
                           WHERE ({AcceptedSupplyStatusCondition})
                           ORDER BY sd.ID_PRODUCT, COALESCE(s.DELIVERY_DATE, s.CREATION_DATE), sd.ID_SUPPLIES, sd.PRODUCTION_DATE;
                           """;

        var incomingBatches = new List<(int ProductId, string ProductName, int ExpirationDays, DateTime ProductionDate, int Quantity)>();
        using (var command = transaction is null
                   ? new SqlCommand(incomingSql, connection)
                   : new SqlCommand(incomingSql, connection, transaction))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                incomingBatches.Add((
                    Convert.ToInt32(reader["ID_PRODUCT"]),
                    reader["PRODUCT_NAME"]?.ToString() ?? string.Empty,
                    Convert.ToInt32(reader["EXPIRATION_DATE"]),
                    Convert.ToDateTime(reader["PRODUCTION_DATE"]),
                    Convert.ToInt32(reader["QUANTITY"])));
            }
        }

        var remainingConsumed = GetCountedConsumptionByProduct(connection, transaction);
        var today = DateTime.Today;
        var maxExpirationDate = today.AddDays(warningDays);
        var aggregates = new Dictionary<int, ExpiringProductNotificationItem>();

        foreach (var batch in incomingBatches)
        {
            remainingConsumed.TryGetValue(batch.ProductId, out var consumedLeft);
            var consumedFromBatch = Math.Min(consumedLeft, batch.Quantity);
            remainingConsumed[batch.ProductId] = consumedLeft - consumedFromBatch;

            var remainingQuantity = batch.Quantity - consumedFromBatch;
            if (remainingQuantity <= 0)
            {
                continue;
            }

            var expirationDate = batch.ProductionDate.Date.AddDays(batch.ExpirationDays);
            if (expirationDate > maxExpirationDate)
            {
                continue;
            }

            if (!aggregates.TryGetValue(batch.ProductId, out var aggregate))
            {
                aggregate = new ExpiringProductNotificationItem
                {
                    ProductId = batch.ProductId,
                    ProductName = batch.ProductName
                };
                aggregates[batch.ProductId] = aggregate;
            }

            if (!aggregate.NearestExpirationDate.HasValue || expirationDate < aggregate.NearestExpirationDate.Value)
            {
                aggregate.NearestExpirationDate = expirationDate;
            }

            if (expirationDate < today)
            {
                aggregate.ExpiredQuantity += remainingQuantity;
            }
            else
            {
                aggregate.ExpiringSoonQuantity += remainingQuantity;
            }
        }

        return aggregates.Values
            .OrderBy(item => item.NearestExpirationDate ?? DateTime.MaxValue)
            .ThenBy(item => item.ProductName)
            .ToList();
    }

    private Dictionary<int, int> GetCountedConsumptionByProduct(SqlConnection connection, SqlTransaction? transaction)
    {
        EnsureLossQuantityColumn(connection, transaction);
        var sql = $"""
                    SELECT
                        p.ID_PRODUCT,
                        COALESCE(od.TOTAL_SOLD, 0) + COALESCE(ls.TOTAL_LOST, 0) AS TOTAL_CONSUMED
                    FROM PRODUCTS p
                    OUTER APPLY (
                        SELECT SUM(od.QUANTITY) AS TOTAL_SOLD
                        FROM ORDER_DETAILS od
                        INNER JOIN ORDERS o ON o.ID_ORDER = od.ID_ORDER
                        INNER JOIN STATUS_ORDER so ON so.ID_STATUS = o.ID_STATUS
                        WHERE od.ID_PRODUCT = p.ID_PRODUCT
                          AND ({CountedOrderStatusCondition})
                    ) od
                    OUTER APPLY (
                        SELECT SUM(l.QUANTITY) AS TOTAL_LOST
                        FROM LOSSES l
                        WHERE l.ID_PRODUCT = p.ID_PRODUCT
                    ) ls;
                    """;

        using var command = transaction is null
            ? new SqlCommand(sql, connection)
            : new SqlCommand(sql, connection, transaction);
        using var reader = command.ExecuteReader();

        var result = new Dictionary<int, int>();
        while (reader.Read())
        {
            result[Convert.ToInt32(reader["ID_PRODUCT"])] = Convert.ToInt32(reader["TOTAL_CONSUMED"]);
        }

        return result;
    }

    private static string BuildExpirationNotificationMessage(ExpiringProductNotificationItem product, int warningDays)
    {
        if (product.ExpiredQuantity > 0 && product.ExpiringSoonQuantity > 0)
        {
            return $"Товар \"{product.ProductName}\" требует внимания: просрочено {product.ExpiredQuantity} ед., ещё {product.ExpiringSoonQuantity} ед. истекает в ближайшие {warningDays} дн.";
        }

        if (product.ExpiredQuantity > 0)
        {
            return $"У товара \"{product.ProductName}\" срок годности уже истёк. Просроченный остаток: {product.ExpiredQuantity} ед.";
        }

        var nearestDate = product.NearestExpirationDate?.ToString("dd.MM.yyyy") ?? "в ближайшее время";
        return $"У товара \"{product.ProductName}\" срок годности истекает до {nearestDate}. Остаток партии: {product.ExpiringSoonQuantity} ед.";
    }

    private IReadOnlyList<int> GetInventoryNotificationRecipients(SqlConnection connection, SqlTransaction? transaction)
    {
        const string sql = """
                           SELECT u.ID_USER
                           FROM USERS u
                           INNER JOIN ROLE r ON r.ID_ROLE = u.ID_ROLE
                           WHERE r.ROLE_NAME LIKE N'%менедж%'
                              OR r.ROLE_NAME LIKE N'%администратор%';
                           """;

        using var command = transaction is null
            ? new SqlCommand(sql, connection)
            : new SqlCommand(sql, connection, transaction);
        using var reader = command.ExecuteReader();

        var result = new List<int>();
        while (reader.Read())
        {
            result.Add(Convert.ToInt32(reader["ID_USER"]));
        }

        return result;
    }

    private IReadOnlyList<(int ProductId, string ProductName, int StockQuantity, int MinStockQuantity)> GetCriticalProducts(SqlConnection connection, SqlTransaction? transaction)
    {
        EnsureLossQuantityColumn(connection, transaction);
        var sql = $"""
                    SELECT
                        p.ID_PRODUCT,
                        p.PRODUCT_NAME,
                        p.MIN_STOCK_QUANTITY,
                        COALESCE(sd.TOTAL_SUPPLIED, 0) - COALESCE(od.TOTAL_SOLD, 0) - COALESCE(ls.TOTAL_LOST, 0) AS STOCK_QUANTITY
                    FROM PRODUCTS p
                    OUTER APPLY (
                        SELECT SUM(sd.QUANTITY) AS TOTAL_SUPPLIED
                        FROM SUPPLIES_DETAILS sd
                        INNER JOIN SUPPLIES s ON s.ID_SUPPLIES = sd.ID_SUPPLIES
                        INNER JOIN STATUS_SUPPLIES ss ON ss.ID_STATUS = s.ID_STATUS
                        WHERE sd.ID_PRODUCT = p.ID_PRODUCT
                          AND ({AcceptedSupplyStatusCondition})
                    ) sd
                    OUTER APPLY (
                        SELECT SUM(od.QUANTITY) AS TOTAL_SOLD
                        FROM ORDER_DETAILS od
                        INNER JOIN ORDERS o ON o.ID_ORDER = od.ID_ORDER
                        INNER JOIN STATUS_ORDER so ON so.ID_STATUS = o.ID_STATUS
                        WHERE od.ID_PRODUCT = p.ID_PRODUCT
                          AND ({CountedOrderStatusCondition})
                    ) od
                    OUTER APPLY (
                        SELECT SUM(l.QUANTITY) AS TOTAL_LOST
                        FROM LOSSES l
                        WHERE l.ID_PRODUCT = p.ID_PRODUCT
                    ) ls
                    WHERE COALESCE(sd.TOTAL_SUPPLIED, 0) - COALESCE(od.TOTAL_SOLD, 0) - COALESCE(ls.TOTAL_LOST, 0) < p.MIN_STOCK_QUANTITY
                    ORDER BY p.PRODUCT_NAME;
                    """;

        using var command = transaction is null
            ? new SqlCommand(sql, connection)
            : new SqlCommand(sql, connection, transaction);
        using var reader = command.ExecuteReader();

        var result = new List<(int ProductId, string ProductName, int StockQuantity, int MinStockQuantity)>();
        while (reader.Read())
        {
            result.Add((
                Convert.ToInt32(reader["ID_PRODUCT"]),
                reader["PRODUCT_NAME"]?.ToString() ?? string.Empty,
                Convert.ToInt32(reader["STOCK_QUANTITY"]),
                Convert.ToInt32(reader["MIN_STOCK_QUANTITY"])));
        }

        return result;
    }

    private void EnsureSupplyHasDetails(SqlConnection connection, SqlTransaction? transaction, int supplyId)
    {
        const string sql = """
                           SELECT COUNT(*)
                           FROM SUPPLIES_DETAILS
                           WHERE ID_SUPPLIES = @supplyId;
                           """;

        using var command = transaction is null
            ? new SqlCommand(sql, connection)
            : new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@supplyId", SqlDbType.Int).Value = supplyId;

        if (Convert.ToInt32(command.ExecuteScalar()) == 0)
        {
            throw new InvalidOperationException("Нельзя принять поставку без позиций.");
        }
    }

    private (string ProductName, string CategoryName, decimal RetailPrice) GetProductSnapshot(SqlConnection connection, SqlTransaction? transaction, int productId)
    {
        const string sql = """
                           SELECT
                               p.PRODUCT_NAME,
                               c.CATEGORY_NAME,
                               p.PRICE
                           FROM PRODUCTS p
                           INNER JOIN CATEGORY c ON c.ID_CATEGORY = p.ID_CATEGORY
                           WHERE p.ID_PRODUCT = @productId;
                           """;

        using var command = transaction is null
            ? new SqlCommand(sql, connection)
            : new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@productId", SqlDbType.Int).Value = productId;
        using var reader = command.ExecuteReader();

        if (!reader.Read())
        {
            throw new InvalidOperationException($"Товар с идентификатором {productId} не найден.");
        }

        return (
            reader["PRODUCT_NAME"]?.ToString() ?? string.Empty,
            reader["CATEGORY_NAME"]?.ToString() ?? string.Empty,
            Convert.ToDecimal(reader["PRICE"]));
    }

    private int GetOrCreateCustomerReturnSupplierId(SqlConnection connection, SqlTransaction transaction)
    {
        const string selectSql = """
                                 SELECT TOP (1) ID_SUPPLIER
                                 FROM SUPPLIERS
                                 WHERE SUPPLIER_NAME = @name
                                 ORDER BY ID_SUPPLIER;
                                 """;

        using (var selectCommand = new SqlCommand(selectSql, connection, transaction))
        {
            selectCommand.Parameters.Add("@name", SqlDbType.VarChar, 4000).Value = CustomerReturnSupplierName;
            var existing = selectCommand.ExecuteScalar();
            if (existing is not null)
            {
                return Convert.ToInt32(existing);
            }
        }

        const string insertSql = """
                                 INSERT INTO SUPPLIERS (SUPPLIER_NAME, CONTACT_PERSON, PHONE, EMAIL, ADDRESS)
                                 OUTPUT INSERTED.ID_SUPPLIER
                                 VALUES (@name, @contactPerson, @phone, @email, @address);
                                 """;

        using var insertCommand = new SqlCommand(insertSql, connection, transaction);
        insertCommand.Parameters.Add("@name", SqlDbType.VarChar, 4000).Value = CustomerReturnSupplierName;
        insertCommand.Parameters.Add("@contactPerson", SqlDbType.VarChar, 4000).Value = "Покупатель";
        insertCommand.Parameters.Add("@phone", SqlDbType.VarChar, 20).Value = "-";
        insertCommand.Parameters.Add("@email", SqlDbType.VarChar, 100).Value = DBNull.Value;
        insertCommand.Parameters.Add("@address", SqlDbType.VarChar, 4000).Value = "Возврат через приложение";
        return Convert.ToInt32(insertCommand.ExecuteScalar());
    }

    private void EnsureSupplyIsEditable(SqlConnection connection, SqlTransaction? transaction, int supplyId)
    {
        if (IsSupplyAccepted(connection, transaction, supplyId))
        {
            throw new InvalidOperationException("Принятая поставка больше не редактируется. Для неё доступны только просмотр и учет в остатках.");
        }
    }

    private bool IsSupplyAccepted(SqlConnection connection, SqlTransaction? transaction, int supplyId)
    {
        const string sql = """
                           SELECT TOP (1) ss.NAME
                           FROM SUPPLIES s
                           INNER JOIN STATUS_SUPPLIES ss ON ss.ID_STATUS = s.ID_STATUS
                           WHERE s.ID_SUPPLIES = @supplyId;
                           """;

        using var command = transaction is null
            ? new SqlCommand(sql, connection)
            : new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@supplyId", SqlDbType.Int).Value = supplyId;
        var result = command.ExecuteScalar();
        if (result is null)
        {
            throw new InvalidOperationException($"Поставка {supplyId} не найдена.");
        }

        return IsAcceptedSupplyStatusName(result.ToString());
    }

    private bool IsSupplyAcceptedStatusId(SqlConnection connection, SqlTransaction? transaction, int statusId)
    {
        const string sql = """
                           SELECT TOP (1) NAME
                           FROM STATUS_SUPPLIES
                           WHERE ID_STATUS = @statusId;
                           """;

        using var command = transaction is null
            ? new SqlCommand(sql, connection)
            : new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@statusId", SqlDbType.Int).Value = statusId;
        var result = command.ExecuteScalar();
        if (result is null)
        {
            throw new InvalidOperationException($"Статус поставки {statusId} не найден.");
        }

        return IsAcceptedSupplyStatusName(result.ToString());
    }

    private static bool IsAcceptedSupplyStatusName(string? statusName)
    {
        if (string.IsNullOrWhiteSpace(statusName))
        {
            return false;
        }

        return statusName.StartsWith("Принят", StringComparison.CurrentCultureIgnoreCase)
               || statusName.StartsWith("Достав", StringComparison.CurrentCultureIgnoreCase)
               || statusName.StartsWith("Получ", StringComparison.CurrentCultureIgnoreCase)
               || statusName.StartsWith("Привез", StringComparison.CurrentCultureIgnoreCase)
               || statusName.StartsWith("Заверш", StringComparison.CurrentCultureIgnoreCase)
               || statusName.StartsWith("Закрыт", StringComparison.CurrentCultureIgnoreCase);
    }

    private static bool IsTeaCategoryName(string? categoryName)
    {
        return string.Equals(categoryName?.Trim(), TeaCategoryName, StringComparison.CurrentCultureIgnoreCase);
    }

    private void FillProductParameters(SqlCommand command, ProductEditorModel model)
    {
        command.Parameters.Add("@name", SqlDbType.VarChar, 4000).Value = model.ProductName.Trim();
        command.Parameters.Add("@price", SqlDbType.Decimal).Value = model.Price;
        command.Parameters["@price"].Precision = 8;
        command.Parameters["@price"].Scale = 2;
        command.Parameters.Add("@minStock", SqlDbType.Int).Value = model.MinStockQuantity;
        AddNullableString(command, "@description", model.Description, 4000);
        command.Parameters.Add("@expirationDays", SqlDbType.SmallInt).Value = model.ExpirationDays;
        command.Parameters.Add("@categoryId", SqlDbType.SmallInt).Value = model.CategoryId;
    }

    private void FillOrderDetailParameters(SqlCommand command, OrderDetailEditorModel model)
    {
        command.Parameters.Add("@productId", SqlDbType.Int).Value = model.ProductId;
        command.Parameters.Add("@orderId", SqlDbType.Int).Value = model.OrderId;
        command.Parameters.Add("@quantity", SqlDbType.Int).Value = model.Quantity;
        command.Parameters.Add("@price", SqlDbType.Decimal).Value = model.Price;
        command.Parameters["@price"].Precision = 8;
        command.Parameters["@price"].Scale = 2;
    }

    private void FillNotificationParameters(SqlCommand command, NotificationEditorModel model)
    {
        command.Parameters.Add("@type", SqlDbType.VarChar, 50).Value = model.NotificationType.Trim();
        command.Parameters.Add("@message", SqlDbType.VarChar, -1).Value = model.Message.Trim();
        command.Parameters.Add("@createdDate", SqlDbType.DateTime).Value = model.CreatedAt;
        command.Parameters.Add("@productId", SqlDbType.Int).Value = model.ProductId;
        command.Parameters.Add("@userId", SqlDbType.Int).Value = model.UserId;
        command.Parameters.Add("@isRead", SqlDbType.Bit).Value = model.IsRead;
    }

    private void FillSupplyParameters(SqlCommand command, SupplyEditorModel model)
    {
        command.Parameters.Add("@creationDate", SqlDbType.Date).Value = model.CreationDate.Date;
        var deliveryParameter = command.Parameters.Add("@deliveryDate", SqlDbType.Date);
        deliveryParameter.Value = model.DeliveryDate.HasValue ? model.DeliveryDate.Value.Date : DBNull.Value;
        command.Parameters.Add("@statusId", SqlDbType.Int).Value = model.StatusId;
        command.Parameters.Add("@supplierId", SqlDbType.Int).Value = model.SupplierId;
        command.Parameters.Add("@userId", SqlDbType.Int).Value = model.UserId;
    }

    private void FillSupplyDetailParameters(SqlCommand command, SupplyDetailEditorModel model)
    {
        command.Parameters.Add("@productId", SqlDbType.Int).Value = model.ProductId;
        command.Parameters.Add("@supplyId", SqlDbType.Int).Value = model.SupplyId;
        command.Parameters.Add("@quantity", SqlDbType.Int).Value = model.Quantity;
        command.Parameters.Add("@unitPrice", SqlDbType.Decimal).Value = model.UnitPrice;
        command.Parameters["@unitPrice"].Precision = 8;
        command.Parameters["@unitPrice"].Scale = 2;
        command.Parameters.Add("@productionDate", SqlDbType.Date).Value = model.ProductionDate.Date;
    }

    private void FillLossParameters(SqlCommand command, LossEditorModel model)
    {
        command.Parameters.Add("@productId", SqlDbType.Int).Value = model.ProductId;
        command.Parameters.Add("@quantity", SqlDbType.Int).Value = model.Quantity;
        command.Parameters.Add("@reason", SqlDbType.VarChar, -1).Value = model.Reason.Trim();
        command.Parameters.Add("@lossDate", SqlDbType.Date).Value = model.LossDate.Date;
    }

    private void EnsureLossQuantityColumn(SqlConnection connection, SqlTransaction? transaction = null)
    {
        if (_lossQuantityColumnChecked)
        {
            return;
        }

        lock (_schemaSyncRoot)
        {
            if (_lossQuantityColumnChecked)
            {
                return;
            }

            using var checkCommand = transaction is null
                ? new SqlCommand("SELECT COL_LENGTH('dbo.LOSSES', 'QUANTITY');", connection)
                : new SqlCommand("SELECT COL_LENGTH('dbo.LOSSES', 'QUANTITY');", connection, transaction);
            var columnLength = checkCommand.ExecuteScalar();

            if (columnLength is null or DBNull)
            {
                using var alterCommand = transaction is null
                    ? new SqlCommand("""
                                     ALTER TABLE dbo.LOSSES
                                     ADD QUANTITY INT NOT NULL
                                         CONSTRAINT DF_LOSSES_QUANTITY DEFAULT (1) WITH VALUES;
                                     """, connection)
                    : new SqlCommand("""
                                     ALTER TABLE dbo.LOSSES
                                     ADD QUANTITY INT NOT NULL
                                         CONSTRAINT DF_LOSSES_QUANTITY DEFAULT (1) WITH VALUES;
                                     """, connection, transaction);
                alterCommand.ExecuteNonQuery();
            }

            _lossQuantityColumnChecked = true;
        }
    }

    private void SaveStatus(string tableName, StatusEditorModel model)
    {
        using var connection = OpenConnection();
        if (model.StatusId == 0)
        {
            var insertSql = $"""
                             INSERT INTO {tableName} (NAME)
                             VALUES (@name);
                             """;
            using var command = new SqlCommand(insertSql, connection);
            command.Parameters.Add("@name", SqlDbType.VarChar, 50).Value = model.StatusName.Trim();
            command.ExecuteNonQuery();
            return;
        }

        var updateSql = $"""
                         UPDATE {tableName}
                         SET NAME = @name
                         WHERE ID_STATUS = @statusId;
                         """;
        using var updateCommand = new SqlCommand(updateSql, connection);
        updateCommand.Parameters.Add("@statusId", SqlDbType.Int).Value = model.StatusId;
        updateCommand.Parameters.Add("@name", SqlDbType.VarChar, 50).Value = model.StatusName.Trim();
        updateCommand.ExecuteNonQuery();
    }

    private void DeleteStatus(string tableName, int statusId)
    {
        var sql = $"""
                    DELETE FROM {tableName}
                    WHERE ID_STATUS = @statusId;
                    """;
        ExecuteDelete(sql, ("@statusId", SqlDbType.Int, statusId));
    }

    private void ExecuteDelete(string sql, params (string Name, SqlDbType Type, object Value)[] parameters)
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter.Name, parameter.Type).Value = parameter.Value;
        }

        command.ExecuteNonQuery();
    }

    private static void AddNullableString(SqlCommand command, string parameterName, string? value, int length)
    {
        var parameter = command.Parameters.Add(parameterName, SqlDbType.VarChar, length);
        parameter.Value = string.IsNullOrWhiteSpace(value)
            ? DBNull.Value
            : value.Trim();
    }

    private static string FormatCurrency(decimal value) => $"{value:0.##} ₽";

    private static ApplicationSettingsModel NormalizeApplicationSettings(ApplicationSettingsModel settings)
    {
        return new ApplicationSettingsModel
        {
            ExpirationWarningDays = Math.Clamp(settings.ExpirationWarningDays, 1, 90)
        };
    }

    private ApplicationSettingsModel GetCurrentApplicationSettings()
    {
        return NormalizeApplicationSettings(_settingsStore.Load());
    }

    private SqlConnection OpenConnection()
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
