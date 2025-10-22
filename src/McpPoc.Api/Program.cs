using McpPoc.Api.Services;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register user service
builder.Services.AddSingleton<IUserService, UserService>();

// Add HTTP context accessor (might be needed)
builder.Services.AddHttpContextAccessor();

// ============================================
// TEST: Add MCP Server with HTTP transport
// ============================================
builder.Services
    .AddMcpServer()
    .WithHttpTransport()  // HTTP transport for full pipeline
    .WithToolsFromAssembly();   // Should discover controller with [McpServerToolType]

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
