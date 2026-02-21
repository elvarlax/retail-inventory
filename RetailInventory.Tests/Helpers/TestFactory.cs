using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RetailInventory.Api.Data;

namespace RetailInventory.Tests.Helpers;

public static class TestDbFactory
{
    public static (RetailDbContext Db, SqliteConnection Connection) CreateSqliteInMemoryDb()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<RetailDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new RetailDbContext(options);
        db.Database.EnsureCreated();

        return (db, connection);
    }
}