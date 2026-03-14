using Dapper;
using Npgsql;
using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Products.DTOs;

namespace RetailInventory.Infrastructure.Repositories;

public class ProductQueryRepository : IProductQueryRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public ProductQueryRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<int> CountAsync(string? search = null)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        if (string.IsNullOrWhiteSpace(search))
            return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM products");
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM products WHERE name ILIKE @Search",
            new { Search = $"%{search}%" });
    }

    public async Task<List<ProductDto>> GetPagedAsync(int skip, int take, string? sortBy, string? sortDirection, string? search = null)
    {
        var orderByColumn = sortBy?.ToLower() switch
        {
            "price" => "price",
            "sku" => "sku",
            "stockquantity" => "stock_quantity",
            _ => "name"
        };

        var direction = sortDirection?.ToLower() == "desc" ? "DESC" : "ASC";

        var whereClause = string.IsNullOrWhiteSpace(search) ? "" : "WHERE name ILIKE @Search";

        // Column names and direction are from a whitelist — not user input — so safe to interpolate.
        // All pagination values are parameterized.
        var sql = $"""
            SELECT
                id          AS "Id",
                name        AS "Name",
                sku         AS "SKU",
                image_url   AS "ImageUrl",
                stock_quantity AS "StockQuantity",
                price       AS "Price"
            FROM products
            {whereClause}
            ORDER BY {orderByColumn} {direction}
            LIMIT @Take OFFSET @Skip
            """;

        await using var conn = await _dataSource.OpenConnectionAsync();
        var searchParam = string.IsNullOrWhiteSpace(search)
            ? (object)new { Take = take, Skip = skip }
            : new { Take = take, Skip = skip, Search = $"%{search}%" };
        var results = await conn.QueryAsync<ProductDto>(sql, searchParam);
        return results.ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        const string sql = """
            SELECT
                id          AS "Id",
                name        AS "Name",
                sku         AS "SKU",
                image_url   AS "ImageUrl",
                stock_quantity AS "StockQuantity",
                price       AS "Price"
            FROM products
            WHERE id = @Id
            """;

        await using var conn = await _dataSource.OpenConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<ProductDto>(sql, new { Id = id });
    }
}
