using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using ProductStockSystem.Api.Data;
using ProductStockSystem.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();

// Add Entity Framework with In-Memory database
builder.Services.AddDbContext<StockDbContext>(options =>
    options.UseInMemoryDatabase("StockSystemDb"));

// Add CORS for API access
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<StockDbContext>();
    context.Database.EnsureCreated();
}

// Product endpoints
app.MapGet("/api/products", async (StockDbContext context) =>
{
    var products = await context.Products
        .Where(p => p.IsActive)
        .Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Sku = p.Sku,
            Price = p.Price,
            StockQuantity = p.StockQuantity,
            ReorderLevel = p.ReorderLevel,
            IsActive = p.IsActive
        })
        .ToListAsync();

    return Results.Ok(products);
})
.WithName("GetProducts")
.WithSummary("Get all active products");

app.MapGet("/api/products/{id:int}", async (int id, StockDbContext context) =>
{
    var product = await context.Products
        .Where(p => p.Id == id && p.IsActive)
        .Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Sku = p.Sku,
            Price = p.Price,
            StockQuantity = p.StockQuantity,
            ReorderLevel = p.ReorderLevel,
            IsActive = p.IsActive
        })
        .FirstOrDefaultAsync();

    return product is not null ? Results.Ok(product) : Results.NotFound();
})
.WithName("GetProductById")
.WithSummary("Get a product by ID");

app.MapGet("/api/products/low-stock", async (StockDbContext context) =>
{
    var lowStockProducts = await context.Products
        .Where(p => p.IsActive && p.StockQuantity <= p.ReorderLevel)
        .Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Sku = p.Sku,
            Price = p.Price,
            StockQuantity = p.StockQuantity,
            ReorderLevel = p.ReorderLevel,
            IsActive = p.IsActive
        })
        .ToListAsync();

    return Results.Ok(lowStockProducts);
})
.WithName("GetLowStockProducts")
.WithSummary("Get products with low stock levels");

app.MapPost("/api/products", async (CreateProductRequest request, StockDbContext context) =>
{
    // Check if SKU already exists
    if (await context.Products.AnyAsync(p => p.Sku == request.Sku))
    {
        return Results.BadRequest($"Product with SKU '{request.Sku}' already exists.");
    }

    var now = DateTime.UtcNow;
    var product = new Product
    {
        Name = request.Name,
        Description = request.Description,
        Sku = request.Sku,
        Price = request.Price,
        StockQuantity = request.InitialStock,
        ReorderLevel = request.ReorderLevel,
        IsActive = true,
        CreatedAt = now,
        UpdatedAt = now
    };

    context.Products.Add(product);
    await context.SaveChangesAsync();

    // Add initial stock movement if initial stock > 0
    if (request.InitialStock > 0)
    {
        var stockMovement = new StockMovement
        {
            ProductId = product.Id,
            MovementType = StockMovementType.StockIn,
            Quantity = request.InitialStock,
            Notes = "Initial stock",
            CreatedAt = now
        };
        context.StockMovements.Add(stockMovement);
        await context.SaveChangesAsync();
    }

    var productDto = new ProductDto
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Sku = product.Sku,
        Price = product.Price,
        StockQuantity = product.StockQuantity,
        ReorderLevel = product.ReorderLevel,
        IsActive = product.IsActive
    };

    return Results.Created($"/api/products/{product.Id}", productDto);
})
.WithName("CreateProduct")
.WithSummary("Create a new product");

app.MapPut("/api/products/{id:int}/stock", async (int id, UpdateStockRequest request, StockDbContext context) =>
{
    var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
    if (product is null)
    {
        return Results.NotFound();
    }

    var now = DateTime.UtcNow;

    // Calculate new stock quantity
    var newQuantity = request.MovementType switch
    {
        StockMovementType.StockIn => product.StockQuantity + request.Quantity,
        StockMovementType.StockOut => product.StockQuantity - request.Quantity,
        StockMovementType.Adjustment => request.Quantity,
        _ => product.StockQuantity
    };

    // Validate stock levels
    if (newQuantity < 0)
    {
        return Results.BadRequest("Stock quantity cannot be negative.");
    }

    // Update product stock
    product.StockQuantity = newQuantity;
    product.UpdatedAt = now;

    // Record stock movement
    var stockMovement = new StockMovement
    {
        ProductId = id,
        MovementType = request.MovementType,
        Quantity = request.Quantity,
        Notes = request.Notes,
        CreatedAt = now
    };

    context.StockMovements.Add(stockMovement);
    await context.SaveChangesAsync();

    var productDto = new ProductDto
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Sku = product.Sku,
        Price = product.Price,
        StockQuantity = product.StockQuantity,
        ReorderLevel = product.ReorderLevel,
        IsActive = product.IsActive
    };

    return Results.Ok(productDto);
})
.WithName("UpdateProductStock")
.WithSummary("Update product stock quantity");

// Stock movements endpoints
app.MapGet("/api/products/{id:int}/movements", async (int id, StockDbContext context) =>
{
    var movements = await context.StockMovements
        .Where(sm => sm.ProductId == id)
        .Include(sm => sm.Product)
        .OrderByDescending(sm => sm.CreatedAt)
        .Select(sm => new StockMovementDto
        {
            Id = sm.Id,
            ProductId = sm.ProductId,
            ProductName = sm.Product.Name,
            MovementType = sm.MovementType,
            Quantity = sm.Quantity,
            Notes = sm.Notes,
            CreatedAt = sm.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(movements);
})
.WithName("GetProductMovements")
.WithSummary("Get stock movements for a product");

app.MapGet("/api/movements", async (StockDbContext context) =>
{
    var movements = await context.StockMovements
        .Include(sm => sm.Product)
        .OrderByDescending(sm => sm.CreatedAt)
        .Select(sm => new StockMovementDto
        {
            Id = sm.Id,
            ProductId = sm.ProductId,
            ProductName = sm.Product.Name,
            MovementType = sm.MovementType,
            Quantity = sm.Quantity,
            Notes = sm.Notes,
            CreatedAt = sm.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(movements);
})
.WithName("GetAllMovements")
.WithSummary("Get all stock movements");

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
.WithName("HealthCheck")
.WithSummary("Health check endpoint");

app.Run();
