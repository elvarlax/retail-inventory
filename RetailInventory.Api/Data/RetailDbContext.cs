using Microsoft.EntityFrameworkCore;
using RetailInventory.Api.Models;

namespace RetailInventory.Api.Data;

public class RetailDbContext : DbContext
{
    public RetailDbContext(DbContextOptions<RetailDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasIndex(p => p.ExternalId).IsUnique();
            entity.HasIndex(p => p.SKU).IsUnique();
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");
            entity.HasIndex(c => c.ExternalId).IsUnique();
            entity.HasIndex(c => c.Email).IsUnique();
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasOne(o => o.Customer).WithMany().HasForeignKey(o => o.CustomerId);
            entity.Property(o => o.Status).HasConversion<int>();
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasOne(oi => oi.Order).WithMany(o => o.OrderItems).HasForeignKey(oi => oi.OrderId);
            entity.HasOne(oi => oi.Product).WithMany().HasForeignKey(oi => oi.ProductId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasIndex(u => u.Email).IsUnique();
        });
    }
}