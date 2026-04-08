using System.Data;
using Microsoft.Data.SqlClient;
using TeaShopManager.Models;

namespace TeaShopManager.DataAccess;

public sealed partial class TeaShopRepository
{
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
        EnsureLossQuantityColumn(connection);
        using var transaction = connection.BeginTransaction();

        try
        {
            if (model.LossId == 0)
            {
                const string insertSql = """
                                         INSERT INTO LOSSES (ID_PRODUCT, QUANTITY, REASON, LOSS_DATE)
                                         VALUES (@productId, @quantity, @reason, @lossDate);
                                         """;
                using var command = new SqlCommand(insertSql, connection, transaction);
                FillLossParameters(command, model);
                command.ExecuteNonQuery();
            }
            else
            {
                const string updateSql = """
                                         UPDATE LOSSES
                                         SET ID_PRODUCT = @productId,
                                             QUANTITY = @quantity,
                                             REASON = @reason,
                                             LOSS_DATE = @lossDate
                                         WHERE ID_LOSS = @lossId;
                                         """;
                using var updateCommand = new SqlCommand(updateSql, connection, transaction);
                updateCommand.Parameters.Add("@lossId", SqlDbType.Int).Value = model.LossId;
                FillLossParameters(updateCommand, model);
                updateCommand.ExecuteNonQuery();
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

    public void DeleteLoss(int lossId)
    {
        using var connection = OpenConnection();
        EnsureLossQuantityColumn(connection);
        using var transaction = connection.BeginTransaction();

        try
        {
            const string sql = "DELETE FROM LOSSES WHERE ID_LOSS = @lossId;";
            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.Add("@lossId", SqlDbType.Int).Value = lossId;
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
        EnsureLossQuantityColumn(connection);
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

        var remainingConsumedQuantity = GetCountedSoldQuantity(connection, null, productId) + GetCountedLossQuantity(connection, null, productId);
        foreach (var incomingItem in incomingItems)
        {
            var consumedQuantity = Math.Min(remainingConsumedQuantity, incomingItem.Quantity);
            remainingConsumedQuantity -= consumedQuantity;
            var remainingQuantity = incomingItem.Quantity - consumedQuantity;
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
                ConsumedQuantityValue = consumedQuantity,
                RemainingQuantityValue = remainingQuantity,
                MovementType = isCustomerReturn ? "Возврат" : "Поставка",
                DocumentCode = incomingItem.SupplyId.ToString(),
                PartnerName = incomingItem.SupplierName,
                ReceivedDate = incomingItem.ReceivedDate.ToString("dd.MM.yyyy"),
                ProductionDate = incomingItem.ProductionDate.ToString("dd.MM.yyyy"),
                UnitPrice = $"{incomingItem.UnitPrice:0.##} ₽",
                Quantity = incomingItem.Quantity.ToString(),
                ConsumedQuantity = consumedQuantity.ToString(),
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

}
