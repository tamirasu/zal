using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Persistence;

public class OrderFlowContext : DbContext
{
    private readonly bool _enableLogging;

    public OrderFlowContext(bool enableLogging = false)
    {
        _enableLogging = enableLogging;
    }

    public DbSet<Product>   Products   => Set<Product>();
    public DbSet<Customer>  Customers  => Set<Customer>();
    public DbSet<Order>     Orders     => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            options.UseSqlite("Data Source=orderflow.db");
            if (_enableLogging)
                options.LogTo(System.Console.WriteLine, LogLevel.Information);
        }
    }

    protected override void OnModelCreating(ModelBuilder m)
    {
        // ── Product ──────────────────────────────────────────────────────────
        m.Entity<Product>(e =>
        {
            e.Property(p => p.Price).HasPrecision(18, 2);
            e.Ignore(p => p.Description);  // [JsonIgnore][XmlIgnore] — nie w bazie
        });

        // ── Customer ─────────────────────────────────────────────────────────
        m.Entity<Customer>(e =>
        {
            e.Property(c => c.Name)
             .HasColumnName("FullName")
             .IsRequired();
            e.HasIndex(c => c.Name)
             .HasDatabaseName("IX_Customer_FullName");

            e.Property(c => c.Email).IsRequired(false);
        });

        // ── Order ─────────────────────────────────────────────────────────────
        m.Entity<Order>(e =>
        {
            e.Ignore(o => o.TotalAmount);   // właściwość obliczana
            e.HasIndex(o => o.Status)
             .HasDatabaseName("IX_Order_Status");

            e.Property(o => o.Notes).IsRequired(false);

            e.HasOne(o => o.Customer)
             .WithMany(c => c.Orders)
             .HasForeignKey(o => o.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── OrderItem ─────────────────────────────────────────────────────────
        m.Entity<OrderItem>(e =>
        {
            e.Ignore(i => i.TotalPrice);    // właściwość obliczana
            e.Property(i => i.UnitPrice).HasPrecision(18, 2);

            e.HasOne(i => i.Product)
             .WithMany(p => p.OrderItems)
             .HasForeignKey(i => i.ProductId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne<Order>()
             .WithMany(o => o.Items)
             .HasForeignKey(i => i.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

// Fabryka design-time — pozwala dotnet ef znaleźć kontekst
public class OrderFlowContextFactory : IDesignTimeDbContextFactory<OrderFlowContext>
{
    public OrderFlowContext CreateDbContext(string[] args) => new OrderFlowContext();
}
