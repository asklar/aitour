using ProductStockSystem.Models;

namespace ProductStockSystem.McpServer.Models;

// Base result class
public abstract record BaseResult(bool Success, string? Error = null);

// Product-related result classes
public record ProductResult(
    int Id,
    string Name,
    string Description,
    string Sku,
    decimal Price,
    int StockQuantity,
    int ReorderLevel,
    bool IsLowStock
);

public record ProductsListResult(
    bool Success,
    IReadOnlyList<ProductResult> Products,
    int Count,
    string? Error = null
) : BaseResult(Success, Error);

public record ProductDetailResult(
    bool Success,
    ProductResult? Product = null,
    string? Error = null
) : BaseResult(Success, Error);

public record LowStockProductResult(
    int Id,
    string Name,
    string Sku,
    int StockQuantity,
    int ReorderLevel,
    int Shortfall
);

public record LowStockProductsResult(
    bool Success,
    IReadOnlyList<LowStockProductResult> LowStockProducts,
    int Count,
    string? Error = null
) : BaseResult(Success, Error);

// Product creation result
public record ProductCreationResult(
    bool Success,
    string? Message = null,
    ProductResult? Product = null,
    string? Error = null
) : BaseResult(Success, Error);

// Stock movement related result classes
public record StockMovementResult(
    int Id,
    string ProductName,
    string MovementType,
    int Quantity,
    string Notes,
    DateTime CreatedAt
);

public record StockMovementsResult(
    bool Success,
    int ProductId,
    IReadOnlyList<StockMovementResult> Movements,
    int Count,
    string? Error = null
) : BaseResult(Success, Error);

public record MovementInfo(
    string Type,
    int Quantity,
    string Notes
);

public record UpdatedProductInfo(
    int Id,
    string Name,
    string Sku,
    int NewStockQuantity,
    int ReorderLevel,
    bool IsLowStock
);

public record StockUpdateResult(
    bool Success,
    string? Message = null,
    MovementInfo? Movement = null,
    UpdatedProductInfo? UpdatedProduct = null,
    string? Error = null
) : BaseResult(Success, Error);

// API health result
public record ApiHealthResult(
    bool Success,
    bool ApiHealthy,
    DateTime Timestamp,
    string Message,
    string? Error = null
) : BaseResult(Success, Error);