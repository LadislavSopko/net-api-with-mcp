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

**Custom JSON Serialization:**
```csharp
builder.Services.AddZeroMcpExtensions(options =>
{
    options.SerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
});
```

## Architecture

The library follows a clean architecture with clear separation of concerns:

- **Zero.Mcp.Extensions**: Core library with no HttpContext dependency
- **IAuthForMcpSupplier**: Interface that host implements for auth integration
- **Host Application**: Provides `IAuthForMcpSupplier` implementation with access to HttpContext

This design allows the library to remain **completely decoupled** from ASP.NET Core infrastructure while still supporting flexible authentication and authorization.

## How It Works

1. **Discovery**: Library scans for classes marked with `[McpServerToolType]`
2. **Registration**: Methods marked with `[McpServerTool]` are registered as MCP tools
3. **Authorization Pre-Filter**: Before each tool invocation, checks `[Authorize]` attributes
4. **Execution**: Invokes controller method if authorized
5. **Unwrapping**: Extracts value from `ActionResult<T>` for MCP serialization
6. **Error Handling**: Throws exception for error results (NotFound, BadRequest, etc.)

## Security

- **Multiple [Authorize] Enforcement**: ALL `[Authorize]` attributes are enforced (not just first)
- **[AllowAnonymous] Support**: Method-level `[AllowAnonymous]` overrides class-level `[Authorize]`
- **Attribute Inheritance**: Inherits authorization attributes from base classes
- **Pre-Filter Checks**: Authorization verified BEFORE controller instantiation

## Best Practices

1. **Register IAuthForMcpSupplier as Scoped**: Ensures proper lifecycle management
2. **Use Policy-Based Authorization**: More flexible than role-based
3. **Test Authorization**: Write tests to verify auth behavior
4. **Handle Null Values**: Controllers can return `Ok(null)` for nullable types
5. **Error Results**: Return appropriate error results (NotFound, BadRequest) - they're converted to exceptions

## Troubleshooting

**Problem**: Tools not discovered
**Solution**: Ensure `[McpServerToolType]` is on class and `[McpServerTool]` is on methods

**Problem**: Authorization always fails
**Solution**: Verify `IAuthForMcpSupplier` is registered and implementation is correct

**Problem**: "IAuthForMcpSupplier is not registered" error
**Solution**: Either register `IAuthForMcpSupplier` or set `UseAuthorization = false`

**Problem**: Wrong assembly scanned
**Solution**: Explicitly set `options.ToolAssembly = typeof(YourController).Assembly`

## License

Apache-2.0
