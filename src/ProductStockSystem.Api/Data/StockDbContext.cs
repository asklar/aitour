using Microsoft.EntityFrameworkCore;
using ProductStockSystem.Models;

namespace ProductStockSystem.Api.Data;

public class StockDbContext : DbContext
{
    public StockDbContext(DbContextOptions<StockDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Product entity
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Sku).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.HasIndex(e => e.Sku).IsUnique();
        });

        // Configure StockMovement entity
        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasOne(e => e.Product)
                  .WithMany(p => p.StockMovements)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed some initial data - Zava DIY home improvement products
        var now = DateTime.UtcNow;
        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Name = "Cordless Power Drill",
                Description = "18V cordless drill with 2 batteries and charger",
                Sku = "DRILL-001",
                Price = 89.99m,
                StockQuantity = 15,
                ReorderLevel = 5,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Product
            {
                Id = 2,
                Name = "Interior Paint - White",
                Description = "Premium interior latex paint, 1 gallon, white",
                Sku = "PAINT-001",
                Price = 34.99m,
                StockQuantity = 3,
                ReorderLevel = 10,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Product
            {
                Id = 3,
                Name = "Garden Rake",
                Description = "Heavy-duty steel garden rake with wood handle",
                Sku = "RAKE-001",
                Price = 24.99m,
                StockQuantity = 8,
                ReorderLevel = 3,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        );

        // Seed some stock movement history
        modelBuilder.Entity<StockMovement>().HasData(
            new StockMovement
            {
                Id = 1,
                ProductId = 1,
                MovementType = StockMovementType.StockIn,
                Quantity = 20,
                Notes = "Initial stock",
                CreatedAt = now.AddDays(-7)
            },
            new StockMovement
            {
                Id = 2,
                ProductId = 1,
                MovementType = StockMovementType.StockOut,
                Quantity = 5,
                Notes = "Weekend DIY sale",
                CreatedAt = now.AddDays(-3)
            },
            new StockMovement
            {
                Id = 3,
                ProductId = 2,
                MovementType = StockMovementType.StockIn,
                Quantity = 10,
                Notes = "Initial stock",
                CreatedAt = now.AddDays(-5)
            },
            new StockMovement
            {
                Id = 4,
                ProductId = 2,
                MovementType = StockMovementType.StockOut,
                Quantity = 7,
                Notes = "Contractor bulk order",
                CreatedAt = now.AddDays(-2)
            },
            new StockMovement
            {
                Id = 5,
                ProductId = 3,
                MovementType = StockMovementType.StockIn,
                Quantity = 15,
                Notes = "Initial stock",
                CreatedAt = now.AddDays(-6)
            },
            new StockMovement
            {
                Id = 6,
                ProductId = 3,
                MovementType = StockMovementType.StockOut,
                Quantity = 7,
                Notes = "Spring gardening season sales",
                CreatedAt = now.AddDays(-1)
            }
        );
    }
}