# MCP + ASP.NET Core API Integration

[![NuGet](https://img.shields.io/nuget/v/Zero.Mcp.Extensions.svg)](https://www.nuget.org/packages/Zero.Mcp.Extensions/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

Turn your ASP.NET Core API controllers into **MCP (Model Context Protocol)** tools with the official SDK attributes. Authorization follows the controllers' own `[Authorize]` rules: the MCP SDK filters `tools/list` per user and rejects unauthorized `tools/call`.

Built on the official [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) **2.2.0** (Streamable HTTP, stateless by default). Version **3.0.0** is a breaking release: see [Migration from 2.x](#migration-from-2x) and [CHANGELOG.md](CHANGELOG.md).

## What's Inside

| Component | Description |
|-----------|-------------|
| **Zero.Mcp.Extensions** | NuGet library - add MCP capabilities to any ASP.NET Core API |
| **McpPoc.Api** | Demo API showing full integration with Keycloak authentication |

## Features

- **Attribute-based tool registration** - Mark controllers with the SDK `[McpServerToolType]` and methods with `[McpServerTool]` (`ModelContextProtocol.Server`); `Name`, `Title`, `ReadOnly`/`Destructive`/`Idempotent`/`OpenWorld` hints, `IconSource`, `UseStructuredContent` and `OutputSchemaType` all flow to the client
- **ActionResult unwrapping** - Automatic conversion of `ActionResult<T>` responses (the SDK alone does not do this)
- **SDK-native authorization** - `[Authorize]`, policies and `[AllowAnonymous]` on your controllers are enforced by the MCP SDK authorization filters through your `IAuthorizationService`: `tools/list` shows only what the caller may invoke and `tools/call` on anything else is rejected with `Access forbidden`
- **Cache hints** - optional `ttlMs` / `cacheScope` on `tools/list` (`ToolsListTimeToLive`)
- **Stateless Streamable HTTP** - no session header by default; `SessionMode` switches to stateful when needed
- **Request context** - `IMcpRequestContext` tells a controller whether it was invoked over MCP or HTTP
- **Keycloak demo** - JWT authentication with realm roles mapped to policies

## Quick Start

### Install the NuGet Package

```bash
dotnet add package Zero.Mcp.Extensions
```

### 1. Attribute Your Controllers

```csharp
using ModelContextProtocol.Server;   // SDK attributes (no Zero.Mcp.Extensions attribute types since 3.0.0)

[ApiController]
[Route("api/[controller]")]
[McpServerToolType]  // Enable MCP for this controller
[Authorize]          // Enforced for MCP calls too
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    [McpServerTool(Name = "UserGetById", UseStructuredContent = true, OutputSchemaType = typeof(User))]
    [Description("Gets a user by ID")]
    public async Task<ActionResult<User>> GetById(int id) { ... }

    [HttpPost]
    [McpServerTool]
    [Description("Creates a new user")]
    [Authorize(Policy = "RequireMember")]  // Role-based access
    public async Task<ActionResult<User>> Create(CreateUserRequest request) { ... }
}
```

### 2. Configure Services

```csharp
// Program.cs
builder.Services.AddAuthentication(...).AddJwtBearer(...);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireMember", p => p.RequireRole("member", "manager", "admin"));
});

builder.Services.AddZeroMcpExtensions(options =>
{
    options.RequireAuthentication = true;   // /mcp needs a valid bearer token
    options.UseAuthorization = true;        // SDK filters enforce [Authorize]/[AllowAnonymous]/policies
    options.McpEndpointPath = "/mcp";
    options.ToolsListTimeToLive = TimeSpan.FromMinutes(5);   // optional cache hint
});
```

### 3. Map the Endpoint

```csharp
app.UseZeroMcpMarking();   // optional: enables IMcpRequestContext
app.UseAuthentication();
app.UseAuthorization();
app.MapZeroMcp();
```

That's it! Your API now speaks MCP at `/mcp`.

## Authorization Follows Your Endpoints

There is nothing MCP-specific to configure. The library attaches each controller's `[Authorize]`, policy and `[AllowAnonymous]` attributes to the generated tool, and the MCP SDK's `AddAuthorizationFilters()` evaluates them with the same `IAuthorizationService` your HTTP endpoints use:

| User Role | Visible / callable tools in the demo |
|-----------|---------------|
| Viewer | `UserGetById`, `get_all`, `get_scope_id`, `get_public_info`, `get_mcp_context`, `echo_headers` |
| Member | Above + `create` |
| Manager | Above + `update` |
| Admin | All 9, including `promote_to_manager` |

A `tools/call` on a hidden tool returns a JSON-RPC error (`Access forbidden: This tool requires authorization.`). Set `UseAuthorization = false` to expose every tool without checks (the demo does this when `Auth:Enabled=false`).

## Migration from 2.x

1. Replace `using Zero.Mcp.Extensions;` for the attributes with `using ModelContextProtocol.Server;`: the library no longer ships its own `McpServerToolType`/`McpServerTool` attributes.
2. Delete your custom MCP auth supplier and role resolver implementations and their registrations; call `builder.Services.AddAuthorization(...)` with your policies instead.
3. Remove `options.FilterToolsByPermissions`: filtering is always on when `UseAuthorization` is true.
4. Optional: `options.ToolsListTimeToLive` (cache hints), `options.SessionMode` (default `Stateless`), `OutputSchemaType`/`UseStructuredContent` on tools.
5. Tests: the SDK client now throws `McpProtocolException` for forbidden calls instead of returning `IsError = true`.

## Running the Demo

### Prerequisites

- .NET 9.0 SDK
- Docker & Docker Compose

### Start Infrastructure

```bash
cd docker
docker-compose up -d
```

This starts:
- **Keycloak** (localhost:8080) - Identity provider
- **PostgreSQL** - Database for Keycloak

### Run the API

```bash
dotnet run --project src/McpPoc.Api
```

API available at `http://localhost:5001`

### Get a Token

```bash
# As admin
TOKEN=$(./get-token.sh admin admin123)

# As member
TOKEN=$(./get-token.sh alice@example.com alice123)

# As viewer
TOKEN=$(./get-token.sh viewer viewer123)
```

### Test MCP Endpoint

```bash
# List available tools
curl -X POST http://localhost:5001/mcp \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/list","id":1}'
```

## Test Users

| Username | Password | Role | Can Do |
|----------|----------|------|--------|
| `viewer` | `viewer123` | Viewer | Read only |
| `alice@example.com` | `alice123` | Member | Read + Create |
| `bob@example.com` | `bob123` | Manager | Read + Create + Update |
| `carol@example.com` | `carol123` | Admin | Everything |

## Configuration Options

```csharp
builder.Services.AddZeroMcpExtensions(options =>
{
    // Require JWT authentication (default: true)
    options.RequireAuthentication = true;

    // Let the MCP SDK enforce [Authorize]/[AllowAnonymous]/policies from your controllers (default: true)
    options.UseAuthorization = true;

    // tools/list cache hints: ttlMs + cacheScope (private when UseAuthorization) (default: null = none)
    options.ToolsListTimeToLive = TimeSpan.FromMinutes(5);

    // Streamable HTTP session mode (default: Stateless)
    options.SessionMode = HttpServerSessionMode.Stateless;

    // MCP endpoint path (default: "/mcp")
    options.McpEndpointPath = "/mcp";

    // Assembly to scan for tools (default: calling assembly)
    options.ToolAssembly = typeof(MyController).Assembly;

    // JSON serialization options
    options.SerializerOptions = new JsonSerializerOptions { ... };
});
```

## Project Structure

```
├── src/
│   ├── Zero.Mcp.Extensions/     # NuGet library
│   └── McpPoc.Api/              # Demo API
├── tests/
│   ├── Zero.Mcp.Extensions.Tests/
│   └── McpPoc.Api.Tests/
├── docker/                       # Keycloak + Postgres
└── docs/                         # Additional documentation
```

## Documentation

- [Zero.Mcp.Extensions README](src/Zero.Mcp.Extensions/README.md) - Library details
- [Users and Permissions](USERS-AND-PERMISSIONS.md) - Role system explained
- [MCP Authorization Guide](docs/MCP-AUTHORIZATION-COMPLETE-GUIDE.md) - Deep dive

## License

[Apache 2.0](LICENSE) - Ladislav Sopko / 0ics srl
