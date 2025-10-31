# Product Stock System

A .NET 10 demo solution featuring a product stock management system with both a Web API and an MCP (Model Context Protocol) stdio server.

## Projects Structure

### ProductStockSystem.Models
Shared data models and DTOs used by both the API and MCP server:
- `Product`, `StockMovement` entities
- `ProductDto`, `StockMovementDto`, `CreateProductRequest`, `UpdateStockRequest` DTOs
- `StockMovementType` enum

### ProductStockSystem.Api
ASP.NET Core minimal API for product stock management:
- **Products**: CRUD operations, low stock detection
- **Stock**: Stock movements (in/out/adjustments)
- **Health**: API health check endpoint
- Uses Entity Framework Core In-Memory database for demo purposes
- Includes sample data seeding

### ProductStockSystem.McpServer
MCP stdio server that wraps the API calls:
- Uses ModelContextProtocol NuGet package v0.3.0-preview.4
- Implements Microsoft's hosting pattern with dependency injection
- Provides 7 MCP tools for stock management

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/products` | Get all products |
| GET | `/api/products/{id}` | Get product by ID |
| GET | `/api/products/low-stock` | Get low stock products |
| POST | `/api/products` | Create new product |
| PUT | `/api/products/{id}/stock` | Update stock levels |
| GET | `/api/products/{id}/movements` | Get stock movements for product |
| GET | `/api/movements` | Get all stock movements |
| GET | `/api/health` | Health check |

## MCP Tools

| Tool | Description | Parameters |
|------|-------------|------------|
| `list_products` | Get all active products | None |
| `get_product` | Get specific product by ID | `productId` (int) |
| `list_low_stock_products` | Get products at/below reorder level | None |
| `create_product` | Create new product | `name`, `sku`, `price`, `initialStock`, `reorderLevel`, `description?` |
| `update_stock` | Update stock (in/out/adjustment) | `productId`, `movementType`, `quantity`, `notes?` |
| `get_stock_movements` | Get stock movement history | `productId` |
| `check_api_health` | Check if API is healthy | None |

## Getting Started

### Prerequisites
- .NET 10 SDK (preview)
- PowerShell (for test script)

### Running the API
```bash
cd src/ProductStockSystem.Api
dotnet run
```
The API will start on `http://localhost:5033` (or check console output for actual port)

### Running the MCP Server
Option 1 - Using PowerShell script:
```powershell
.\test-mcp-server.ps1
```

Option 2 - Manual:
```bash
$env:STOCK_API_URL = "http://localhost:5033"  # Set API URL
cd src/ProductStockSystem.McpServer
dotnet run
```

### Configuration
The MCP server uses the environment variable `STOCK_API_URL` to configure the API endpoint:
- Default: `http://localhost:5033` (matches development API port)
- For production: Set to your actual API URL (e.g., `https://your-api.azurewebsites.net`)

**Development**: The MCP server now defaults to the same port the API uses in development, so it should work out-of-the-box without additional configuration.

## Sample Data
The API includes sample products:
1. **Wireless Mouse** (SKU: WM001) - 50 units, reorder at 10
2. **USB Keyboard** (SKU: KB002) - 5 units, reorder at 10 (low stock)
3. **Monitor Stand** (SKU: MS003) - 25 units, reorder at 5

## Architecture Highlights

### Modern .NET Features
- ✅ Minimal APIs with route groups
- ✅ Entity Framework Core 10 with in-memory database
- ✅ Global exception handling
- ✅ Structured logging
- ✅ Microsoft's hosting pattern for MCP server

### MCP Server Best Practices
- ✅ Uses latest ModelContextProtocol SDK (0.3.0-preview.4)
- ✅ Proper `[McpServerToolType]` and `[McpServerTool]` attributes
- ✅ Dependency injection with `AddMcpServer().WithStdioServerTransport().WithTools<T>()`
- ✅ Structured error handling and JSON responses
- ✅ HTTP client factory for API calls

### Production Considerations
- 🔄 Replace in-memory database with SQL Server/PostgreSQL
- 🔄 Add authentication and authorization
- 🔄 Implement proper error handling and retry policies
- 🔄 Add comprehensive logging and monitoring
- 🔄 Configure HTTPS and CORS for production deployment

## Example Usage

### Testing API
```bash
# Get all products
curl http://localhost:5033/api/products

# Create a new product
curl -X POST http://localhost:5033/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Product",
    "sku": "TEST001", 
    "price": 29.99,
    "initialStock": 100,
    "reorderLevel": 15,
    "description": "A test product"
  }'

# Update stock (Stock In = 1, Stock Out = 2, Adjustment = 3)
curl -X PUT http://localhost:5033/api/products/1/stock \
  -H "Content-Type: application/json" \
  -d '{
    "movementType": 1,
    "quantity": 25,
    "notes": "Restock from supplier"
  }'
```

### Using MCP Tools
The MCP server communicates via stdio protocol. Tools return structured JSON responses with success/error information and relevant data.

## Built With
- .NET 10 (preview)
- ASP.NET Core Minimal APIs
- Entity Framework Core 10
- ModelContextProtocol SDK 0.3.0-preview.4
- Microsoft.Extensions.Hosting 8.0.1
- Microsoft.Extensions.Http 8.0.1