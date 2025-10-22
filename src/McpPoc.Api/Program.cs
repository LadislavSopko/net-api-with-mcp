using McpPoc.Api.Extensions;
using McpPoc.Api.Services;
using Serilog;

// Configure Serilog for file logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File("logs/mcppoc-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog
builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register user service
builder.Services.AddSingleton<IUserService, UserService>();

// ============================================
// TEST: Add MCP Server with HTTP transport
// ============================================
builder.Services
    .AddMcpServer()
    .WithHttpTransport()  // HTTP transport for full pipeline
    .WithToolsFromAssemblyUnwrappingActionResult();  // Custom extension that unwraps ActionResult<T>

var app = builder.Build();

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// ============================================
// TEST: Map MCP endpoint
// ============================================
app.MapMcp("/mcp");  // Expose MCP at /mcp

app.Logger.LogInformation("===========================================");
app.Logger.LogInformation("TEST: MCP + Controller Integration");
app.Logger.LogInformation("HTTP API: http://localhost:5001/api/users");
app.Logger.LogInformation("MCP Endpoint: http://localhost:5001/mcp");
app.Logger.LogInformation("Swagger: http://localhost:5001/swagger");
app.Logger.LogInformation("===========================================");

app.Run();

// Make Program accessible for WebApplicationFactory
public partial class Program { }
