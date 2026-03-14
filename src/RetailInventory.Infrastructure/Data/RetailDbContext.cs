using Microsoft.EntityFrameworkCore;
using RetailInventory.Domain;

namespace RetailInventory.Infrastructure.Data;

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
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasIndex(p => p.SKU).IsUnique();
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");
            entity.HasIndex(c => c.Email).IsUnique();
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasOne(o => o.Customer).WithMany().HasForeignKey(o => o.CustomerId);
            entity.Property(o => o.Status).HasConversion<int>();
            entity.HasIndex(o => o.Status).IncludeProperties(o => o.TotalAmount);
            entity.HasIndex(o => o.CreatedAt);
            entity.HasIndex(o => o.CustomerId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasOne(oi => oi.Order).WithMany(o => o.OrderItems).HasForeignKey(oi => oi.OrderId);
            entity.HasOne(oi => oi.Product).WithMany().HasForeignKey(oi => oi.ProductId);
            entity.HasIndex(oi => oi.OrderId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Source).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Payload).IsRequired().HasColumnType("jsonb");
            entity.Property(x => x.OccurredAtUtc).IsRequired();
            entity.Property(x => x.PublishedAtUtc);
            entity.HasIndex(x => x.PublishedAtUtc);
        });
    }
}
