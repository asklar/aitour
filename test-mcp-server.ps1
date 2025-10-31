# Test script for MCP Server
# Note: The MCP server now defaults to http://localhost:5033, so no environment variable needed
# But you can still override it if needed:
# $env:STOCK_API_URL = "http://localhost:5033"

Write-Host "Starting Product Stock MCP Server..." -ForegroundColor Green
Write-Host "Using default API URL: http://localhost:5033" -ForegroundColor Yellow
Write-Host "To override, set STOCK_API_URL environment variable" -ForegroundColor Cyan

# Run the MCP server
Set-Location "src\ProductStockSystem.McpServer"
dotnet run