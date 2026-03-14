using Dapper;
using Npgsql;
using RetailInventory.Application.Customers.DTOs;
using RetailInventory.Application.Interfaces;

namespace RetailInventory.Infrastructure.Repositories;

public class CustomerQueryRepository : ICustomerQueryRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public CustomerQueryRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<int> CountAsync(string? search = null)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        if (string.IsNullOrWhiteSpace(search))
            return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM customers");
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM customers WHERE first_name ILIKE @Search OR last_name ILIKE @Search OR email ILIKE @Search",
            new { Search = $"%{search}%" });
    }

    public async Task<List<CustomerDto>> GetPagedAsync(int skip, int take, string? sortBy, string? sortDirection, string? search = null)
    {
        var orderByColumn = sortBy?.ToLower() switch
        {
            "firstname" => "first_name",
            "email"     => "email",
            _           => "last_name"
        };

        var direction = sortDirection?.ToLower() == "desc" ? "DESC" : "ASC";

        var whereClause = string.IsNullOrWhiteSpace(search)
            ? ""
            : "WHERE first_name ILIKE @Search OR last_name ILIKE @Search OR email ILIKE @Search";

        var sql = $"""
            SELECT
                id          AS "Id",
                first_name  AS "FirstName",
                last_name   AS "LastName",
                email       AS "Email"
            FROM customers
            {whereClause}
            ORDER BY {orderByColumn} {direction}
            LIMIT @Take OFFSET @Skip
            """;

        await using var conn = await _dataSource.OpenConnectionAsync();
        var searchParam = string.IsNullOrWhiteSpace(search)
            ? (object)new { Take = take, Skip = skip }
            : new { Take = take, Skip = skip, Search = $"%{search}%" };
        var results = await conn.QueryAsync<CustomerDto>(sql, searchParam);
        return results.ToList();
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id)
    {
        const string sql = """
            SELECT
                id          AS "Id",
                first_name  AS "FirstName",
                last_name   AS "LastName",
                email       AS "Email"
            FROM customers
            WHERE id = @Id
            """;

        await using var conn = await _dataSource.OpenConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<CustomerDto>(sql, new { Id = id });
    }

    public async Task<CustomerDto?> GetByEmailAsync(string email)
    {
        const string sql = """
            SELECT
                id          AS "Id",
                first_name  AS "FirstName",
                last_name   AS "LastName",
                email       AS "Email"
            FROM customers
            WHERE LOWER(email) = LOWER(@Email)
            """;

        await using var conn = await _dataSource.OpenConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<CustomerDto>(sql, new { Email = email });
    }
}
