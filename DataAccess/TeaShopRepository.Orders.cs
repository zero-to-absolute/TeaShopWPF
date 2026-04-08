using System.Data;
using Microsoft.Data.SqlClient;
using TeaShopManager.Models;

namespace TeaShopManager.DataAccess;

public sealed partial class TeaShopRepository
{
    public void SaveOrder(OrderEditorModel model)
    {
        using var connection = OpenConnection();
        if (model.OrderId == 0)
        {
            const string insertSql =
                """
                INSERT INTO ORDERS (ORDER_DATE, ID_STATUS, ID_USER)
                VALUES (@orderDate, @statusId, @userId);
                """;
            using var command = new SqlCommand(insertSql, connection);
            command.Parameters.Add("@orderDate", SqlDbType.DateTime).Value = model.OrderDate;
            command.Parameters.Add("@statusId", SqlDbType.Int).Value = model.StatusId;
            command.Parameters.Add("@userId", SqlDbType.Int).Value = model.UserId;
            command.ExecuteNonQuery();
            SyncLowStockNotifications(connection);
            return;
        }

        const string updateSql =
            """
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
        SyncLowStockNotifications(connection);
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

            SyncLowStockNotifications(connection, transaction);
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

                using var insertCommand = new SqlCommand(
                    """
                    INSERT INTO ORDER_DETAILS (ID_PRODUCT, ID_ORDER, QUANTITY, PRICE)
                    VALUES (@productId, @orderId, @quantity, @price);
                    """, connection, transaction);
                FillOrderDetailParameters(insertCommand, model);
                insertCommand.ExecuteNonQuery();
            }
            else
            {
                using var updateCommand = new SqlCommand(
                    """
                    UPDATE ORDER_DETAILS
                    SET QUANTITY = @quantity,
                        PRICE = @price
                    WHERE ID_ORDER = @orderId AND ID_PRODUCT = @productId;
                    """, connection, transaction);
                FillOrderDetailParameters(updateCommand, model);
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

    public void DeleteOrderDetail(int orderId, int productId)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            using var command = new SqlCommand("DELETE FROM ORDER_DETAILS WHERE ID_ORDER = @orderId AND ID_PRODUCT = @productId;", connection, transaction);
            command.Parameters.Add("@orderId", SqlDbType.Int).Value = orderId;
            command.Parameters.Add("@productId", SqlDbType.Int).Value = productId;
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

}
