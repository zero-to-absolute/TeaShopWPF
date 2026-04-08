using System.Data;
using Microsoft.Data.SqlClient;
using TeaShopManager.Models;

namespace TeaShopManager.DataAccess;

public sealed partial class TeaShopRepository
{
    public IReadOnlyList<SalesBySellerReportRow> GetSalesBySellerReport(DateTime dateFrom, DateTime dateTo, int? sellerId)
    {
        var sql =
            $"""
            SELECT
                o.ID_USER,
                COALESCE(NULLIF(LTRIM(RTRIM(u.FULL_NAME)), ''), u.LOGIN) AS SELLER_NAME,
                COUNT(DISTINCT o.ID_ORDER) AS ORDERS_COUNT,
                COUNT(*) AS POSITIONS_COUNT,
                SUM(od.QUANTITY) AS TOTAL_QUANTITY,
                SUM(od.QUANTITY * od.PRICE) AS TOTAL_REVENUE
            FROM ORDERS o
            INNER JOIN STATUS_ORDER so ON so.ID_STATUS = o.ID_STATUS
            INNER JOIN USERS u ON u.ID_USER = o.ID_USER
            INNER JOIN ORDER_DETAILS od ON od.ID_ORDER = o.ID_ORDER
            WHERE o.ORDER_DATE >= @dateFrom
              AND o.ORDER_DATE < DATEADD(DAY, 1, @dateTo)
              AND ({CountedOrderStatusCondition})
              AND (@sellerId IS NULL OR o.ID_USER = @sellerId)
            GROUP BY o.ID_USER, COALESCE(NULLIF(LTRIM(RTRIM(u.FULL_NAME)), ''), u.LOGIN)
            ORDER BY TOTAL_REVENUE DESC, SELLER_NAME;
            """;

        var result = new List<SalesBySellerReportRow>();
        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@dateFrom", SqlDbType.Date).Value = dateFrom.Date;
        command.Parameters.Add("@dateTo", SqlDbType.Date).Value = dateTo.Date;
        command.Parameters.Add("@sellerId", SqlDbType.Int).Value = sellerId ?? (object)DBNull.Value;
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var ordersCount = Convert.ToInt32(reader["ORDERS_COUNT"]);
            var positionsCount = Convert.ToInt32(reader["POSITIONS_COUNT"]);
            var quantity = Convert.ToInt32(reader["TOTAL_QUANTITY"]);
            var revenue = Convert.ToDecimal(reader["TOTAL_REVENUE"]);
            var averageCheck = ordersCount == 0 ? 0m : revenue / ordersCount;

            result.Add(new SalesBySellerReportRow
            {
                SellerId = Convert.ToInt32(reader["ID_USER"]),
                SellerName = reader["SELLER_NAME"]?.ToString() ?? string.Empty,
                OrdersCountValue = ordersCount,
                OrdersCount = ordersCount.ToString(),
                PositionsCountValue = positionsCount,
                PositionsCount = positionsCount.ToString(),
                QuantityValue = quantity,
                Quantity = quantity.ToString(),
                RevenueValue = revenue,
                Revenue = FormatCurrency(revenue),
                AverageCheckValue = averageCheck,
                AverageCheck = FormatCurrency(averageCheck)
            });
        }

        return result;
    }

    public IReadOnlyList<SalesByCategoryReportRow> GetSalesByCategoryReport(DateTime dateFrom, DateTime dateTo, short? categoryId)
    {
        var sql =
            $"""
            SELECT
                c.ID_CATEGORY,
                c.CATEGORY_NAME,
                COUNT(DISTINCT o.ID_ORDER) AS ORDERS_COUNT,
                COUNT(DISTINCT p.ID_PRODUCT) AS PRODUCTS_COUNT,
                SUM(od.QUANTITY) AS TOTAL_QUANTITY,
                SUM(od.QUANTITY * od.PRICE) AS TOTAL_REVENUE
            FROM ORDERS o
            INNER JOIN STATUS_ORDER so ON so.ID_STATUS = o.ID_STATUS
            INNER JOIN ORDER_DETAILS od ON od.ID_ORDER = o.ID_ORDER
            INNER JOIN PRODUCTS p ON p.ID_PRODUCT = od.ID_PRODUCT
            INNER JOIN CATEGORY c ON c.ID_CATEGORY = p.ID_CATEGORY
            WHERE o.ORDER_DATE >= @dateFrom
              AND o.ORDER_DATE < DATEADD(DAY, 1, @dateTo)
              AND ({CountedOrderStatusCondition})
              AND (@categoryId IS NULL OR c.ID_CATEGORY = @categoryId)
            GROUP BY c.ID_CATEGORY, c.CATEGORY_NAME
            ORDER BY TOTAL_REVENUE DESC, c.CATEGORY_NAME;
            """;

        var result = new List<SalesByCategoryReportRow>();
        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@dateFrom", SqlDbType.Date).Value = dateFrom.Date;
        command.Parameters.Add("@dateTo", SqlDbType.Date).Value = dateTo.Date;
        command.Parameters.Add("@categoryId", SqlDbType.SmallInt).Value = categoryId ?? (object)DBNull.Value;
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var ordersCount = Convert.ToInt32(reader["ORDERS_COUNT"]);
            var productsCount = Convert.ToInt32(reader["PRODUCTS_COUNT"]);
            var quantity = Convert.ToInt32(reader["TOTAL_QUANTITY"]);
            var revenue = Convert.ToDecimal(reader["TOTAL_REVENUE"]);
            var averagePrice = quantity == 0 ? 0m : revenue / quantity;

            result.Add(new SalesByCategoryReportRow
            {
                CategoryId = Convert.ToInt16(reader["ID_CATEGORY"]),
                CategoryName = reader["CATEGORY_NAME"]?.ToString() ?? string.Empty,
                OrdersCountValue = ordersCount,
                OrdersCount = ordersCount.ToString(),
                ProductCountValue = productsCount,
                ProductCount = productsCount.ToString(),
                QuantityValue = quantity,
                Quantity = quantity.ToString(),
                RevenueValue = revenue,
                Revenue = FormatCurrency(revenue),
                AveragePriceValue = averagePrice,
                AveragePrice = FormatCurrency(averagePrice)
            });
        }

        return result;
    }

    public IReadOnlyList<CashReportRow> GetCashReport(DateTime dateFrom, DateTime dateTo, int? sellerId)
    {
        var sql =
            $"""
            SELECT
                CAST(o.ORDER_DATE AS date) AS REPORT_DATE,
                COUNT(DISTINCT o.ID_ORDER) AS ORDERS_COUNT,
                COUNT(*) AS POSITIONS_COUNT,
                SUM(od.QUANTITY) AS TOTAL_QUANTITY,
                SUM(od.QUANTITY * od.PRICE) AS TOTAL_REVENUE
            FROM ORDERS o
            INNER JOIN STATUS_ORDER so ON so.ID_STATUS = o.ID_STATUS
            INNER JOIN ORDER_DETAILS od ON od.ID_ORDER = o.ID_ORDER
            WHERE o.ORDER_DATE >= @dateFrom
              AND o.ORDER_DATE < DATEADD(DAY, 1, @dateTo)
              AND ({CountedOrderStatusCondition})
              AND (@sellerId IS NULL OR o.ID_USER = @sellerId)
            GROUP BY CAST(o.ORDER_DATE AS date)
            ORDER BY REPORT_DATE;
            """;

        var result = new List<CashReportRow>();
        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@dateFrom", SqlDbType.Date).Value = dateFrom.Date;
        command.Parameters.Add("@dateTo", SqlDbType.Date).Value = dateTo.Date;
        command.Parameters.Add("@sellerId", SqlDbType.Int).Value = sellerId ?? (object)DBNull.Value;
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var reportDate = Convert.ToDateTime(reader["REPORT_DATE"]);
            var ordersCount = Convert.ToInt32(reader["ORDERS_COUNT"]);
            var positionsCount = Convert.ToInt32(reader["POSITIONS_COUNT"]);
            var quantity = Convert.ToInt32(reader["TOTAL_QUANTITY"]);
            var revenue = Convert.ToDecimal(reader["TOTAL_REVENUE"]);
            var averageCheck = ordersCount == 0 ? 0m : revenue / ordersCount;

            result.Add(new CashReportRow
            {
                ReportDateValue = reportDate,
                ReportDate = reportDate.ToString("dd.MM.yyyy"),
                OrdersCountValue = ordersCount,
                OrdersCount = ordersCount.ToString(),
                PositionsCountValue = positionsCount,
                PositionsCount = positionsCount.ToString(),
                QuantityValue = quantity,
                Quantity = quantity.ToString(),
                RevenueValue = revenue,
                Revenue = FormatCurrency(revenue),
                AverageCheckValue = averageCheck,
                AverageCheck = FormatCurrency(averageCheck)
            });
        }

        return result;
    }

    public IReadOnlyList<ManagementSalesTopProductRow> GetManagementSalesTopProducts(DateTime dateFrom, DateTime dateTo, int topCount = 10)
    {
        var sql =
            $"""
            SELECT TOP (@topCount)
                p.ID_PRODUCT,
                p.PRODUCT_NAME,
                c.CATEGORY_NAME,
                COUNT(DISTINCT o.ID_ORDER) AS ORDERS_COUNT,
                SUM(od.QUANTITY) AS TOTAL_QUANTITY,
                SUM(od.QUANTITY * od.PRICE) AS TOTAL_REVENUE
            FROM ORDERS o
            INNER JOIN STATUS_ORDER so ON so.ID_STATUS = o.ID_STATUS
            INNER JOIN ORDER_DETAILS od ON od.ID_ORDER = o.ID_ORDER
            INNER JOIN PRODUCTS p ON p.ID_PRODUCT = od.ID_PRODUCT
            INNER JOIN CATEGORY c ON c.ID_CATEGORY = p.ID_CATEGORY
            WHERE o.ORDER_DATE >= @dateFrom
              AND o.ORDER_DATE < DATEADD(DAY, 1, @dateTo)
              AND ({CountedOrderStatusCondition})
            GROUP BY p.ID_PRODUCT, p.PRODUCT_NAME, c.CATEGORY_NAME
            ORDER BY TOTAL_REVENUE DESC, TOTAL_QUANTITY DESC, p.PRODUCT_NAME;
            """;

        var result = new List<ManagementSalesTopProductRow>();
        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@dateFrom", SqlDbType.Date).Value = dateFrom.Date;
        command.Parameters.Add("@dateTo", SqlDbType.Date).Value = dateTo.Date;
        command.Parameters.Add("@topCount", SqlDbType.Int).Value = topCount <= 0 ? 10 : topCount;
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var ordersCount = Convert.ToInt32(reader["ORDERS_COUNT"]);
            var quantity = Convert.ToInt32(reader["TOTAL_QUANTITY"]);
            var revenue = Convert.ToDecimal(reader["TOTAL_REVENUE"]);

            result.Add(new ManagementSalesTopProductRow
            {
                ProductId = Convert.ToInt32(reader["ID_PRODUCT"]),
                ProductName = reader["PRODUCT_NAME"]?.ToString() ?? string.Empty,
                CategoryName = reader["CATEGORY_NAME"]?.ToString() ?? string.Empty,
                OrdersCountValue = ordersCount,
                OrdersCount = ordersCount.ToString(),
                QuantityValue = quantity,
                Quantity = quantity.ToString(),
                RevenueValue = revenue,
                Revenue = FormatCurrency(revenue)
            });
        }

        return result;
    }

    public IReadOnlyList<ProfitabilityReportRow> GetProfitabilityReport(DateTime dateFrom, DateTime dateTo, short? categoryId)
    {
        var sql =
            $"""
            WITH sold AS (
                SELECT
                    od.ID_PRODUCT,
                    SUM(od.QUANTITY) AS SOLD_QUANTITY,
                    SUM(od.QUANTITY * od.PRICE) AS REVENUE
                FROM ORDER_DETAILS od
                INNER JOIN ORDERS o ON o.ID_ORDER = od.ID_ORDER
                INNER JOIN STATUS_ORDER so ON so.ID_STATUS = o.ID_STATUS
                WHERE o.ORDER_DATE >= @dateFrom
                  AND o.ORDER_DATE < DATEADD(DAY, 1, @dateTo)
                  AND ({CountedOrderStatusCondition})
                GROUP BY od.ID_PRODUCT
            ),
            purchase_costs AS (
                SELECT
                    sd.ID_PRODUCT,
                    CAST(SUM(sd.QUANTITY * sd.UNIT_PRICE) AS decimal(18, 4)) / NULLIF(SUM(sd.QUANTITY), 0) AS AVG_PURCHASE_COST
                FROM SUPPLIES_DETAILS sd
                INNER JOIN SUPPLIES s ON s.ID_SUPPLIES = sd.ID_SUPPLIES
                INNER JOIN STATUS_SUPPLIES ss ON ss.ID_STATUS = s.ID_STATUS
                WHERE ({AcceptedSupplyStatusCondition})
                GROUP BY sd.ID_PRODUCT
            )
            SELECT
                p.ID_PRODUCT,
                p.PRODUCT_NAME,
                p.ID_CATEGORY,
                c.CATEGORY_NAME,
                sold.SOLD_QUANTITY,
                sold.REVENUE,
                COALESCE(pc.AVG_PURCHASE_COST, 0) AS AVG_PURCHASE_COST,
                sold.SOLD_QUANTITY * COALESCE(pc.AVG_PURCHASE_COST, 0) AS ESTIMATED_COST,
                sold.REVENUE - sold.SOLD_QUANTITY * COALESCE(pc.AVG_PURCHASE_COST, 0) AS GROSS_PROFIT,
                CASE
                    WHEN sold.REVENUE = 0 THEN 0
                    ELSE ((sold.REVENUE - sold.SOLD_QUANTITY * COALESCE(pc.AVG_PURCHASE_COST, 0)) * 100.0) / sold.REVENUE
                END AS MARGIN_PERCENT
            FROM sold
            INNER JOIN PRODUCTS p ON p.ID_PRODUCT = sold.ID_PRODUCT
            INNER JOIN CATEGORY c ON c.ID_CATEGORY = p.ID_CATEGORY
            LEFT JOIN purchase_costs pc ON pc.ID_PRODUCT = sold.ID_PRODUCT
            WHERE @categoryId IS NULL OR p.ID_CATEGORY = @categoryId
            ORDER BY GROSS_PROFIT DESC, sold.REVENUE DESC, p.PRODUCT_NAME;
            """;

        var result = new List<ProfitabilityReportRow>();
        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@dateFrom", SqlDbType.Date).Value = dateFrom.Date;
        command.Parameters.Add("@dateTo", SqlDbType.Date).Value = dateTo.Date;
        command.Parameters.Add("@categoryId", SqlDbType.SmallInt).Value = categoryId ?? (object)DBNull.Value;
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var quantitySold = Convert.ToInt32(reader["SOLD_QUANTITY"]);
            var revenue = Convert.ToDecimal(reader["REVENUE"]);
            var averagePurchaseCost = Convert.ToDecimal(reader["AVG_PURCHASE_COST"]);
            var estimatedCost = Convert.ToDecimal(reader["ESTIMATED_COST"]);
            var grossProfit = Convert.ToDecimal(reader["GROSS_PROFIT"]);
            var marginPercent = Convert.ToDecimal(reader["MARGIN_PERCENT"]);

            result.Add(new ProfitabilityReportRow
            {
                ProductId = Convert.ToInt32(reader["ID_PRODUCT"]),
                ProductName = reader["PRODUCT_NAME"]?.ToString() ?? string.Empty,
                CategoryId = Convert.ToInt16(reader["ID_CATEGORY"]),
                CategoryName = reader["CATEGORY_NAME"]?.ToString() ?? string.Empty,
                QuantitySoldValue = quantitySold,
                QuantitySold = quantitySold.ToString(),
                RevenueValue = revenue,
                Revenue = FormatCurrency(revenue),
                AveragePurchaseCostValue = averagePurchaseCost,
                AveragePurchaseCost = FormatCurrency(averagePurchaseCost),
                EstimatedCostValue = estimatedCost,
                EstimatedCost = FormatCurrency(estimatedCost),
                GrossProfitValue = grossProfit,
                GrossProfit = FormatCurrency(grossProfit),
                MarginPercentValue = marginPercent,
                MarginPercent = $"{marginPercent:0.##} %"
            });
        }

        return result;
    }

    public IReadOnlyList<LossAnalyticsReportRow> GetLossAnalyticsReport(DateTime dateFrom, DateTime dateTo)
    {
        var sql =
            $"""
            WITH purchase_costs AS (
                SELECT
                    sd.ID_PRODUCT,
                    CAST(SUM(sd.QUANTITY * sd.UNIT_PRICE) AS decimal(18, 4)) / NULLIF(SUM(sd.QUANTITY), 0) AS AVG_PURCHASE_COST
                FROM SUPPLIES_DETAILS sd
                INNER JOIN SUPPLIES s ON s.ID_SUPPLIES = sd.ID_SUPPLIES
                INNER JOIN STATUS_SUPPLIES ss ON ss.ID_STATUS = s.ID_STATUS
                WHERE ({AcceptedSupplyStatusCondition})
                GROUP BY sd.ID_PRODUCT
            )
            SELECT
                p.ID_PRODUCT,
                p.PRODUCT_NAME,
                p.ID_CATEGORY,
                c.CATEGORY_NAME,
                l.REASON,
                COUNT(*) AS OPERATIONS_COUNT,
                SUM(l.QUANTITY) AS TOTAL_QUANTITY,
                SUM(l.QUANTITY * COALESCE(pc.AVG_PURCHASE_COST, 0)) AS ESTIMATED_COST,
                MAX(CASE
                    WHEN l.REASON LIKE N'%срок%'
                      OR l.REASON LIKE N'%просроч%'
                      OR l.REASON LIKE N'%годност%'
                    THEN 1 ELSE 0 END) AS IS_EXPIRED_LOSS
            FROM LOSSES l
            INNER JOIN PRODUCTS p ON p.ID_PRODUCT = l.ID_PRODUCT
            INNER JOIN CATEGORY c ON c.ID_CATEGORY = p.ID_CATEGORY
            LEFT JOIN purchase_costs pc ON pc.ID_PRODUCT = l.ID_PRODUCT
            WHERE l.LOSS_DATE >= @dateFrom
              AND l.LOSS_DATE < DATEADD(DAY, 1, @dateTo)
            GROUP BY p.ID_PRODUCT, p.PRODUCT_NAME, p.ID_CATEGORY, c.CATEGORY_NAME, l.REASON
            ORDER BY ESTIMATED_COST DESC, TOTAL_QUANTITY DESC, p.PRODUCT_NAME;
            """;

        var result = new List<LossAnalyticsReportRow>();
        using var connection = OpenConnection();
        EnsureLossQuantityColumn(connection);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@dateFrom", SqlDbType.Date).Value = dateFrom.Date;
        command.Parameters.Add("@dateTo", SqlDbType.Date).Value = dateTo.Date;
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var operationsCount = Convert.ToInt32(reader["OPERATIONS_COUNT"]);
            var quantity = Convert.ToInt32(reader["TOTAL_QUANTITY"]);
            var estimatedCost = Convert.ToDecimal(reader["ESTIMATED_COST"]);

            result.Add(new LossAnalyticsReportRow
            {
                ProductId = Convert.ToInt32(reader["ID_PRODUCT"]),
                ProductName = reader["PRODUCT_NAME"]?.ToString() ?? string.Empty,
                CategoryId = Convert.ToInt16(reader["ID_CATEGORY"]),
                CategoryName = reader["CATEGORY_NAME"]?.ToString() ?? string.Empty,
                Reason = reader["REASON"]?.ToString() ?? string.Empty,
                OperationsCountValue = operationsCount,
                OperationsCount = operationsCount.ToString(),
                QuantityValue = quantity,
                Quantity = quantity.ToString(),
                EstimatedCostValue = estimatedCost,
                EstimatedCost = FormatCurrency(estimatedCost),
                IsExpiredLoss = Convert.ToInt32(reader["IS_EXPIRED_LOSS"]) == 1
            });
        }

        return result;
    }

    public IReadOnlyList<CustomerReturnAnalyticsRow> GetCustomerReturnAnalyticsReport(DateTime dateFrom, DateTime dateTo)
    {
        var sql =
            $"""
            SELECT
                p.ID_PRODUCT,
                p.PRODUCT_NAME,
                p.ID_CATEGORY,
                c.CATEGORY_NAME,
                COUNT(DISTINCT s.ID_SUPPLIES) AS RETURNS_COUNT,
                SUM(sd.QUANTITY) AS TOTAL_QUANTITY,
                SUM(sd.QUANTITY * sd.UNIT_PRICE) AS TOTAL_AMOUNT
            FROM SUPPLIES s
            INNER JOIN SUPPLIERS sup ON sup.ID_SUPPLIER = s.ID_SUPPLIER
            INNER JOIN STATUS_SUPPLIES ss ON ss.ID_STATUS = s.ID_STATUS
            INNER JOIN SUPPLIES_DETAILS sd ON sd.ID_SUPPLIES = s.ID_SUPPLIES
            INNER JOIN PRODUCTS p ON p.ID_PRODUCT = sd.ID_PRODUCT
            INNER JOIN CATEGORY c ON c.ID_CATEGORY = p.ID_CATEGORY
            WHERE s.CREATION_DATE >= @dateFrom
              AND s.CREATION_DATE < DATEADD(DAY, 1, @dateTo)
              AND sup.SUPPLIER_NAME = @supplierName
              AND ({AcceptedSupplyStatusCondition})
            GROUP BY p.ID_PRODUCT, p.PRODUCT_NAME, p.ID_CATEGORY, c.CATEGORY_NAME
            ORDER BY TOTAL_AMOUNT DESC, TOTAL_QUANTITY DESC, p.PRODUCT_NAME;
            """;

        var result = new List<CustomerReturnAnalyticsRow>();
        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@dateFrom", SqlDbType.Date).Value = dateFrom.Date;
        command.Parameters.Add("@dateTo", SqlDbType.Date).Value = dateTo.Date;
        command.Parameters.Add("@supplierName", SqlDbType.VarChar, 4000).Value = CustomerReturnSupplierName;
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var returnsCount = Convert.ToInt32(reader["RETURNS_COUNT"]);
            var quantity = Convert.ToInt32(reader["TOTAL_QUANTITY"]);
            var amount = Convert.ToDecimal(reader["TOTAL_AMOUNT"]);

            result.Add(new CustomerReturnAnalyticsRow
            {
                ProductId = Convert.ToInt32(reader["ID_PRODUCT"]),
                ProductName = reader["PRODUCT_NAME"]?.ToString() ?? string.Empty,
                CategoryId = Convert.ToInt16(reader["ID_CATEGORY"]),
                CategoryName = reader["CATEGORY_NAME"]?.ToString() ?? string.Empty,
                ReturnsCountValue = returnsCount,
                ReturnsCount = returnsCount.ToString(),
                QuantityValue = quantity,
                Quantity = quantity.ToString(),
                AmountValue = amount,
                Amount = FormatCurrency(amount)
            });
        }

        return result;
    }

}
