using Dapper;
using Npgsql;
using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Orders.DTOs;
using RetailInventory.Domain;

namespace RetailInventory.Infrastructure.Repositories;

public class OrderQueryRepository : IOrderQueryRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public OrderQueryRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<int> CountAsync(OrderStatus? status, Guid? customerId, string? search = null)
    {
        var hasSearch = !string.IsNullOrWhiteSpace(search);
        var join = hasSearch ? "JOIN customers c ON c.id = o.customer_id" : "";
        var sql = $"SELECT COUNT(*)::int FROM orders o {join} WHERE 1=1";
        if (status.HasValue)     sql += " AND o.status = @Status";
        if (customerId.HasValue) sql += " AND o.customer_id = @CustomerId";
        if (hasSearch)           sql += " AND (c.first_name ILIKE @Search OR c.last_name ILIKE @Search OR c.email ILIKE @Search)";

        await using var conn = await _dataSource.OpenConnectionAsync();
        return await conn.ExecuteScalarAsync<int>(sql, new { Status = (int?)status, CustomerId = customerId, Search = hasSearch ? $"%{search}%" : null });
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id)
    {
        const string orderSql = """
            SELECT
                o.id                                        AS "Id",
                o.customer_id                               AS "CustomerId",
                c.first_name || ' ' || c.last_name          AS "CustomerName",
                CASE o.status
                    WHEN 0 THEN 'Pending'
                    WHEN 1 THEN 'Completed'
                    WHEN 2 THEN 'Cancelled'
                END                                         AS "Status",
                o.total_amount                              AS "TotalAmount",
                o.created_at                                AS "CreatedAt"
            FROM orders o
            JOIN customers c ON c.id = o.customer_id
            WHERE o.id = @Id
            """;

        const string itemsSql = """
            SELECT
                oi.product_id   AS "ProductId",
                p.name          AS "ProductName",
                oi.quantity     AS "Quantity",
                oi.unit_price   AS "UnitPrice"
            FROM order_items oi
            JOIN products p ON p.id = oi.product_id
            WHERE oi.order_id = @Id
            """;

        await using var conn = await _dataSource.OpenConnectionAsync();

        var order = await conn.QuerySingleOrDefaultAsync<OrderDto>(orderSql, new { Id = id });
        if (order == null) return null;

        var items = await conn.QueryAsync<OrderItemDto>(itemsSql, new { Id = id });
        order.Items = items.ToList();

        return order;
    }

    public async Task<List<OrderDto>> GetPagedAsync(
        int skip, int take, OrderStatus? status, string? sortBy, string? sortDirection, Guid? customerId, string? search = null)
    {
        var orderByColumn = sortBy?.ToLower() switch
        {
            "totalamount" => "o.total_amount",
            "status"      => "o.status",
            _             => "o.created_at"
        };

        var direction = sortDirection?.ToLower() == "asc" ? "ASC" : "DESC";

        var hasSearch = !string.IsNullOrWhiteSpace(search);

        var whereClauses = new List<string>();
        if (status.HasValue)     whereClauses.Add("o.status = @Status");
        if (customerId.HasValue) whereClauses.Add("o.customer_id = @CustomerId");
        if (hasSearch)           whereClauses.Add("(c.first_name ILIKE @Search OR c.last_name ILIKE @Search OR c.email ILIKE @Search)");
        var where = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        // orderByColumn and direction come from a whitelist — safe to interpolate.
        var sql = $"""
            SELECT
                o.id                                        AS "Id",
                o.customer_id                               AS "CustomerId",
                c.first_name || ' ' || c.last_name          AS "CustomerName",
                CASE o.status
                    WHEN 0 THEN 'Pending'
                    WHEN 1 THEN 'Completed'
                    WHEN 2 THEN 'Cancelled'
                END                                         AS "Status",
                o.total_amount                              AS "TotalAmount",
                o.created_at                                AS "CreatedAt"
            FROM orders o
            JOIN customers c ON c.id = o.customer_id
            {where}
            ORDER BY {orderByColumn} {direction}
            LIMIT @Take OFFSET @Skip
            """;

        await using var conn = await _dataSource.OpenConnectionAsync();

        var orders = (await conn.QueryAsync<OrderDto>(sql, new
        {
            Status = (int?)status,
            CustomerId = customerId,
            Search = hasSearch ? $"%{search}%" : null,
            Take = take,
            Skip = skip
        })).ToList();

        if (orders.Count == 0) return orders;

        var orderIds = orders.Select(o => o.Id).ToArray();

        const string itemsSql = """
            SELECT
                oi.order_id     AS "OrderId",
                oi.product_id   AS "ProductId",
                p.name          AS "ProductName",
                oi.quantity     AS "Quantity",
                oi.unit_price   AS "UnitPrice"
            FROM order_items oi
            JOIN products p ON p.id = oi.product_id
            WHERE oi.order_id = ANY(@OrderIds)
            """;

        var itemRows = await conn.QueryAsync<OrderItemRow>(itemsSql, new { OrderIds = orderIds });

        var itemsByOrderId = itemRows
            .GroupBy(r => r.OrderId)
            .ToDictionary(g => g.Key, g => g.Select(r => new OrderItemDto
            {
                ProductId = r.ProductId,
                ProductName = r.ProductName,
                Quantity = r.Quantity,
                UnitPrice = r.UnitPrice
            }).ToList());

        foreach (var order in orders)
            order.Items = itemsByOrderId.GetValueOrDefault(order.Id, []);

        return orders;
    }

    public async Task<OrderSummaryDto> GetSummaryAsync()
    {
        const string sql = """
            SELECT
                status          AS "Status",
                COUNT(*)::int   AS "Count",
                COALESCE(SUM(total_amount), 0) AS "Revenue"
            FROM orders
            GROUP BY status
            """;

        await using var conn = await _dataSource.OpenConnectionAsync();
        var rows = (await conn.QueryAsync<StatusRow>(sql)).ToList();

        var byStatus = rows.ToDictionary(r => r.Status);

        var pending   = byStatus.GetValueOrDefault(0);
        var completed = byStatus.GetValueOrDefault(1);
        var cancelled = byStatus.GetValueOrDefault(2);

        return new OrderSummaryDto
        {
            TotalOrders     = rows.Sum(r => r.Count),
            PendingOrders   = pending?.Count     ?? 0,
            CompletedOrders = completed?.Count   ?? 0,
            CancelledOrders = cancelled?.Count   ?? 0,
            TotalRevenue    = completed?.Revenue ?? 0m,
            PendingRevenue  = pending?.Revenue   ?? 0m
        };
    }

    public async Task<List<TopProductDto>> GetTopProductsAsync(int limit)
    {
        const string sql = """
            SELECT
                p.id            AS "ProductId",
                p.name          AS "ProductName",
                p.sku           AS "SKU",
                SUM(oi.quantity)::int                   AS "UnitsSold",
                SUM(oi.quantity * oi.unit_price)        AS "Revenue"
            FROM order_items oi
            JOIN products p ON p.id = oi.product_id
            JOIN orders o ON o.id = oi.order_id
            WHERE o.status = 1
            GROUP BY p.id, p.name, p.sku
            ORDER BY SUM(oi.quantity * oi.unit_price) DESC
            LIMIT @Limit
            """;

        await using var conn = await _dataSource.OpenConnectionAsync();
        var rows = await conn.QueryAsync<TopProductDto>(sql, new { Limit = limit });
        return rows.ToList();
    }

    private record OrderItemRow(Guid OrderId, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);
    private record StatusRow(int Status, int Count, decimal Revenue);
}
