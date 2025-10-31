using System.ComponentModel;
using ModelContextProtocol.Server;
using ProductStockSystem.McpServer.Models;
using ProductStockSystem.McpServer.Services;
using ProductStockSystem.Models;

namespace ProductStockSystem.McpServer.Tools;

[McpServerToolType]
public class ProductStockTools
{
    private readonly StockApiService _stockApi;

    public ProductStockTools(StockApiService stockApi)
    {
        _stockApi = stockApi;
    }

    [McpServerTool(Name = "list_products", UseStructuredContent = true)]
    [Description("Get a list of all active products in the stock system")]
    public async Task<ProductsListResult> ListProductsAsync()
    {
        try
        {
            var products = await _stockApi.GetProductsAsync();

            var productResults = products.Select(p => new ProductResult(
                p.Id,
                p.Name,
                p.Description,
                p.Sku,
                p.Price,
                p.StockQuantity,
                p.ReorderLevel,
                p.IsLowStock
            )).ToList();

            return new ProductsListResult(true, productResults, products.Count);
        }
        catch (Exception ex)
        {
            return new ProductsListResult(false, [], 0, $"Error retrieving products: {ex.Message}");
        }
    }

    [McpServerTool(Name = "get_product", UseStructuredContent = true)]
    [Description("Get details of a specific product by ID")]
    public async Task<ProductDetailResult> GetProductAsync(
        [Description("The ID of the product to retrieve")] int productId)
    {
        try
        {
            var product = await _stockApi.GetProductByIdAsync(productId);

            if (product == null)
            {
                return new ProductDetailResult(false, null, $"Product with ID {productId} not found");
            }

            var productResult = new ProductResult(
                product.Id,
                product.Name,
                product.Description,
                product.Sku,
                product.Price,
                product.StockQuantity,
                product.ReorderLevel,
                product.IsLowStock
            );

            return new ProductDetailResult(true, productResult);
        }
        catch (Exception ex)
        {
            return new ProductDetailResult(false, null, $"Error retrieving product {productId}: {ex.Message}");
        }
    }

    [McpServerTool(Name = "list_low_stock_products", UseStructuredContent = true)]
    [Description("Get a list of products that are at or below their reorder level")]
    public async Task<LowStockProductsResult> ListLowStockProductsAsync()
    {
        try
        {
            var products = await _stockApi.GetLowStockProductsAsync();

            var lowStockResults = products.Select(p => new LowStockProductResult(
                p.Id,
                p.Name,
                p.Sku,
                p.StockQuantity,
                p.ReorderLevel,
                p.ReorderLevel - p.StockQuantity
            )).ToList();

            return new LowStockProductsResult(true, lowStockResults, products.Count);
        }
        catch (Exception ex)
        {
            return new LowStockProductsResult(false, [], 0, $"Error retrieving low stock products: {ex.Message}");
        }
    }

    [McpServerTool(Name = "create_product", UseStructuredContent = true)]
    [Description("Create a new product in the stock system")]
    public async Task<ProductCreationResult> CreateProductAsync(
        [Description("The name of the product")] string name,
        [Description("The SKU (Stock Keeping Unit) for the product")] string sku,
        [Description("The price of the product")] decimal price,
        [Description("The initial stock quantity")] int initialStock,
        [Description("The reorder level for the product")] int reorderLevel,
        [Description("The description of the product")] string description = "")
    {
        try
        {
            var request = new CreateProductRequest
            {
                Name = name,
                Description = description,
                Sku = sku,
                Price = price,
                InitialStock = initialStock,
                ReorderLevel = reorderLevel
            };

            var product = await _stockApi.CreateProductAsync(request);

            var productResult = new ProductResult(
                product.Id,
                product.Name,
                product.Description,
                product.Sku,
                product.Price,
                product.StockQuantity,
                product.ReorderLevel,
                product.IsLowStock
            );

            return new ProductCreationResult(
                true,
                $"Product '{name}' created successfully",
                productResult
            );
        }
        catch (Exception ex)
        {
            return new ProductCreationResult(false, null, null, $"Error creating product: {ex.Message}");
        }
    }

    [McpServerTool(Name = "update_stock", UseStructuredContent = true)]
    [Description("Update the stock quantity for a product (stock in, stock out, or adjustment)")]
    public async Task<StockUpdateResult> UpdateStockAsync(
        [Description("The ID of the product to update")] int productId,
        [Description("The type of stock movement: 1=StockIn, 2=StockOut, 3=Adjustment")] int movementType,
        [Description("The quantity to add, remove, or set (for adjustment)")] int quantity,
        [Description("Notes about the stock movement")] string notes = "")
    {
        try
        {
            if (!Enum.IsDefined(typeof(StockMovementType), movementType))
            {
                return new StockUpdateResult(
                    false,
                    null,
                    null,
                    null,
                    "Invalid movement type. Use 1=StockIn, 2=StockOut, 3=Adjustment"
                );
            }

            var request = new UpdateStockRequest
            {
                MovementType = (StockMovementType)movementType,
                Quantity = quantity,
                Notes = notes
            };

            var product = await _stockApi.UpdateStockAsync(productId, request);

            var movementTypeName = ((StockMovementType)movementType).ToString();

            var movement = new MovementInfo(movementTypeName, quantity, notes);
            var updatedProduct = new UpdatedProductInfo(
                product.Id,
                product.Name,
                product.Sku,
                product.StockQuantity,
                product.ReorderLevel,
                product.IsLowStock
            );

            return new StockUpdateResult(
                true,
                $"Stock updated successfully for product '{product.Name}'",
                movement,
                updatedProduct
            );
        }
        catch (Exception ex)
        {
            return new StockUpdateResult(false, null, null, null, $"Error updating stock for product {productId}: {ex.Message}");
        }
    }

    [McpServerTool(Name = "get_stock_movements", UseStructuredContent = true)]
    [Description("Get the stock movement history for a specific product")]
    public async Task<StockMovementsResult> GetStockMovementsAsync(
        [Description("The ID of the product to get movements for")] int productId)
    {
        try
        {
            var movements = await _stockApi.GetProductMovementsAsync(productId);

            var movementResults = movements.Select(m => new StockMovementResult(
                m.Id,
                m.ProductName,
                m.MovementType.ToString(),
                m.Quantity,
                m.Notes,
                m.CreatedAt
            )).ToList();

            return new StockMovementsResult(true, productId, movementResults, movements.Count);
        }
        catch (Exception ex)
        {
            return new StockMovementsResult(false, productId, [], 0, $"Error retrieving stock movements for product {productId}: {ex.Message}");
        }
    }

    [McpServerTool(Name = "check_api_health", UseStructuredContent = true)]
    [Description("Check if the stock API is healthy and responding")]
    public async Task<ApiHealthResult> CheckApiHealthAsync()
    {
        try
        {
            var isHealthy = await _stockApi.IsApiHealthyAsync();

            var message = isHealthy
                ? "Stock API is healthy and responding"
                : "Stock API is not responding";

            return new ApiHealthResult(true, isHealthy, DateTime.UtcNow, message);
        }
        catch (Exception ex)
        {
            return new ApiHealthResult(false, false, DateTime.UtcNow, "Failed to check API health", $"Error checking API health: {ex.Message}");
        }
    }
}