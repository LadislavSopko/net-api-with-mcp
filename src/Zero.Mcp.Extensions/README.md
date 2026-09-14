# Zero.Mcp.Extensions

Enables ASP.NET Core API controllers to function as MCP (Model Context Protocol) server tools. Mark a controller with the official SDK attributes, and the library exposes its actions as MCP tools, unwraps `ActionResult<T>`, and lets the SDK enforce the controller's own `[Authorize]` rules.

Built on the official [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) **ModelContextProtocol 2.2.0** (Streamable HTTP, stateless by default). Version **3.0.0** is a breaking release: see [Migration from 2.x](#migration-from-2x).

## Features

- Turn attributed controllers into MCP tools automatically using the SDK's own `[McpServerToolType]` / `[McpServerTool]` (`ModelContextProtocol.Server`)
- `ActionResult<T>` unwrapping (including `Ok(null)` for nullable types) - the SDK's `WithToolsFromAssembly` does not do this
- SDK-native authorization: `[Authorize]`, policies and `[AllowAnonymous]` on your controllers are enforced by the SDK `AddAuthorizationFilters()` through your `IAuthorizationService`
- Per-user `tools/list` filtering and JSON-RPC error on forbidden `tools/call`
- Full passthrough of the SDK tool attribute: `Name`, `Title`, `ReadOnly` / `Destructive` / `Idempotent` / `OpenWorld` hints, `IconSource`, `UseStructuredContent`, `OutputSchemaType`
- Structured content and output schema for tools that opt in
- Optional `tools/list` cache hints (`ttlMs` + `cacheScope`) via `ToolsListTimeToLive`
- Streamable HTTP with configurable `SessionMode` (stateless by default, no `Mcp-Session-Id`)
- Configurable tool naming conventions (`MethodOnly`, `ControllerPrefix`)
- `IMcpRequestContext` for MCP call detection and header access inside controllers
- Automatic `x-mcp-call` header injection via `UseZeroMcpMarking()` middleware
- Constructor dependency injection for controllers (one instance per tool call, from the request scope)
- Name-based parameter binding from JSON (snake_case by default)
- Simple 3-step integration

## Installation

```bash
dotnet add package Zero.Mcp.Extensions
```

Requirements: .NET 10, `ModelContextProtocol` 2.2.0 (`ModelContextProtocol`, `ModelContextProtocol.Core`, `ModelContextProtocol.AspNetCore` are pulled in transitively).

## Quick Start

### Step 1: Attribute Your Controllers

The attributes are the SDK's. Since 3.0.0 the library ships no attribute types of its own.

```csharp
using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Server;   // [McpServerToolType], [McpServerTool]

[ApiController]
[Route("api/[controller]")]
[McpServerToolType]
[Authorize]  // Enforced for MCP calls too
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    [McpServerTool(Name = "UserGetById", UseStructuredContent = true, OutputSchemaType = typeof(User))]
    [Description("Gets a user by ID")]   // Tool name: "UserGetById" (explicit); would be "get_by_id" without Name
    public async Task<ActionResult<User>> GetById(int id) { ... }

    [HttpPost]
    [McpServerTool]                      // Tool name: "create"
    [Description("Creates a new user")]
    [Authorize(Policy = "RequireMember")] // Policy-based access
    public async Task<ActionResult<User>> Create(CreateUserRequest request) { ... }

    [HttpGet("public")]
    [McpServerTool]                      // Tool name: "get_public_info"
    [AllowAnonymous]                     // Overrides class-level [Authorize]
    public ActionResult<object> GetPublicInfo() { ... }
}
```

`[McpServerTool]` supports the SDK members `Name`, `Title`, `Destructive`, `Idempotent`, `OpenWorld`, `ReadOnly`, `UseStructuredContent`, `OutputSchemaType` and `IconSource`; all of them flow to the client. Note that the SDK only emits `outputSchema` when `UseStructuredContent = true`, so set both together as in the example above.

### Step 2: Register Authentication and Authorization

The library has no auth abstraction of its own. Configure ASP.NET Core authentication as usual and register your policies with `AddAuthorization(...)`; the SDK authorization filters evaluate the controller attributes through the host `IAuthorizationService` / `IAuthorizationPolicyProvider`.

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { ... });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireMember", p => p.RequireRole("member", "manager", "admin"));
});
```

### Step 3: Configure in Program.cs

```csharp
// Configure MCP server with options (returns the SDK IMcpServerBuilder)
builder.Services.AddZeroMcpExtensions(options =>
{
    options.RequireAuthentication = true;  // /mcp requires an authenticated caller
    options.UseAuthorization = true;       // SDK filters enforce [Authorize]/[AllowAnonymous]/policies
    options.McpEndpointPath = "/mcp";      // MCP endpoint path
    options.ToolsListTimeToLive = TimeSpan.FromMinutes(5);  // optional tools/list cache hint
});

var app = builder.Build();

// Mark MCP requests (adds x-mcp-call header) - BEFORE authentication
app.UseZeroMcpMarking();
app.UseAuthentication();
app.UseAuthorization();

// Map MCP endpoint (Streamable HTTP, stateless by default)
app.MapZeroMcp();
```

That's it. Your API now speaks MCP at `/mcp`.

## Configuration Options

```csharp
using ModelContextProtocol.AspNetCore;   // HttpServerSessionMode

public class ZeroMcpOptions
{
    // Whether to require authentication for the MCP endpoint (default: true)
    public bool RequireAuthentication { get; set; } = true;

    // Attach [Authorize]/[AllowAnonymous] metadata to tools and register the SDK
    // AddAuthorizationFilters(): tools/list is filtered per user and forbidden tools/call
    // is rejected. Requires the host to call AddAuthorization(). When false, no authorization
    // metadata is attached, no filters are registered, every tool is listed and callable.
    // (default: true)
    public bool UseAuthorization { get; set; } = true;

    // The path where the MCP endpoint will be mapped (default: "/mcp")
    public string McpEndpointPath { get; set; } = "/mcp";

    // The assembly to scan for MCP tools (default: null = calling assembly)
    public Assembly? ToolAssembly { get; set; }

    // JSON serializer options for parameters and results (default: null = snake_case_lower)
    public JsonSerializerOptions? SerializerOptions { get; set; }

    // Tool naming convention (default: MethodOnly)
    public ToolNamingConvention NamingConvention { get; set; } = ToolNamingConvention.MethodOnly;

    // Separator for controller prefix (default: "_")
    public string ToolNameSeparator { get; set; } = "_";

    // When set, tools/list responses carry ttlMs = value and
    // cacheScope = "private" if UseAuthorization, otherwise "public" (default: null = no hints)
    public TimeSpan? ToolsListTimeToLive { get; set; }

    // Streamable HTTP session mode passed to the SDK transport (default: Stateless)
    // Stateless: no Mcp-Session-Id, every request self-contained, load-balancer friendly
    // Stateful / StatefulForInitializeClients: session header issued by the SDK
    public HttpServerSessionMode SessionMode { get; set; } = HttpServerSessionMode.Stateless;
}
```

`AddZeroMcpExtensions` returns the SDK `IMcpServerBuilder`, so further SDK configuration (prompts, resources, extra filters) can be chained on the result.

## Tool Naming Conventions

Control how tool names are generated from controller methods. Names are snake_case and an `Async` suffix is stripped.

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
[McpServerTool(Name = "UserGetById")]  // Always "UserGetById" regardless of convention
public async Task<ActionResult<User>> GetById(int id) { ... }
```

### Custom Separator
```csharp
options.NamingConvention = ToolNamingConvention.ControllerPrefix;
options.ToolNameSeparator = "-";  // "users-get-by-id" instead of "users_get_by_id"
```

## MCP Request Context

Detect MCP calls and access request headers in your application code. This works inside tools under the default stateless mode: the SDK flows the HTTP request context into each tool invocation, so `IHttpContextAccessor` (and therefore `IMcpRequestContext`) sees the originating request.

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
    public async Task<ActionResult<User>> GetById(int id)
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
    options.UseAuthorization = false;   // every tool listed and callable, no [Authorize] evaluation
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

**tools/list Cache Hints:**
```csharp
builder.Services.AddZeroMcpExtensions(options =>
{
    options.ToolsListTimeToLive = TimeSpan.FromMinutes(5);
});
// tools/list result: "ttlMs": 300000, "cacheScope": "private" (UseAuthorization = true)
//                                      "cacheScope": "public"  (UseAuthorization = false)
```

**Stateful Sessions:**
```csharp
using ModelContextProtocol.AspNetCore;

builder.Services.AddZeroMcpExtensions(options =>
{
    options.SessionMode = HttpServerSessionMode.Stateful;   // SDK issues Mcp-Session-Id
});
```

**Structured Content with Output Schema:**
```csharp
[McpServerTool(Name = "UserGetById", UseStructuredContent = true, OutputSchemaType = typeof(User))]
public async Task<ActionResult<User>> GetById(int id) { ... }
// tools/list advertises outputSchema for User; tools/call returns structuredContent
```

**Tool Hints, Title and Icon:**
```csharp
[McpServerTool(Name = "delete_user", Title = "Delete user", Destructive = true, Idempotent = true,
               IconSource = "https://example.com/icons/delete.svg")]
public async Task<ActionResult<bool>> Delete(int id) { ... }
```

## Architecture

The library is a thin layer over the official MCP C# SDK:

- **Zero.Mcp.Extensions**: assembly scanning, tool naming, `ActionResult<T>` unwrapping, tool option derivation, cache hints, request-context marking
- **ModelContextProtocol SDK**: attributes, protocol, Streamable HTTP transport, tool invocation, authorization filters
- **ASP.NET Core**: authentication, `IAuthorizationService` / `IAuthorizationPolicyProvider` with your policies
- **IMcpRequestContext**: interface for detecting MCP calls in application code

There is no library-specific authorization abstraction to implement. The controllers' existing `[Authorize]`, policy and `[AllowAnonymous]` attributes are the single source of truth for both HTTP and MCP.

## How It Works

1. **Discovery**: Library scans `ToolAssembly` for classes marked with the SDK `[McpServerToolType]`
2. **Registration**: Methods marked with `[McpServerTool]` are registered as `McpServerTool` singletons
3. **Naming**: Tool names generated based on `NamingConvention` or explicit `Name` attribute
4. **Tool creation**: Each method is wrapped with `AIFunctionFactory.Create` plus a custom `MarshalResult`, then `McpServerTool.Create(aiFunction, McpServerToolCreateOptions)`; the options carry `Description`, `Title`, hints, `OutputSchema`, `Icons` and the tool metadata
5. **Metadata**: Metadata mirrors the SDK reflection layout `[MethodInfo, class attributes, method attributes]`; when `UseAuthorization` is false the authorization entries are stripped
6. **Authorization**: When `UseAuthorization` is true, the SDK `AddAuthorizationFilters()` is registered and evaluates `[Authorize]` / policies / `[AllowAnonymous]` against the host `IAuthorizationService` for both `tools/list` (filtered per user) and `tools/call` (forbidden tools rejected)
7. **Cache hints**: When `ToolsListTimeToLive` is set, a `tools/list` filter stamps `ttlMs` and `cacheScope` on the already-filtered result
8. **MCP marking**: `UseZeroMcpMarking()` middleware adds the `x-mcp-call` header and an `HttpContext.Items` marker
9. **Execution**: Instance controllers are created per call via `ActivatorUtilities.CreateInstance` from the request's service provider, so constructor DI and scoped services work
10. **Unwrapping**: `MarshalResult` extracts the value from `ActionResult<T>` / `ObjectResult` (`Ok(null)` yields `null`); error results such as `NotFound()` / `BadRequest()` throw `InvalidOperationException`, which the SDK reports as a tool error

## Security

- **Authorization follows your endpoints**: the same attributes govern HTTP and MCP; nothing MCP-specific to configure
- **Per-user tool visibility**: `tools/list` only returns tools the caller is authorized to invoke
- **Forbidden calls rejected**: `tools/call` on a hidden tool returns a JSON-RPC error `Access forbidden: This tool requires authorization.` (SDK clients throw `McpProtocolException`)
- **Multiple [Authorize] enforcement**: all `[Authorize]` attributes (class and method) are evaluated
- **[AllowAnonymous] support**: method-level `[AllowAnonymous]` overrides class-level `[Authorize]`
- **Attribute inheritance**: authorization attributes are collected with `inherit: true`
- **Endpoint authentication**: `RequireAuthentication = true` applies `RequireAuthorization()` to the MCP endpoint
- **Cache scope**: cache hints are `private` whenever authorization is on, so a per-user list is never shared

## Best Practices

1. **Use ControllerPrefix** for multiple controllers with similar method names
2. **Register your policies with AddAuthorization(...)**: the SDK filters resolve policy names through the host `IAuthorizationPolicyProvider`
3. **Call UseZeroMcpMarking() early**: before authentication middleware
4. **Use policy-based authorization**: more flexible than raw role checks
5. **Set UseStructuredContent together with OutputSchemaType**: the SDK only emits `outputSchema` for structured tools
6. **Handle null values**: controllers can return `Ok(null)` for nullable types
7. **Return domain error objects instead of NotFound()/BadRequest()** when you want the client to see a payload rather than a tool error
8. **Stay stateless** unless you need SDK sessions; it scales behind load balancers without sticky sessions
9. **Test authorization**: write tests to verify `tools/list` filtering and forbidden `tools/call`

## Troubleshooting

**Problem**: Duplicate tool names
**Solution**: Use `ToolNamingConvention.ControllerPrefix` or explicit `[McpServerTool(Name = "...")]`

**Problem**: `IsMcpCall` always returns false
**Solution**: Ensure `app.UseZeroMcpMarking()` is called BEFORE authentication middleware and its path matches `McpEndpointPath`

**Problem**: Tools not discovered
**Solution**: Ensure the SDK `[McpServerToolType]` (`ModelContextProtocol.Server`) is on the class and `[McpServerTool]` is on the methods

**Problem**: Attributes compile against the old library types
**Solution**: Replace `using Zero.Mcp.Extensions;` for the attributes with `using ModelContextProtocol.Server;` (see Migration)

**Problem**: Policy-based tool always forbidden or `InvalidOperationException` about a missing policy
**Solution**: Call `builder.Services.AddAuthorization(options => options.AddPolicy(...))` with every policy name used on your controllers

**Problem**: `tools/call` fails with `Access forbidden: This tool requires authorization.`
**Solution**: The caller does not satisfy the tool's `[Authorize]` requirements; the SDK client surfaces this as `McpProtocolException`. Set `UseAuthorization = false` only if you intend to expose every tool

**Problem**: `outputSchema` missing from `tools/list`
**Solution**: Set `UseStructuredContent = true` together with `OutputSchemaType`

**Problem**: Wrong assembly scanned
**Solution**: Explicitly set `options.ToolAssembly = typeof(YourController).Assembly`

## Migration from 2.x

1. Replace `using Zero.Mcp.Extensions;` for the attributes with `using ModelContextProtocol.Server;`: the library no longer ships its own `McpServerToolType` / `McpServerTool` attributes.
2. Delete your custom MCP auth supplier and role resolver implementations and their registrations; call `builder.Services.AddAuthorization(...)` with your policies instead.
3. Remove `options.FilterToolsByPermissions`: filtering is always on when `UseAuthorization` is true.
4. Optional: `options.ToolsListTimeToLive` (cache hints), `options.SessionMode` (default `Stateless`), `OutputSchemaType` / `UseStructuredContent` on tools.
5. Tests: the SDK client now throws `McpProtocolException` for forbidden calls instead of returning `IsError = true`.

## Changelog

### v3.0.0
- **Breaking**: the SDK `[McpServerToolType]` / `[McpServerTool]` (`ModelContextProtocol.Server`) replace the library's own attribute types
- **Breaking**: custom auth supplier, role resolver, authorization pre-filter, role store and tool list filter removed; authorization is delegated to the SDK
- **Breaking**: the separate permission-filtering toggle on `ZeroMcpOptions` removed (filtering is always on when `UseAuthorization` is true; see Migration step 3)
- **Breaking**: `tools/call` on a forbidden tool now returns a JSON-RPC error (`McpProtocolException` on SDK clients) instead of an `IsError` result
- **Added**: `ToolsListTimeToLive` cache hints (`ttlMs` + `cacheScope`) on `tools/list`
- **Added**: `SessionMode` (Streamable HTTP `Stateless` / `Stateful` / `StatefulForInitializeClients`)
- **Added**: `Title`, `ReadOnly` / `Destructive` / `Idempotent` / `OpenWorld` hints, `IconSource`, `UseStructuredContent`, `OutputSchemaType` passthrough
- **Added**: structured content (`structuredContent` + `outputSchema`) for tools that opt in
- **Changed**: authorization delegated to the SDK `AddAuthorizationFilters()` through the host `IAuthorizationService`
- **Changed**: stateless Streamable HTTP is the default transport (no `Mcp-Session-Id`)
- **Dependencies**: ModelContextProtocol 2.2.0, ASP.NET Core 10.0.12

### v2.1.0
- Tool naming conventions (`MethodOnly`, `ControllerPrefix`) for generic controllers
- `IMcpRequestContext` for header access and MCP call detection
- `UseZeroMcpMarking()` middleware for automatic `x-mcp-call` header injection
- `[McpServerTool(Name = "...")]` explicit tool naming support

### v2.0.0
- Role-based tool filtering - `tools/list` only returns authorized tools
- Professional configuration system with `ZeroMcpOptions`
- Custom role resolver for role resolution

### v1.0.0
- Initial release with controller-to-MCP-tool conversion
- `ActionResult<T>` unwrapping
- Authorization pre-filter support

## License

Apache-2.0
