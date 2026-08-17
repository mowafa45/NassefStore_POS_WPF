using Microsoft.EntityFrameworkCore;
using NassefStore.Data.Entities;
using System.IO;

namespace NassefStore.Data;

public class AppDbContext : DbContext
{
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<SaleItem> SaleItems { get; set; }
    public DbSet<Purchase> Purchases { get; set; }
    public DbSet<PurchaseItem> PurchaseItems { get; set; }
    public DbSet<Return> Returns { get; set; }
    public DbSet<CreditPayment> CreditPayments { get; set; }
    public DbSet<SupplierPayment> SupplierPayments { get; set; }
    public DbSet<StockAlert> StockAlerts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NassefStore", "nassef.db");

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        options.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Decimal precision
        modelBuilder.Entity<Product>(e => {
            e.Property(p => p.CostPrice).HasColumnType("decimal(18,2)");
            e.Property(p => p.SellPrice).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Sale>(e => {
            e.Property(p => p.TotalAmount).HasColumnType("decimal(18,2)");
            e.Property(p => p.DiscountAmount).HasColumnType("decimal(18,2)");
            e.Property(p => p.NetAmount).HasColumnType("decimal(18,2)");
            e.Property(p => p.PaidAmount).HasColumnType("decimal(18,2)");
            e.Property(p => p.RemainingAmount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<SaleItem>(e => {
            e.Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(p => p.TotalPrice).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Purchase>(e => {
            e.Property(p => p.TotalAmount).HasColumnType("decimal(18,2)");
            e.Property(p => p.PaidAmount).HasColumnType("decimal(18,2)");
            e.Property(p => p.RemainingAmount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<PurchaseItem>(e => {
            e.Property(p => p.UnitCost).HasColumnType("decimal(18,2)");
            e.Property(p => p.TotalCost).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Return>(e => {
            e.Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(p => p.TotalPrice).HasColumnType("decimal(18,2)");
        });

        // Seed default categories
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Hardware", Description = "Hinges, Screws, Hammers, Screwdrivers" },
            new Category { Id = 2, Name = "Plumbing", Description = "Pipes, Fittings, Valves" },
            new Category { Id = 3, Name = "Electrical", Description = "Wires, Cables, Sockets, Switches" },
            new Category { Id = 4, Name = "Lighting", Description = "Bulbs, LED, Fixtures" },
            new Category { Id = 5, Name = "Building Materials", Description = "Cement tools, Paint brushes, etc." },
            new Category { Id = 6, Name = "Other", Description = "Miscellaneous items" }
        );
    }
}
