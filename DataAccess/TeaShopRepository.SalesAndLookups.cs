using System.Data;
using Microsoft.Data.SqlClient;
using TeaShopManager.Models;

namespace TeaShopManager.DataAccess;

public sealed partial class TeaShopRepository
{
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

    public IReadOnlyList<LookupItem> GetReportSellerLookupItems()
    {
        const string sql = """
                           SELECT
                               u.ID_USER,
                               COALESCE(NULLIF(LTRIM(RTRIM(u.FULL_NAME)), ''), u.LOGIN) AS DISPLAY_NAME
                           FROM USERS u
                           ORDER BY DISPLAY_NAME;
                           """;

        var result = new List<LookupItem>();
        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            result.Add(new LookupItem
            {
                Id = Convert.ToInt32(reader["ID_USER"]),
                Name = reader["DISPLAY_NAME"]?.ToString() ?? string.Empty
            });
        }

        return result;
    }

    public IReadOnlyList<LookupItem> GetReportCategoryLookupItems()
    {
        return GetCategories()
            .Select(category => new LookupItem { Id = category.CategoryId, Name = category.CategoryName })
            .ToList();
    }

}
