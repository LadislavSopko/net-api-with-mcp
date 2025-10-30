# Zero.Mcp.Extensions

Enables ASP.NET Core API controllers to function as MCP (Model Context Protocol) server tools with flexible authorization support.

## Features

- ✅ Turn attributed controllers into MCP tools automatically
- ✅ Support for `ActionResult<T>` unwrapping (including null values)
- ✅ Flexible authorization integration via `IAuthForMcpSupplier`
- ✅ Pre-filter authorization checks before tool execution
- ✅ Support for [AllowAnonymous] override
- ✅ Support for multiple [Authorize] attributes (all enforced)
- ✅ Name-based parameter binding from JSON
- ✅ Simple 3-step integration

## Installation

```bash
dotnet add package Zero.Mcp.Extensions
```

## Quick Start

### Step 1: Attribute Your Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
[McpServerToolType]
[Authorize]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    [McpServerTool]
    public async Task<ActionResult<User>> GetById(Guid id) { ... }

    [HttpGet("public")]
    [McpServerTool]
    [AllowAnonymous]  // Override class-level [Authorize]
    public async Task<ActionResult<User>> GetPublicInfo() { ... }
}
```

### Step 2: Implement IAuthForMcpSupplier

```csharp
public class MyAuthSupplier : IAuthForMcpSupplier
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthorizationService _authorizationService;

    public MyAuthSupplier(
        IHttpContextAccessor httpContextAccessor,
        IAuthorizationService authorizationService)
    {
        _httpContextAccessor = httpContextAccessor;
        _authorizationService = authorizationService;
    }

    public async Task<bool> CheckAuthenticatedAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.User?.Identity?.IsAuthenticated ?? false;
    }

    public async Task<bool> CheckPolicyAsync(AuthorizeAttribute attribute)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null || string.IsNullOrEmpty(attribute.Policy))
            return false;

        var result = await _authorizationService.AuthorizeAsync(
            httpContext.User, null, attribute.Policy);

        return result.Succeeded;
    }
}
```

### Step 3: Configure in Program.cs

```csharp
// Register your auth supplier
builder.Services.AddScoped<IAuthForMcpSupplier, MyAuthSupplier>();

// Configure MCP server with options
builder.Services.AddZeroMcpExtensions(options =>
{
    options.RequireAuthentication = true;  // Require auth for MCP endpoint
    options.UseAuthorization = true;       // Use [Authorize] policies
    options.McpEndpointPath = "/mcp";      // MCP endpoint path
});

// Map MCP endpoint (uses configuration from above)
app.MapZeroMcp();
```

## Configuration Options

```csharp
public class ZeroMcpOptions
{
    // Whether to require authentication for MCP endpoints (default: true)
    public bool RequireAuthentication { get; set; } = true;

    // Whether to use authorization policies (default: true)
    // When false, [Authorize] attributes are ignored
    public bool UseAuthorization { get; set; } = true;

    // The path where the MCP endpoint will be mapped (default: "/mcp")
    public string McpEndpointPath { get; set; } = "/mcp";

    // The assembly to scan for MCP tools (default: calling assembly)
    public Assembly? ToolAssembly { get; set; }

    // JSON serializer options (default: snake_case_lower)
    public JsonSerializerOptions? SerializerOptions { get; set; }
}
```

### Examples

**Without Authentication:**
```csharp
builder.Services.AddZeroMcpExtensions(options =>
{
    options.RequireAuthentication = false;
    options.UseAuthorization = false;
});
```

**Custom Endpoint Path:**
```csharp
builder.Services.AddZeroMcpExtensions(options =>
{
    options.McpEndpointPath = "/api/mcp";
});
```

**Explicit Assembly:**
```csharp
builder.Services.AddZeroMcpExtensions(options =>
{
    options.ToolAssembly = typeof(MyController).Assembly;
});
```

## License

MIT
