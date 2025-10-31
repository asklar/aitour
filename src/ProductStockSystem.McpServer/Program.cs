using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using ProductStockSystem.McpServer.Services;
using ProductStockSystem.McpServer.Tools;

var builder = Host.CreateApplicationBuilder(args);

// Disable logging for MCP stdio protocol compatibility
builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.None);

// Register HTTP client and services
builder.Services.AddHttpClient<StockApiService>(client =>
{
    var stockApiBaseUrl = Environment.GetEnvironmentVariable("STOCK_API_URL") ?? "http://localhost:5033";
    client.BaseAddress = new Uri(stockApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add MCP server with stdio transport and tools
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<ProductStockTools>();

var host = builder.Build();

try
{
    // Check API health at startup silently
    var stockApiService = host.Services.GetRequiredService<StockApiService>();
    await stockApiService.IsApiHealthyAsync();

    await host.RunAsync();
}
catch
{
    // Exit silently on error to avoid interfering with MCP protocol
    Environment.Exit(1);
}
