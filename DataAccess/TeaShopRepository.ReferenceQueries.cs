using System.Data;
using Microsoft.Data.SqlClient;
using TeaShopManager.Models;

namespace TeaShopManager.DataAccess;

public sealed partial class TeaShopRepository
{
    public IReadOnlyList<UserPreviewItem> GetUsers()
    {
        const string sql =
            """
            SELECT
                u.ID_USER,
                u.LOGIN,
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
        const string sql =
            """
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
        const string sql =
            """
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
        var sql =
            $"""
            SELECT
                p.ID_PRODUCT,
                p.PRODUCT_NAME,
                p.PRICE,
                p.MIN_STOCK_QUANTITY,
                p.DESCRIPTION,
                p.EXPIRATION_DATE,
                p.ID_CATEGORY,
                c.CATEGORY_NAME,
                COALESCE(sd.TOTAL_SUPPLIED, 0) - COALESCE(od.TOTAL_SOLD, 0) - COALESCE(ls.TOTAL_LOST, 0) AS STOCK_QUANTITY
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
            OUTER APPLY (
                SELECT SUM(l.QUANTITY) AS TOTAL_LOST
                FROM LOSSES l
                WHERE l.ID_PRODUCT = p.ID_PRODUCT
            ) ls
            ORDER BY p.PRODUCT_NAME;
            """;

        var result = new List<ProductPreviewItem>();
        using var connection = OpenConnection();
        EnsureLossQuantityColumn(connection);
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
        const string sql =
            """
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
        const string sql =
            """
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
        const string sql =
            """
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
        const string sql =
            """
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
        const string sql =
            """
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
        const string sql =
            """
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
        const string sql =
            """
            SELECT
                l.ID_LOSS,
                l.ID_PRODUCT,
                l.QUANTITY,
                p.PRODUCT_NAME,
                l.REASON,
                l.LOSS_DATE
            FROM LOSSES l
            INNER JOIN PRODUCTS p ON p.ID_PRODUCT = l.ID_PRODUCT
            ORDER BY l.LOSS_DATE DESC;
            """;

        var result = new List<LossPreviewItem>();
        using var connection = OpenConnection();
        EnsureLossQuantityColumn(connection);
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var lossId = Convert.ToInt32(reader["ID_LOSS"]);
            result.Add(new LossPreviewItem
            {
                LossId = lossId,
                ProductId = Convert.ToInt32(reader["ID_PRODUCT"]),
                QuantityValue = Convert.ToInt32(reader["QUANTITY"]),
                LossDateValue = Convert.ToDateTime(reader["LOSS_DATE"]),
                LossCode = lossId.ToString(),
                ProductName = reader["PRODUCT_NAME"]?.ToString() ?? string.Empty,
                Quantity = Convert.ToInt32(reader["QUANTITY"]).ToString(),
                Reason = reader["REASON"]?.ToString() ?? string.Empty,
                LossDate = Convert.ToDateTime(reader["LOSS_DATE"]).ToString("dd.MM.yyyy")
            });
        }

        return result;
    }

}
