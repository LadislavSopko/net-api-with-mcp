# Zero.Mcp.Extensions

Enables ASP.NET Core API controllers to function as MCP (Model Context Protocol) server tools with flexible authorization support.

## Features

- Turn attributed controllers into MCP tools automatically
- Support for `ActionResult<T>` unwrapping (including null values)
- Flexible authorization integration via `IAuthForMcpSupplier`
- Role-based tool visibility filtering (tools/list respects permissions)
- **NEW v2.1.0:** Configurable tool naming conventions (MethodOnly, ControllerPrefix)
- **NEW v2.1.0:** `IMcpRequestContext` for MCP call detection and header access
- **NEW v2.1.0:** Automatic `x-mcp-call` header injection via middleware
- Support for [AllowAnonymous] override
- Support for multiple [Authorize] attributes (all enforced)
- Name-based parameter binding from JSON
- Simple 3-step integration

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
    [McpServerTool]  // Tool name: "get_by_id"
    public async Task<ActionResult<User>> GetById(Guid id) { ... }

    [HttpPost]
    [McpServerTool(Name = "create_user")]  // Explicit name override
    public async Task<ActionResult<User>> Create(CreateUserRequest request) { ... }

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

// Mark MCP requests (adds x-mcp-call header) - BEFORE authentication
app.UseZeroMcpMarking();
app.UseAuthentication();
app.UseAuthorization();

// Map MCP endpoint
app.MapZeroMcp();
```

## Configuration Options

```csharp
public class ZeroMcpOptions
{
    // Whether to require authentication for MCP endpoints (default: true)
    public bool RequireAuthentication { get; set; } = true;

    // Whether to use authorization policies (default: true)
    public bool UseAuthorization { get; set; } = true;

    // The path where the MCP endpoint will be mapped (default: "/mcp")
    public string McpEndpointPath { get; set; } = "/mcp";

    // The assembly to scan for MCP tools (default: calling assembly)
    public Assembly? ToolAssembly { get; set; }

    // JSON serializer options (default: snake_case_lower)
    public JsonSerializerOptions? SerializerOptions { get; set; }

    // NEW v2.1.0: Tool naming convention (default: MethodOnly)
    public ToolNamingConvention NamingConvention { get; set; } = ToolNamingConvention.MethodOnly;

    // NEW v2.1.0: Separator for controller prefix (default: "_")
    public string ToolNameSeparator { get; set; } = "_";
}
```

## Tool Naming Conventions (v2.1.0)

Control how tool names are generated from controller methods:

### MethodOnly (Default)
```csharp
// UsersController.GetById() -> "get_by_id"
// ProductsController.GetById() -> "get_by_id"  (collision!)
options.NamingConvention = ToolNamingConvention.MethodOnly;
```

### ControllerPrefix
```csharp
// UsersController.GetById() -> "users_get_by_id"
// ProductsController.GetById() -> "products_get_by_id"  (no collision)
options.NamingConvention = ToolNamingConvention.ControllerPrefix;
```

### Explicit Name Override
The `[McpServerTool(Name = "...")]` attribute always takes priority:

```csharp
[McpServerTool(Name = "fetch_user")]  // Always "fetch_user" regardless of convention
public async Task<ActionResult<User>> GetById(Guid id) { ... }
```

### Custom Separator
```csharp
options.NamingConvention = ToolNamingConvention.ControllerPrefix;
options.ToolNameSeparator = "-";  // "users-get-by-id" instead of "users_get_by_id"
```

## MCP Request Context (v2.1.0)

Detect MCP calls and access headers in your application code:

### Setup
```csharp
// In Program.cs - BEFORE authentication middleware
app.UseZeroMcpMarking();  // Marks MCP requests with x-mcp-call header
app.UseAuthentication();
app.UseAuthorization();
```

### Usage in Controllers
```csharp
public class UsersController : ControllerBase
{
    private readonly IMcpRequestContext _mcpContext;

    public UsersController(IMcpRequestContext mcpContext)
    {
        _mcpContext = mcpContext;
    }

    [HttpGet("{id}")]
    [McpServerTool]
    public async Task<ActionResult<User>> GetById(Guid id)
    {
        if (_mcpContext.IsMcpCall)
        {
            // Called via MCP - maybe return different format
            _logger.LogInformation("MCP call detected");
        }

        // Access MCP headers
        var customHeader = _mcpContext.GetHeader("x-custom-header");

        return Ok(user);
    }
}
```

### IMcpRequestContext Interface
```csharp
public interface IMcpRequestContext
{
    // True if this request came through the MCP endpoint
    bool IsMcpCall { get; }

    // Get a specific header value (returns null if not MCP call)
    string? GetHeader(string name);

    // Access all headers (returns null if not MCP call)
    IHeaderDictionary? Headers { get; }
}
```

## Examples

**Without Authentication:**
```csharp
builder.Services.AddZeroMcpExtensions(options =>
{
    options.RequireAuthentication = false;
    options.UseAuthorization = false;
});
```

**Multiple Controllers with Same Method Names:**
```csharp
builder.Services.AddZeroMcpExtensions(options =>
{
    options.NamingConvention = ToolNamingConvention.ControllerPrefix;
});
// UsersController.GetAll() -> "users_get_all"
// ProductsController.GetAll() -> "products_get_all"
```

**Custom Endpoint Path:**
```csharp
builder.Services.AddZeroMcpExtensions(options =>
{
    options.McpEndpointPath = "/api/mcp";
});

// Don't forget to match in middleware
app.UseZeroMcpMarking("/api/mcp");
```

## Architecture

The library follows a clean architecture with clear separation of concerns:

- **Zero.Mcp.Extensions**: Core library with no HttpContext dependency
- **IAuthForMcpSupplier**: Interface that host implements for auth integration
- **IMcpRequestContext**: Interface for detecting MCP calls in application code
- **Host Application**: Provides implementations with access to HttpContext

This design allows the library to remain **completely decoupled** from ASP.NET Core infrastructure while still supporting flexible authentication and authorization.

## How It Works

1. **Discovery**: Library scans for classes marked with `[McpServerToolType]`
2. **Registration**: Methods marked with `[McpServerTool]` are registered as MCP tools
3. **Naming**: Tool names generated based on `NamingConvention` or explicit `Name` attribute
4. **MCP Marking**: Middleware adds `x-mcp-call` header to MCP requests
5. **Authorization Pre-Filter**: Before each tool invocation, checks `[Authorize]` attributes
6. **Tool Filtering**: `tools/list` only returns tools the user is authorized to invoke
7. **Execution**: Invokes controller method if authorized
8. **Unwrapping**: Extracts value from `ActionResult<T>` for MCP serialization

## Security

- **Role-Based Tool Visibility**: `tools/list` respects user permissions
- **Multiple [Authorize] Enforcement**: ALL `[Authorize]` attributes are enforced
- **[AllowAnonymous] Support**: Method-level `[AllowAnonymous]` overrides class-level `[Authorize]`
- **Attribute Inheritance**: Inherits authorization attributes from base classes
- **Pre-Filter Checks**: Authorization verified BEFORE controller instantiation

## Best Practices

1. **Use ControllerPrefix** for multiple controllers with similar method names
2. **Register IAuthForMcpSupplier as Scoped**: Ensures proper lifecycle management
3. **Call UseZeroMcpMarking() early**: Before authentication middleware
4. **Use Policy-Based Authorization**: More flexible than role-based
5. **Test Authorization**: Write tests to verify auth behavior
6. **Handle Null Values**: Controllers can return `Ok(null)` for nullable types

## Troubleshooting

**Problem**: Duplicate tool names
**Solution**: Use `ToolNamingConvention.ControllerPrefix` or explicit `[McpServerTool(Name = "...")]`

**Problem**: `IsMcpCall` always returns false
**Solution**: Ensure `app.UseZeroMcpMarking()` is called BEFORE authentication middleware

**Problem**: Tools not discovered
**Solution**: Ensure `[McpServerToolType]` is on class and `[McpServerTool]` is on methods

**Problem**: Authorization always fails
**Solution**: Verify `IAuthForMcpSupplier` is registered and implementation is correct

**Problem**: Wrong assembly scanned
**Solution**: Explicitly set `options.ToolAssembly = typeof(YourController).Assembly`

## Changelog

### v2.1.0
- Tool naming conventions (`MethodOnly`, `ControllerPrefix`) for generic controllers
- `IMcpRequestContext` for header access and MCP call detection
- `UseZeroMcpMarking()` middleware for automatic `x-mcp-call` header injection
- `[McpServerTool(Name = "...")]` explicit tool naming support

### v2.0.0
- Role-based tool filtering - `tools/list` only returns authorized tools
- Professional configuration system with `ZeroMcpOptions`
- `IUserRoleResolver` for custom role resolution

### v1.0.0
- Initial release with controller-to-MCP-tool conversion
- `ActionResult<T>` unwrapping
- Authorization pre-filter support

## License

Apache-2.0
