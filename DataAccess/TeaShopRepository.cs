using System.Data;
using Microsoft.Data.SqlClient;
using TeaShopManager.Models;

namespace TeaShopManager.DataAccess;

public sealed class TeaShopRepository
{
    private const string LowStockNotificationType = "Минимальный остаток";
    private const string CustomerReturnSupplierName = "Возврат покупателя";
    private const string TeaCategoryName = "Чай";

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

    public TeaShopRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IReadOnlyList<UserPreviewItem> GetUsers()
    {
        const string sql = """
                           SELECT
                               u.ID_USER,
                               u.LOGIN,
                               u.PASSWORD,
                               u.FULL_NAME,
                               u.ID_ROLE,
                               r.ROLE_NAME
                           FROM USERS u
                           INNER JOIN ROLE r ON r.ID_ROLE = u.ID_ROLE
                           ORDER BY u.LOGIN;
                           """;

        var result = new List<UserPreviewItem>();
        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            result.Add(new UserPreviewItem
            {
                UserId = reader.GetInt32(reader.GetOrdinal("ID_USER")),
                Login = reader["LOGIN"]?.ToString() ?? string.Empty,
                PasswordHash = reader["PASSWORD"]?.ToString() ?? string.Empty,
                FullName = reader["FULL_NAME"]?.ToString() ?? string.Empty,
                RoleId = Convert.ToInt16(reader["ID_ROLE"]),
                RoleName = reader["ROLE_NAME"]?.ToString() ?? string.Empty,
                Status = "Активен"
            });
        }

        return result;
    }

    public IReadOnlyList<RolePreviewItem> GetRoles()
    {
        const string sql = """
                           SELECT ID_ROLE, ROLE_NAME
                           FROM ROLE
                           ORDER BY ID_ROLE;
                           """;

        var result = new List<RolePreviewItem>();
        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var roleId = Convert.ToInt16(reader["ID_ROLE"]);
            result.Add(new RolePreviewItem
            {
                RoleId = roleId,
                RoleCode = roleId.ToString(),
                RoleName = reader["ROLE_NAME"]?.ToString() ?? string.Empty
            });
        }

        return result;
    }

    public IReadOnlyList<CategoryPreviewItem> GetCategories()
    {
        const string sql = """
                           SELECT ID_CATEGORY, CATEGORY_NAME
                           FROM CATEGORY
                           ORDER BY CATEGORY_NAME;
                           """;

        var result = new List<CategoryPreviewItem>();
        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var categoryId = Convert.ToInt16(reader["ID_CATEGORY"]);
            result.Add(new CategoryPreviewItem
            {
                CategoryId = categoryId,
                CategoryCode = categoryId.ToString(),
                CategoryName = reader["CATEGORY_NAME"]?.ToString() ?? string.Empty
            });
        }

        return result;
    }

    public IReadOnlyList<ProductPreviewItem> GetProducts()
    {
        var sql = $"""
                    SELECT
                        p.ID_PRODUCT,
                        p.PRODUCT_NAME,
                        p.PRICE,
                        p.MIN_STOCK_QUANTITY,
                        p.DESCRIPTION,
                        p.EXPIRATION_DATE,
                        p.ID_CATEGORY,
                        c.CATEGORY_NAME,
                        COALESCE(sd.TOTAL_SUPPLIED, 0) - COALESCE(od.TOTAL_SOLD, 0) AS STOCK_QUANTITY
                    FROM PRODUCTS p
                    INNER JOIN CATEGORY c ON c.ID_CATEGORY = p.ID_CATEGORY
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
                    ORDER BY p.PRODUCT_NAME;
                    """;

        var result = new List<ProductPreviewItem>();
        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var minStock = Convert.ToInt32(reader["MIN_STOCK_QUANTITY"]);
            var stock = Convert.ToInt32(reader["STOCK_QUANTITY"]);
            var expirationDays = Convert.ToInt16(reader["EXPIRATION_DATE"]);
            result.Add(new ProductPreviewItem
            {
                ProductId = Convert.ToInt32(reader["ID_PRODUCT"]),
                ProductName = reader["PRODUCT_NAME"]?.ToString() ?? string.Empty,
                PriceValue = Convert.ToDecimal(reader["PRICE"]),
                Description = reader["DESCRIPTION"]?.ToString() ?? string.Empty,
                ExpirationDays = expirationDays,
                CategoryId = Convert.ToInt16(reader["ID_CATEGORY"]),
                CategoryName = reader["CATEGORY_NAME"]?.ToString() ?? string.Empty,
                StockQuantityValue = stock,
                StockQuantity = stock.ToString(),
                MinStockQuantityValue = minStock,
                MinStockQuantity = minStock.ToString(),
                ExpirationDate = $"{expirationDays} дн.",
                IsCritical = stock < minStock
            });
        }

        return result;
    }

    public IReadOnlyList<SupplierPreviewItem> GetSuppliers()
    {
        const string sql = """
                           SELECT
                               ID_SUPPLIER,
                               SUPPLIER_NAME,
                               CONTACT_PERSON,
                               PHONE,
                               EMAIL,
                               ADDRESS
                           FROM SUPPLIERS
                           ORDER BY SUPPLIER_NAME;
                           """;

        var result = new List<SupplierPreviewItem>();
        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            result.Add(new SupplierPreviewItem
            {
                SupplierId = Convert.ToInt32(reader["ID_SUPPLIER"]),
                SupplierName = reader["SUPPLIER_NAME"]?.ToString() ?? string.Empty,
                ContactPerson = reader["CONTACT_PERSON"]?.ToString() ?? string.Empty,
                Phone = reader["PHONE"]?.ToString() ?? string.Empty,
                Email = reader["EMAIL"]?.ToString() ?? string.Empty,
                Address = reader["ADDRESS"]?.ToString() ?? string.Empty
            });
        }

        return result;
    }

    public IReadOnlyList<StatusPreviewItem> GetStatusOrders()
    {
        return GetStatuses("STATUS_ORDER");
    }

    public IReadOnlyList<StatusPreviewItem> GetStatusSupplies()
    {
        return GetStatuses("STATUS_SUPPLIES");
    }

    public IReadOnlyList<OrderPreviewItem> GetOrders()
    {
        const string sql = """
                           SELECT
                               o.ID_ORDER,
                               o.ORDER_DATE,
                               o.ID_STATUS,
                               o.ID_USER,
                               so.NAME AS STATUS_NAME,
                               COALESCE(SUM(od.QUANTITY * od.PRICE), 0) AS TOTAL_AMOUNT
                           FROM ORDERS o
                           INNER JOIN STATUS_ORDER so ON so.ID_STATUS = o.ID_STATUS
                           LEFT JOIN ORDER_DETAILS od ON od.ID_ORDER = o.ID_ORDER
                           GROUP BY o.ID_ORDER, o.ORDER_DATE, o.ID_STATUS, o.ID_USER, so.NAME
                           ORDER BY o.ID_ORDER DESC;
                           """;

        var result = new List<OrderPreviewItem>();
        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var orderId = Convert.ToInt32(reader["ID_ORDER"]);
            var totalAmount = Convert.ToDecimal(reader["TOTAL_AMOUNT"]);
            result.Add(new OrderPreviewItem
            {
                OrderId = orderId,
                StatusId = Convert.ToInt32(reader["ID_STATUS"]),
                UserId = Convert.ToInt32(reader["ID_USER"]),
                OrderDateValue = Convert.ToDateTime(reader["ORDER_DATE"]),
                OrderCode = $"#{orderId}",
                OrderDate = Convert.ToDateTime(reader["ORDER_DATE"]).ToString("dd.MM.yyyy HH:mm"),
                StatusName = reader["STATUS_NAME"]?.ToString() ?? string.Empty,
                TotalAmount = $"{totalAmount:0.##} ₽"
            });
        }

        return result;
    }

    public IReadOnlyList<OrderDetailPreviewItem> GetOrderDetails()
    {
        const string sql = """
                           SELECT
                               od.ID_ORDER,
                               od.ID_PRODUCT,
                               p.PRODUCT_NAME,
                               od.QUANTITY,
                               od.PRICE
                           FROM ORDER_DETAILS od
                           INNER JOIN PRODUCTS p ON p.ID_PRODUCT = od.ID_PRODUCT
                           ORDER BY od.ID_ORDER DESC, p.PRODUCT_NAME;
                           """;

        var result = new List<OrderDetailPreviewItem>();
        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var orderId = Convert.ToInt32(reader["ID_ORDER"]);
            result.Add(new OrderDetailPreviewItem
            {
                OrderId = orderId,
                ProductId = Convert.ToInt32(reader["ID_PRODUCT"]),
                QuantityValue = Convert.ToInt32(reader["QUANTITY"]),
                PriceValue = Convert.ToDecimal(reader["PRICE"]),
                OrderCode = $"#{orderId}",
                ProductName = reader["PRODUCT_NAME"]?.ToString() ?? string.Empty,
                Quantity = Convert.ToInt32(reader["QUANTITY"]).ToString(),
                Price = $"{Convert.ToDecimal(reader["PRICE"]):0.##} ₽"
            });
        }

        return result;
    }

    public IReadOnlyList<NotificationPreviewItem> GetNotifications()
    {
        const string sql = """
                           SELECT
                               ID_NOTIFICATION,
                               NOTIFICATION_TYPE,
                               MESSAGE,
                               CREATED_DATE,
                               ID_PRODUCT,
                               ID_USER,
                               IS_READ
                           FROM NOTIFICATIONS
                           ORDER BY IS_READ, CREATED_DATE DESC;
                           """;

        var result = new List<NotificationPreviewItem>();
        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            result.Add(new NotificationPreviewItem
            {
                NotificationId = Convert.ToInt32(reader["ID_NOTIFICATION"]),
                ProductId = Convert.ToInt32(reader["ID_PRODUCT"]),
                UserId = Convert.ToInt32(reader["ID_USER"]),
                IsRead = Convert.ToBoolean(reader["IS_READ"]),
                CreatedAtValue = Convert.ToDateTime(reader["CREATED_DATE"]),
                NotificationType = reader["NOTIFICATION_TYPE"]?.ToString() ?? string.Empty,
                Message = reader["MESSAGE"]?.ToString() ?? string.Empty,
                CreatedAt = Convert.ToDateTime(reader["CREATED_DATE"]).ToString("dd.MM.yyyy HH:mm"),
                ReadState = Convert.ToBoolean(reader["IS_READ"]) ? "Да" : "Нет"
            });
        }

        return result;
    }

    public IReadOnlyList<SupplyPreviewItem> GetSupplies()
    {
        const string sql = """
                           SELECT
                               s.ID_SUPPLIES,
                               s.CREATION_DATE,
                               s.DELIVERY_DATE,
                               s.ID_STATUS,
                               s.ID_SUPPLIER,
                               s.ID_USER,
                               ss.NAME AS STATUS_NAME,
                               sp.SUPPLIER_NAME,
                               u.LOGIN
                           FROM SUPPLIES s
                           INNER JOIN STATUS_SUPPLIES ss ON ss.ID_STATUS = s.ID_STATUS
                           INNER JOIN SUPPLIERS sp ON sp.ID_SUPPLIER = s.ID_SUPPLIER
                           INNER JOIN USERS u ON u.ID_USER = s.ID_USER
                           ORDER BY s.ID_SUPPLIES DESC;
                           """;

        var result = new List<SupplyPreviewItem>();
        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var supplyId = Convert.ToInt32(reader["ID_SUPPLIES"]);
            var statusName = reader["STATUS_NAME"]?.ToString() ?? string.Empty;
            var isAccepted = IsAcceptedSupplyStatusName(statusName);
            result.Add(new SupplyPreviewItem
            {
                SupplyId = supplyId,
                StatusId = Convert.ToInt32(reader["ID_STATUS"]),
                SupplierId = Convert.ToInt32(reader["ID_SUPPLIER"]),
                UserId = Convert.ToInt32(reader["ID_USER"]),
                CreationDateValue = Convert.ToDateTime(reader["CREATION_DATE"]),
                DeliveryDateValue = reader["DELIVERY_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["DELIVERY_DATE"]),
                SupplyCode = supplyId.ToString(),
                CreationDate = Convert.ToDateTime(reader["CREATION_DATE"]).ToString("dd.MM.yyyy"),
                DeliveryDate = reader["DELIVERY_DATE"] == DBNull.Value
                    ? "-"
                    : Convert.ToDateTime(reader["DELIVERY_DATE"]).ToString("dd.MM.yyyy"),
                StatusName = statusName,
                SupplierName = reader["SUPPLIER_NAME"]?.ToString() ?? string.Empty,
                UserName = reader["LOGIN"]?.ToString() ?? string.Empty,
                CanModify = !isAccepted,
                CanReceive = !isAccepted
            });
        }

        return result;
    }

    public IReadOnlyList<SupplyDetailPreviewItem> GetSupplyDetails()
    {
        const string sql = """
                           SELECT
                               sd.ID_SUPPLIES,
                               sd.ID_PRODUCT,
                               p.PRODUCT_NAME,
                               sd.QUANTITY,
                               sd.UNIT_PRICE,
                               sd.PRODUCTION_DATE,
                               ss.NAME AS STATUS_NAME
                           FROM SUPPLIES_DETAILS sd
                           INNER JOIN SUPPLIES s ON s.ID_SUPPLIES = sd.ID_SUPPLIES
                           INNER JOIN STATUS_SUPPLIES ss ON ss.ID_STATUS = s.ID_STATUS
                           INNER JOIN PRODUCTS p ON p.ID_PRODUCT = sd.ID_PRODUCT
                           ORDER BY sd.ID_SUPPLIES DESC, p.PRODUCT_NAME;
                           """;

        var result = new List<SupplyDetailPreviewItem>();
        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var supplyId = Convert.ToInt32(reader["ID_SUPPLIES"]);
            var statusName = reader["STATUS_NAME"]?.ToString() ?? string.Empty;
            result.Add(new SupplyDetailPreviewItem
            {
                SupplyId = supplyId,
                ProductId = Convert.ToInt32(reader["ID_PRODUCT"]),
                QuantityValue = Convert.ToInt32(reader["QUANTITY"]),
                UnitPriceValue = Convert.ToDecimal(reader["UNIT_PRICE"]),
                ProductionDateValue = Convert.ToDateTime(reader["PRODUCTION_DATE"]),
                SupplyCode = supplyId.ToString(),
                ProductName = reader["PRODUCT_NAME"]?.ToString() ?? string.Empty,
                Quantity = Convert.ToInt32(reader["QUANTITY"]).ToString(),
                UnitPrice = $"{Convert.ToDecimal(reader["UNIT_PRICE"]):0.##} ₽",
                ProductionDate = Convert.ToDateTime(reader["PRODUCTION_DATE"]).ToString("dd.MM.yyyy"),
                CanModify = !IsAcceptedSupplyStatusName(statusName)
            });
        }

        return result;
    }

    public IReadOnlyList<LossPreviewItem> GetLosses()
    {
        const string sql = """
                           SELECT
                               l.ID_LOSS,
                               l.ID_PRODUCT,
                               p.PRODUCT_NAME,
                               l.REASON,
                               l.LOSS_DATE
                           FROM LOSSES l
                           INNER JOIN PRODUCTS p ON p.ID_PRODUCT = l.ID_PRODUCT
                           ORDER BY l.LOSS_DATE DESC;
                           """;

        var result = new List<LossPreviewItem>();
        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var lossId = Convert.ToInt32(reader["ID_LOSS"]);
            result.Add(new LossPreviewItem
            {
                LossId = lossId,
                ProductId = Convert.ToInt32(reader["ID_PRODUCT"]),
                LossDateValue = Convert.ToDateTime(reader["LOSS_DATE"]),
                LossCode = lossId.ToString(),
                ProductName = reader["PRODUCT_NAME"]?.ToString() ?? string.Empty,
                Reason = reader["REASON"]?.ToString() ?? string.Empty,
                LossDate = Convert.ToDateTime(reader["LOSS_DATE"]).ToString("dd.MM.yyyy")
            });
        }

        return result;
    }

    public void SaveUser(UserEditorModel model)
    {
        using var connection = OpenConnection();

        if (model.UserId == 0)
        {
            const string insertSql = """
                                     INSERT INTO USERS (LOGIN, PASSWORD, FULL_NAME, ID_ROLE)
                                     VALUES (@login, @password, @fullName, @roleId);
                                     """;

            using var command = new SqlCommand(insertSql, connection);
            command.Parameters.Add("@login", SqlDbType.VarChar, 50).Value = model.Login.Trim();
            command.Parameters.Add("@password", SqlDbType.VarChar, 4000).Value =
                PasswordHasher.HashPasswordSha256(model.Password);
            AddNullableString(command, "@fullName", model.FullName, 4000);
            command.Parameters.Add("@roleId", SqlDbType.SmallInt).Value = model.RoleId;
            command.ExecuteNonQuery();
            return;
        }

        var updateSql = string.IsNullOrWhiteSpace(model.Password)
            ? """
              UPDATE USERS
              SET LOGIN = @login,
                  FULL_NAME = @fullName,
                  ID_ROLE = @roleId
              WHERE ID_USER = @userId;
              """
            : """
              UPDATE USERS
              SET LOGIN = @login,
                  PASSWORD = @password,
                  FULL_NAME = @fullName,
                  ID_ROLE = @roleId
              WHERE ID_USER = @userId;
              """;

        using (var command = new SqlCommand(updateSql, connection))
        {
            command.Parameters.Add("@userId", SqlDbType.Int).Value = model.UserId;
            command.Parameters.Add("@login", SqlDbType.VarChar, 50).Value = model.Login.Trim();
            AddNullableString(command, "@fullName", model.FullName, 4000);
            command.Parameters.Add("@roleId", SqlDbType.SmallInt).Value = model.RoleId;

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                command.Parameters.Add("@password", SqlDbType.VarChar, 4000).Value =
                    PasswordHasher.HashPasswordSha256(model.Password);
            }

            command.ExecuteNonQuery();
        }
    }

    public void DeleteUser(int userId)
    {
        const string sql = "DELETE FROM USERS WHERE ID_USER = @userId;";
        ExecuteDelete(sql, ("@userId", SqlDbType.Int, userId));
    }

    public void SaveCategory(CategoryEditorModel model)
    {
        using var connection = OpenConnection();
        if (model.CategoryId == 0)
        {
            const string insertSql = "INSERT INTO CATEGORY (CATEGORY_NAME) VALUES (@name);";
            using var command = new SqlCommand(insertSql, connection);
            command.Parameters.Add("@name", SqlDbType.VarChar, 100).Value = model.CategoryName.Trim();
            command.ExecuteNonQuery();
            return;
        }

        const string updateSql = """
                                 UPDATE CATEGORY
                                 SET CATEGORY_NAME = @name
                                 WHERE ID_CATEGORY = @categoryId;
                                 """;
        using (var command = new SqlCommand(updateSql, connection))
        {
            command.Parameters.Add("@categoryId", SqlDbType.SmallInt).Value = model.CategoryId;
            command.Parameters.Add("@name", SqlDbType.VarChar, 100).Value = model.CategoryName.Trim();
            command.ExecuteNonQuery();
        }
    }

    public void DeleteCategory(short categoryId)
    {
        const string sql = "DELETE FROM CATEGORY WHERE ID_CATEGORY = @categoryId;";
        ExecuteDelete(sql, ("@categoryId", SqlDbType.SmallInt, categoryId));
    }

    public void SaveRole(RoleEditorModel model)
    {
        using var connection = OpenConnection();
        if (model.RoleId == 0)
        {
            const string insertSql = "INSERT INTO ROLE (ROLE_NAME) VALUES (@name);";
            using var command = new SqlCommand(insertSql, connection);
            command.Parameters.Add("@name", SqlDbType.VarChar, 50).Value = model.RoleName.Trim();
            command.ExecuteNonQuery();
            return;
        }

        const string updateSql = "UPDATE ROLE SET ROLE_NAME = @name WHERE ID_ROLE = @roleId;";
        using var updateCommand = new SqlCommand(updateSql, connection);
        updateCommand.Parameters.Add("@roleId", SqlDbType.SmallInt).Value = model.RoleId;
        updateCommand.Parameters.Add("@name", SqlDbType.VarChar, 50).Value = model.RoleName.Trim();
        updateCommand.ExecuteNonQuery();
    }

    public void DeleteRole(short roleId)
    {
        const string sql = "DELETE FROM ROLE WHERE ID_ROLE = @roleId;";
        ExecuteDelete(sql, ("@roleId", SqlDbType.SmallInt, roleId));
    }

    public void SaveSupplier(SupplierEditorModel model)
    {
        using var connection = OpenConnection();
        if (model.SupplierId == 0)
        {
            const string insertSql = """
                                     INSERT INTO SUPPLIERS (SUPPLIER_NAME, CONTACT_PERSON, PHONE, EMAIL, ADDRESS)
                                     VALUES (@name, @contact, @phone, @email, @address);
                                     """;
            using var command = new SqlCommand(insertSql, connection);
            command.Parameters.Add("@name", SqlDbType.VarChar, 4000).Value = model.SupplierName.Trim();
            command.Parameters.Add("@contact", SqlDbType.VarChar, 4000).Value = model.ContactPerson.Trim();
            command.Parameters.Add("@phone", SqlDbType.VarChar, 20).Value = model.Phone.Trim();
            AddNullableString(command, "@email", model.Email, 100);
            command.Parameters.Add("@address", SqlDbType.VarChar, 4000).Value = model.Address.Trim();
            command.ExecuteNonQuery();
            return;
        }

        const string updateSql = """
                                 UPDATE SUPPLIERS
                                 SET SUPPLIER_NAME = @name,
                                     CONTACT_PERSON = @contact,
                                     PHONE = @phone,
                                     EMAIL = @email,
                                     ADDRESS = @address
                                 WHERE ID_SUPPLIER = @supplierId;
                                 """;

        using (var command = new SqlCommand(updateSql, connection))
        {
            command.Parameters.Add("@supplierId", SqlDbType.Int).Value = model.SupplierId;
            command.Parameters.Add("@name", SqlDbType.VarChar, 4000).Value = model.SupplierName.Trim();
            command.Parameters.Add("@contact", SqlDbType.VarChar, 4000).Value = model.ContactPerson.Trim();
            command.Parameters.Add("@phone", SqlDbType.VarChar, 20).Value = model.Phone.Trim();
            AddNullableString(command, "@email", model.Email, 100);
            command.Parameters.Add("@address", SqlDbType.VarChar, 4000).Value = model.Address.Trim();
            command.ExecuteNonQuery();
        }
    }

    public void DeleteSupplier(int supplierId)
    {
        const string sql = "DELETE FROM SUPPLIERS WHERE ID_SUPPLIER = @supplierId;";
        ExecuteDelete(sql, ("@supplierId", SqlDbType.Int, supplierId));
    }

    public void SaveProduct(ProductEditorModel model)
    {
        using var connection = OpenConnection();
        if (model.ProductId == 0)
        {
            const string insertSql = """
                                     INSERT INTO PRODUCTS
                                         (PRODUCT_NAME, PRICE, MIN_STOCK_QUANTITY, DESCRIPTION, EXPIRATION_DATE, ID_CATEGORY)
                                     VALUES
                                         (@name, @price, @minStock, @description, @expirationDays, @categoryId);
                                     """;
            using var command = new SqlCommand(insertSql, connection);
            FillProductParameters(command, model);
            command.ExecuteNonQuery();
            SyncLowStockNotifications(connection);
            return;
        }

        const string updateSql = """
                                 UPDATE PRODUCTS
                                 SET PRODUCT_NAME = @name,
                                     PRICE = @price,
                                     MIN_STOCK_QUANTITY = @minStock,
                                     DESCRIPTION = @description,
                                     EXPIRATION_DATE = @expirationDays,
                                     ID_CATEGORY = @categoryId
                                 WHERE ID_PRODUCT = @productId;
                                 """;
        using (var command = new SqlCommand(updateSql, connection))
        {
            command.Parameters.Add("@productId", SqlDbType.Int).Value = model.ProductId;
            FillProductParameters(command, model);
            command.ExecuteNonQuery();
        }

        SyncLowStockNotifications(connection);
    }

    public void DeleteProduct(int productId)
    {
        using var connection = OpenConnection();
        const string sql = "DELETE FROM PRODUCTS WHERE ID_PRODUCT = @productId;";
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@productId", SqlDbType.Int).Value = productId;
        command.ExecuteNonQuery();
        SyncLowStockNotifications(connection);
    }

    public void SaveStatusOrder(StatusEditorModel model)
    {
        SaveStatus("STATUS_ORDER", model);
    }

    public void DeleteStatusOrder(int statusId)
    {
        DeleteStatus("STATUS_ORDER", statusId);
    }

    public void SaveStatusSupply(StatusEditorModel model)
    {
        SaveStatus("STATUS_SUPPLIES", model);
    }

    public void DeleteStatusSupply(int statusId)
    {
        DeleteStatus("STATUS_SUPPLIES", statusId);
    }

    public void SaveOrder(OrderEditorModel model)
    {
        using var connection = OpenConnection();
        if (model.OrderId == 0)
        {
            const string insertSql = """
                                     INSERT INTO ORDERS (ORDER_DATE, ID_STATUS, ID_USER)
                                     VALUES (@orderDate, @statusId, @userId);
                                     """;
            using var command = new SqlCommand(insertSql, connection);
            command.Parameters.Add("@orderDate", SqlDbType.DateTime).Value = model.OrderDate;
            command.Parameters.Add("@statusId", SqlDbType.Int).Value = model.StatusId;
            command.Parameters.Add("@userId", SqlDbType.Int).Value = model.UserId;
            command.ExecuteNonQuery();
            return;
        }

        const string updateSql = """
                                 UPDATE ORDERS
                                 SET ORDER_DATE = @orderDate,
                                     ID_STATUS = @statusId,
                                     ID_USER = @userId
                                 WHERE ID_ORDER = @orderId;
                                 """;
        using var updateCommand = new SqlCommand(updateSql, connection);
        updateCommand.Parameters.Add("@orderId", SqlDbType.Int).Value = model.OrderId;
        updateCommand.Parameters.Add("@orderDate", SqlDbType.DateTime).Value = model.OrderDate;
        updateCommand.Parameters.Add("@statusId", SqlDbType.Int).Value = model.StatusId;
        updateCommand.Parameters.Add("@userId", SqlDbType.Int).Value = model.UserId;
        updateCommand.ExecuteNonQuery();
    }

    public void DeleteOrder(int orderId)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            using (var detailCommand = new SqlCommand("DELETE FROM ORDER_DETAILS WHERE ID_ORDER = @orderId;", connection, transaction))
            {
                detailCommand.Parameters.Add("@orderId", SqlDbType.Int).Value = orderId;
                detailCommand.ExecuteNonQuery();
            }

            using (var orderCommand = new SqlCommand("DELETE FROM ORDERS WHERE ID_ORDER = @orderId;", connection, transaction))
            {
                orderCommand.Parameters.Add("@orderId", SqlDbType.Int).Value = orderId;
                orderCommand.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void SaveOrderDetail(OrderDetailEditorModel model)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var isNew = model.OriginalOrderId == 0 && model.OriginalProductId == 0;
            var keysChanged = !isNew && (model.OriginalOrderId != model.OrderId || model.OriginalProductId != model.ProductId);

            if (isNew || keysChanged)
            {
                if (keysChanged)
                {
                    using var deleteCommand = new SqlCommand("DELETE FROM ORDER_DETAILS WHERE ID_ORDER = @orderId AND ID_PRODUCT = @productId;", connection, transaction);
                    deleteCommand.Parameters.Add("@orderId", SqlDbType.Int).Value = model.OriginalOrderId;
                    deleteCommand.Parameters.Add("@productId", SqlDbType.Int).Value = model.OriginalProductId;
                    deleteCommand.ExecuteNonQuery();
                }

                using var insertCommand = new SqlCommand("""
                                                         INSERT INTO ORDER_DETAILS (ID_PRODUCT, ID_ORDER, QUANTITY, PRICE)
                                                         VALUES (@productId, @orderId, @quantity, @price);
                                                         """, connection, transaction);
                FillOrderDetailParameters(insertCommand, model);
                insertCommand.ExecuteNonQuery();
            }
            else
            {
                using var updateCommand = new SqlCommand("""
                                                         UPDATE ORDER_DETAILS
                                                         SET QUANTITY = @quantity,
                                                             PRICE = @price
                                                         WHERE ID_ORDER = @orderId AND ID_PRODUCT = @productId;
                                                         """, connection, transaction);
                FillOrderDetailParameters(updateCommand, model);
                updateCommand.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void DeleteOrderDetail(int orderId, int productId)
    {
        const string sql = "DELETE FROM ORDER_DETAILS WHERE ID_ORDER = @orderId AND ID_PRODUCT = @productId;";
        ExecuteDelete(sql, ("@orderId", SqlDbType.Int, orderId), ("@productId", SqlDbType.Int, productId));
    }

    public void SaveNotification(NotificationEditorModel model)
    {
        using var connection = OpenConnection();
        if (model.NotificationId == 0)
        {
            const string insertSql = """
                                     INSERT INTO NOTIFICATIONS (NOTIFICATION_TYPE, MESSAGE, CREATED_DATE, ID_PRODUCT, ID_USER, IS_READ)
                                     VALUES (@type, @message, @createdDate, @productId, @userId, @isRead);
                                     """;
            using var command = new SqlCommand(insertSql, connection);
            FillNotificationParameters(command, model);
            command.ExecuteNonQuery();
            return;
        }

        const string updateSql = """
                                 UPDATE NOTIFICATIONS
                                 SET NOTIFICATION_TYPE = @type,
                                     MESSAGE = @message,
                                     CREATED_DATE = @createdDate,
                                     ID_PRODUCT = @productId,
                                     ID_USER = @userId,
                                     IS_READ = @isRead
                                 WHERE ID_NOTIFICATION = @notificationId;
                                 """;
        using var updateCommand = new SqlCommand(updateSql, connection);
        updateCommand.Parameters.Add("@notificationId", SqlDbType.Int).Value = model.NotificationId;
        FillNotificationParameters(updateCommand, model);
        updateCommand.ExecuteNonQuery();
    }

    public void DeleteNotification(int notificationId)
    {
        const string sql = "DELETE FROM NOTIFICATIONS WHERE ID_NOTIFICATION = @notificationId;";
        ExecuteDelete(sql, ("@notificationId", SqlDbType.Int, notificationId));
    }

    public void MarkNotificationAsRead(int notificationId)
    {
        const string sql = """
                           UPDATE NOTIFICATIONS
                           SET IS_READ = 1
                           WHERE ID_NOTIFICATION = @notificationId;
                           """;

        ExecuteDelete(sql, ("@notificationId", SqlDbType.Int, notificationId));
    }

    public void SaveSupply(SupplyEditorModel model)
    {
        using var connection = OpenConnection();
        if (model.SupplyId == 0)
        {
            if (IsSupplyAcceptedStatusId(connection, null, model.StatusId))
            {
                throw new InvalidOperationException("Нельзя создавать поставку сразу в принятом статусе. Сначала создайте поставку и добавьте её позиции.");
            }

            const string insertSql = """
                                     INSERT INTO SUPPLIES (CREATION_DATE, DELIVERY_DATE, ID_STATUS, ID_SUPPLIER, ID_USER)
                                     VALUES (@creationDate, @deliveryDate, @statusId, @supplierId, @userId);
                                     """;
            using var command = new SqlCommand(insertSql, connection);
            FillSupplyParameters(command, model);
            command.ExecuteNonQuery();
            return;
        }

        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureSupplyIsEditable(connection, transaction, model.SupplyId);

            var isAcceptedStatus = IsSupplyAcceptedStatusId(connection, transaction, model.StatusId);
            if (isAcceptedStatus)
            {
                EnsureSupplyHasDetails(connection, transaction, model.SupplyId);
            }

            const string updateSql = """
                                     UPDATE SUPPLIES
                                     SET CREATION_DATE = @creationDate,
                                         DELIVERY_DATE = @deliveryDate,
                                         ID_STATUS = @statusId,
                                         ID_SUPPLIER = @supplierId,
                                         ID_USER = @userId
                                     WHERE ID_SUPPLIES = @supplyId;
                                     """;
            using var updateCommand = new SqlCommand(updateSql, connection, transaction);
            updateCommand.Parameters.Add("@supplyId", SqlDbType.Int).Value = model.SupplyId;
            updateCommand.Parameters.Add("@creationDate", SqlDbType.Date).Value = model.CreationDate.Date;
            var deliveryDate = model.DeliveryDate ?? (isAcceptedStatus ? DateTime.Today : null);
            var deliveryParameter = updateCommand.Parameters.Add("@deliveryDate", SqlDbType.Date);
            deliveryParameter.Value = deliveryDate.HasValue ? deliveryDate.Value.Date : DBNull.Value;
            updateCommand.Parameters.Add("@statusId", SqlDbType.Int).Value = model.StatusId;
            updateCommand.Parameters.Add("@supplierId", SqlDbType.Int).Value = model.SupplierId;
            updateCommand.Parameters.Add("@userId", SqlDbType.Int).Value = model.UserId;
            updateCommand.ExecuteNonQuery();

            if (isAcceptedStatus)
            {
                SyncLowStockNotifications(connection, transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void DeleteSupply(int supplyId)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureSupplyIsEditable(connection, transaction, supplyId);

            using (var detailCommand = new SqlCommand("DELETE FROM SUPPLIES_DETAILS WHERE ID_SUPPLIES = @supplyId;", connection, transaction))
            {
                detailCommand.Parameters.Add("@supplyId", SqlDbType.Int).Value = supplyId;
                detailCommand.ExecuteNonQuery();
            }

            using (var supplyCommand = new SqlCommand("DELETE FROM SUPPLIES WHERE ID_SUPPLIES = @supplyId;", connection, transaction))
            {
                supplyCommand.Parameters.Add("@supplyId", SqlDbType.Int).Value = supplyId;
                supplyCommand.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void SaveSupplyDetail(SupplyDetailEditorModel model)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var isNew = model.OriginalSupplyId == 0 && model.OriginalProductId == 0;
            var keysChanged = !isNew && (model.OriginalSupplyId != model.SupplyId || model.OriginalProductId != model.ProductId);

            if (isNew)
            {
                EnsureSupplyIsEditable(connection, transaction, model.SupplyId);
            }
            else
            {
                EnsureSupplyIsEditable(connection, transaction, model.OriginalSupplyId);
                if (keysChanged)
                {
                    EnsureSupplyIsEditable(connection, transaction, model.SupplyId);
                }
            }

            if (isNew || keysChanged)
            {
                if (keysChanged)
                {
                    using var deleteCommand = new SqlCommand("DELETE FROM SUPPLIES_DETAILS WHERE ID_SUPPLIES = @supplyId AND ID_PRODUCT = @productId;", connection, transaction);
                    deleteCommand.Parameters.Add("@supplyId", SqlDbType.Int).Value = model.OriginalSupplyId;
                    deleteCommand.Parameters.Add("@productId", SqlDbType.Int).Value = model.OriginalProductId;
                    deleteCommand.ExecuteNonQuery();
                }

                using var insertCommand = new SqlCommand("""
                                                         INSERT INTO SUPPLIES_DETAILS (ID_PRODUCT, ID_SUPPLIES, QUANTITY, UNIT_PRICE, PRODUCTION_DATE)
                                                         VALUES (@productId, @supplyId, @quantity, @unitPrice, @productionDate);
                                                         """, connection, transaction);
                FillSupplyDetailParameters(insertCommand, model);
                insertCommand.ExecuteNonQuery();
            }
            else
            {
                using var updateCommand = new SqlCommand("""
                                                         UPDATE SUPPLIES_DETAILS
                                                         SET QUANTITY = @quantity,
                                                             UNIT_PRICE = @unitPrice,
                                                             PRODUCTION_DATE = @productionDate
                                                         WHERE ID_SUPPLIES = @supplyId AND ID_PRODUCT = @productId;
                                                         """, connection, transaction);
                FillSupplyDetailParameters(updateCommand, model);
                updateCommand.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void DeleteSupplyDetail(int supplyId, int productId)
    {
        using var connection = OpenConnection();
        EnsureSupplyIsEditable(connection, null, supplyId);
        const string sql = "DELETE FROM SUPPLIES_DETAILS WHERE ID_SUPPLIES = @supplyId AND ID_PRODUCT = @productId;";
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@supplyId", SqlDbType.Int).Value = supplyId;
        command.Parameters.Add("@productId", SqlDbType.Int).Value = productId;
        command.ExecuteNonQuery();
    }

    public void ReceiveSupply(int supplyId)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            if (IsSupplyAccepted(connection, transaction, supplyId))
            {
                throw new InvalidOperationException("Поставка уже принята.");
            }

            EnsureSupplyHasDetails(connection, transaction, supplyId);
            var acceptedStatusId = GetAcceptedSupplyStatusId(connection, transaction);

            const string sql = """
                               UPDATE SUPPLIES
                               SET ID_STATUS = @statusId,
                                   DELIVERY_DATE = COALESCE(DELIVERY_DATE, CAST(GETDATE() AS date))
                               WHERE ID_SUPPLIES = @supplyId;
                               """;

            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.Add("@statusId", SqlDbType.Int).Value = acceptedStatusId;
            command.Parameters.Add("@supplyId", SqlDbType.Int).Value = supplyId;
            command.ExecuteNonQuery();

            SyncLowStockNotifications(connection, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void SaveLoss(LossEditorModel model)
    {
        using var connection = OpenConnection();
        if (model.LossId == 0)
        {
            const string insertSql = """
                                     INSERT INTO LOSSES (ID_PRODUCT, REASON, LOSS_DATE)
                                     VALUES (@productId, @reason, @lossDate);
                                     """;
            using var command = new SqlCommand(insertSql, connection);
            FillLossParameters(command, model);
            command.ExecuteNonQuery();
            return;
        }

        const string updateSql = """
                                 UPDATE LOSSES
                                 SET ID_PRODUCT = @productId,
                                     REASON = @reason,
                                     LOSS_DATE = @lossDate
                                 WHERE ID_LOSS = @lossId;
                                 """;
        using var updateCommand = new SqlCommand(updateSql, connection);
        updateCommand.Parameters.Add("@lossId", SqlDbType.Int).Value = model.LossId;
        FillLossParameters(updateCommand, model);
        updateCommand.ExecuteNonQuery();
    }

    public void DeleteLoss(int lossId)
    {
        const string sql = "DELETE FROM LOSSES WHERE ID_LOSS = @lossId;";
        ExecuteDelete(sql, ("@lossId", SqlDbType.Int, lossId));
    }

    public IReadOnlyList<ProductMovementPreviewItem> GetProductMovements(int productId)
    {
        var incomingSql = $"""
                           SELECT
                               sd.ID_SUPPLIES,
                               sp.SUPPLIER_NAME,
                               COALESCE(s.DELIVERY_DATE, s.CREATION_DATE) AS RECEIVED_DATE,
                               sd.PRODUCTION_DATE,
                               sd.UNIT_PRICE,
                               sd.QUANTITY
                           FROM SUPPLIES_DETAILS sd
                           INNER JOIN SUPPLIES s ON s.ID_SUPPLIES = sd.ID_SUPPLIES
                           INNER JOIN STATUS_SUPPLIES ss ON ss.ID_STATUS = s.ID_STATUS
                           INNER JOIN SUPPLIERS sp ON sp.ID_SUPPLIER = s.ID_SUPPLIER
                           WHERE sd.ID_PRODUCT = @productId
                             AND ({AcceptedSupplyStatusCondition})
                           ORDER BY COALESCE(s.DELIVERY_DATE, s.CREATION_DATE), sd.ID_SUPPLIES, sd.PRODUCTION_DATE, sd.ID_PRODUCT;
                           """;

        var result = new List<ProductMovementPreviewItem>();
        using var connection = OpenConnection();
        using var command = new SqlCommand(incomingSql, connection);
        command.Parameters.Add("@productId", SqlDbType.Int).Value = productId;
        using var reader = command.ExecuteReader();

        var incomingItems = new List<(int SupplyId, string SupplierName, DateTime ReceivedDate, DateTime ProductionDate, decimal UnitPrice, int Quantity)>();
        while (reader.Read())
        {
            incomingItems.Add((
                Convert.ToInt32(reader["ID_SUPPLIES"]),
                reader["SUPPLIER_NAME"]?.ToString() ?? string.Empty,
                Convert.ToDateTime(reader["RECEIVED_DATE"]),
                Convert.ToDateTime(reader["PRODUCTION_DATE"]),
                Convert.ToDecimal(reader["UNIT_PRICE"]),
                Convert.ToInt32(reader["QUANTITY"])));
        }

        reader.Close();

        var remainingSoldQuantity = GetCountedSoldQuantity(connection, null, productId);
        foreach (var incomingItem in incomingItems)
        {
            var soldQuantity = Math.Min(remainingSoldQuantity, incomingItem.Quantity);
            remainingSoldQuantity -= soldQuantity;
            var remainingQuantity = incomingItem.Quantity - soldQuantity;
            var isCustomerReturn = string.Equals(
                incomingItem.SupplierName,
                CustomerReturnSupplierName,
                StringComparison.CurrentCultureIgnoreCase);

            result.Add(new ProductMovementPreviewItem
            {
                SupplyId = incomingItem.SupplyId,
                IsCustomerReturn = isCustomerReturn,
                ReceivedDateValue = incomingItem.ReceivedDate,
                ProductionDateValue = incomingItem.ProductionDate,
                UnitPriceValue = incomingItem.UnitPrice,
                QuantityValue = incomingItem.Quantity,
                SoldQuantityValue = soldQuantity,
                RemainingQuantityValue = remainingQuantity,
                MovementType = isCustomerReturn ? "Возврат" : "Поставка",
                DocumentCode = incomingItem.SupplyId.ToString(),
                PartnerName = incomingItem.SupplierName,
                ReceivedDate = incomingItem.ReceivedDate.ToString("dd.MM.yyyy"),
                ProductionDate = incomingItem.ProductionDate.ToString("dd.MM.yyyy"),
                UnitPrice = $"{incomingItem.UnitPrice:0.##} ₽",
                Quantity = incomingItem.Quantity.ToString(),
                SoldQuantity = soldQuantity.ToString(),
                RemainingQuantity = remainingQuantity.ToString()
            });
        }

        return result;
    }

    public CustomerReturnEditorModel GetCustomerReturnDraft(int productId)
    {
        var sql = $"""
                    SELECT
                        p.ID_PRODUCT,
                        p.PRODUCT_NAME,
                        c.CATEGORY_NAME,
                        p.PRICE,
                        lastSupply.UNIT_PRICE AS LAST_UNIT_PRICE
                    FROM PRODUCTS p
                    INNER JOIN CATEGORY c ON c.ID_CATEGORY = p.ID_CATEGORY
                    OUTER APPLY (
                        SELECT TOP (1) sd.UNIT_PRICE
                        FROM SUPPLIES_DETAILS sd
                        INNER JOIN SUPPLIES s ON s.ID_SUPPLIES = sd.ID_SUPPLIES
                        INNER JOIN STATUS_SUPPLIES ss ON ss.ID_STATUS = s.ID_STATUS
                        WHERE sd.ID_PRODUCT = p.ID_PRODUCT
                          AND ({AcceptedSupplyStatusCondition})
                        ORDER BY COALESCE(s.DELIVERY_DATE, s.CREATION_DATE) DESC, sd.ID_SUPPLIES DESC
                    ) lastSupply
                    WHERE p.ID_PRODUCT = @productId;
                    """;

        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@productId", SqlDbType.Int).Value = productId;
        using var reader = command.ExecuteReader();

        if (!reader.Read())
        {
            throw new InvalidOperationException($"Товар с идентификатором {productId} не найден.");
        }

        var suggestedUnitPrice = reader["LAST_UNIT_PRICE"] == DBNull.Value
            ? Convert.ToDecimal(reader["PRICE"])
            : Convert.ToDecimal(reader["LAST_UNIT_PRICE"]);

        return new CustomerReturnEditorModel
        {
            ProductId = Convert.ToInt32(reader["ID_PRODUCT"]),
            ProductName = reader["PRODUCT_NAME"]?.ToString() ?? string.Empty,
            CategoryName = reader["CATEGORY_NAME"]?.ToString() ?? string.Empty,
            ReturnDate = DateTime.Today,
            Quantity = 1,
            UnitPrice = suggestedUnitPrice,
            ProductionDate = DateTime.Today
        };
    }

    public void CreateCustomerReturn(CustomerReturnEditorModel model, int userId)
    {
        if (model.ProductId <= 0)
        {
            throw new InvalidOperationException("Не выбран товар для возврата.");
        }

        if (model.Quantity <= 0)
        {
            throw new InvalidOperationException("Количество возврата должно быть больше нуля.");
        }

        if (model.UnitPrice <= 0)
        {
            throw new InvalidOperationException("Закупочная цена возврата должна быть больше нуля.");
        }

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            var productSnapshot = GetProductSnapshot(connection, transaction, model.ProductId);
            if (IsTeaCategoryName(productSnapshot.CategoryName))
            {
                throw new InvalidOperationException("Возврат на склад для категории \"Чай\" не поддерживается.");
            }

            var supplierId = GetOrCreateCustomerReturnSupplierId(connection, transaction);
            var statusId = GetAcceptedSupplyStatusId(connection, transaction);

            const string supplySql = """
                                     INSERT INTO SUPPLIES (CREATION_DATE, DELIVERY_DATE, ID_STATUS, ID_SUPPLIER, ID_USER)
                                     OUTPUT INSERTED.ID_SUPPLIES
                                     VALUES (@creationDate, @deliveryDate, @statusId, @supplierId, @userId);
                                     """;

            int supplyId;
            using (var supplyCommand = new SqlCommand(supplySql, connection, transaction))
            {
                supplyCommand.Parameters.Add("@creationDate", SqlDbType.Date).Value = model.ReturnDate.Date;
                supplyCommand.Parameters.Add("@deliveryDate", SqlDbType.Date).Value = model.ReturnDate.Date;
                supplyCommand.Parameters.Add("@statusId", SqlDbType.Int).Value = statusId;
                supplyCommand.Parameters.Add("@supplierId", SqlDbType.Int).Value = supplierId;
                supplyCommand.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                supplyId = Convert.ToInt32(supplyCommand.ExecuteScalar());
            }

            const string detailSql = """
                                     INSERT INTO SUPPLIES_DETAILS (ID_PRODUCT, ID_SUPPLIES, QUANTITY, UNIT_PRICE, PRODUCTION_DATE)
                                     VALUES (@productId, @supplyId, @quantity, @unitPrice, @productionDate);
                                     """;

            using (var detailCommand = new SqlCommand(detailSql, connection, transaction))
            {
                detailCommand.Parameters.Add("@productId", SqlDbType.Int).Value = model.ProductId;
                detailCommand.Parameters.Add("@supplyId", SqlDbType.Int).Value = supplyId;
                detailCommand.Parameters.Add("@quantity", SqlDbType.Int).Value = model.Quantity;
                detailCommand.Parameters.Add("@unitPrice", SqlDbType.Decimal).Value = model.UnitPrice;
                detailCommand.Parameters["@unitPrice"].Precision = 8;
                detailCommand.Parameters["@unitPrice"].Scale = 2;
                detailCommand.Parameters.Add("@productionDate", SqlDbType.Date).Value = model.ProductionDate.Date;
                detailCommand.ExecuteNonQuery();
            }

            SyncLowStockNotifications(connection, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public int CreateSale(int userId, IReadOnlyList<SaleDraftLine> lines)
    {
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Нельзя сохранить пустую продажу.");
        }

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            foreach (var requestedLine in lines
                         .GroupBy(line => new { line.ProductId, line.ProductName })
                         .Select(group => new
                         {
                             group.Key.ProductId,
                             group.Key.ProductName,
                             Quantity = group.Sum(line => line.Quantity)
                         }))
            {
                var availableQuantity = GetAvailableStockQuantity(connection, transaction, requestedLine.ProductId);
                if (requestedLine.Quantity > availableQuantity)
                {
                    var details = availableQuantity > 0
                        ? $"Доступно только {availableQuantity}."
                        : "Товара сейчас нет в наличии.";

                    throw new InvalidOperationException(
                        $"Недостаточно остатка для товара \"{requestedLine.ProductName}\". {details}");
                }
            }

            var statusId = GetCompletedSaleStatusId(connection, transaction);

            const string orderSql = """
                                    INSERT INTO ORDERS (ORDER_DATE, ID_STATUS, ID_USER)
                                    OUTPUT INSERTED.ID_ORDER
                                    VALUES (GETDATE(), @statusId, @userId);
                                    """;

            int orderId;
            using (var orderCommand = new SqlCommand(orderSql, connection, transaction))
            {
                orderCommand.Parameters.Add("@statusId", SqlDbType.Int).Value = statusId;
                orderCommand.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                orderId = Convert.ToInt32(orderCommand.ExecuteScalar());
            }

            const string detailSql = """
                                     INSERT INTO ORDER_DETAILS (ID_PRODUCT, ID_ORDER, QUANTITY, PRICE)
                                     VALUES (@productId, @orderId, @quantity, @price);
                                     """;

            foreach (var line in lines)
            {
                using var detailCommand = new SqlCommand(detailSql, connection, transaction);
                detailCommand.Parameters.Add("@productId", SqlDbType.Int).Value = line.ProductId;
                detailCommand.Parameters.Add("@orderId", SqlDbType.Int).Value = orderId;
                detailCommand.Parameters.Add("@quantity", SqlDbType.Int).Value = line.Quantity;
                detailCommand.Parameters.Add("@price", SqlDbType.Decimal).Value = line.UnitPrice;
                detailCommand.Parameters["@price"].Precision = 8;
                detailCommand.Parameters["@price"].Scale = 2;
                detailCommand.ExecuteNonQuery();
            }

            SyncLowStockNotifications(connection, transaction);
            transaction.Commit();
            return orderId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public IReadOnlyList<LookupItem> GetRoleLookupItems()
    {
        return GetRoles()
            .Select(role => new LookupItem { Id = role.RoleId, Name = role.RoleName })
            .ToList();
    }

    public IReadOnlyList<LookupItem> GetCategoryLookupItems()
    {
        return GetCategories()
            .Select(category => new LookupItem { Id = category.CategoryId, Name = category.CategoryName })
            .ToList();
    }

    public IReadOnlyList<LookupItem> GetUserLookupItems()
    {
        return GetUsers()
            .Select(user => new LookupItem { Id = user.UserId, Name = user.Login })
            .ToList();
    }

    public IReadOnlyList<LookupItem> GetProductLookupItems()
    {
        return GetProducts()
            .Select(product => new LookupItem { Id = product.ProductId, Name = product.ProductName })
            .ToList();
    }

    public IReadOnlyList<LookupItem> GetSupplierLookupItems()
    {
        return GetSuppliers()
            .Select(supplier => new LookupItem { Id = supplier.SupplierId, Name = supplier.SupplierName })
            .ToList();
    }

    public IReadOnlyList<LookupItem> GetOrderLookupItems()
    {
        return GetOrders()
            .Select(order => new LookupItem { Id = order.OrderId, Name = order.OrderCode })
            .ToList();
    }

    public IReadOnlyList<LookupItem> GetSupplyLookupItems()
    {
        return GetSupplies()
            .Where(supply => supply.CanModify)
            .Select(supply => new LookupItem { Id = supply.SupplyId, Name = supply.SupplyCode })
            .ToList();
    }

    public IReadOnlyList<LookupItem> GetStatusOrderLookupItems()
    {
        return GetStatusOrders()
            .Select(status => new LookupItem { Id = status.StatusId, Name = status.StatusName })
            .ToList();
    }

    public IReadOnlyList<LookupItem> GetStatusSupplyLookupItems()
    {
        return GetStatusSupplies()
            .Select(status => new LookupItem { Id = status.StatusId, Name = status.StatusName })
            .ToList();
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
        var sql = $"""
                    SELECT
                        COALESCE(sd.TOTAL_SUPPLIED, 0) - COALESCE(od.TOTAL_SOLD, 0) AS STOCK_QUANTITY
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

    private void SyncLowStockNotifications(SqlConnection connection, SqlTransaction? transaction = null)
    {
        var recipients = GetInventoryNotificationRecipients(connection, transaction);
        if (recipients.Count == 0)
        {
            return;
        }

        var criticalProducts = GetCriticalProducts(connection, transaction);
        var existing = GetExistingLowStockNotifications(connection, transaction);

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
                var createdAt = hasExisting ? snapshot.CreatedAt : DateTime.Now;
                var isRead = hasExisting && snapshot.IsRead;

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
    }

    private Dictionary<(int UserId, int ProductId), (bool IsRead, DateTime CreatedAt)> GetExistingLowStockNotifications(SqlConnection connection, SqlTransaction? transaction)
    {
        const string sql = """
                           SELECT ID_USER, ID_PRODUCT, IS_READ, CREATED_DATE
                           FROM NOTIFICATIONS
                           WHERE NOTIFICATION_TYPE = @type;
                           """;

        using var command = transaction is null
            ? new SqlCommand(sql, connection)
            : new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@type", SqlDbType.VarChar, 50).Value = LowStockNotificationType;

        using var reader = command.ExecuteReader();
        var result = new Dictionary<(int UserId, int ProductId), (bool IsRead, DateTime CreatedAt)>();
        while (reader.Read())
        {
            result[(Convert.ToInt32(reader["ID_USER"]), Convert.ToInt32(reader["ID_PRODUCT"]))] =
                (Convert.ToBoolean(reader["IS_READ"]), Convert.ToDateTime(reader["CREATED_DATE"]));
        }

        return result;
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
        var sql = $"""
                    SELECT
                        p.ID_PRODUCT,
                        p.PRODUCT_NAME,
                        p.MIN_STOCK_QUANTITY,
                        COALESCE(sd.TOTAL_SUPPLIED, 0) - COALESCE(od.TOTAL_SOLD, 0) AS STOCK_QUANTITY
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
                    WHERE COALESCE(sd.TOTAL_SUPPLIED, 0) - COALESCE(od.TOTAL_SOLD, 0) < p.MIN_STOCK_QUANTITY
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
        command.Parameters.Add("@reason", SqlDbType.VarChar, -1).Value = model.Reason.Trim();
        command.Parameters.Add("@lossDate", SqlDbType.Date).Value = model.LossDate.Date;
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

    private SqlConnection OpenConnection()
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
