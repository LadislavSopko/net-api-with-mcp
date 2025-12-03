# MCP + ASP.NET Core API Integration

[![NuGet](https://img.shields.io/nuget/v/Zero.Mcp.Extensions.svg)](https://www.nuget.org/packages/Zero.Mcp.Extensions/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

Turn your ASP.NET Core API controllers into **MCP (Model Context Protocol)** tools with simple attributes. Includes role-based authorization and tool visibility filtering.

## What's Inside

| Component | Description |
|-----------|-------------|
| **Zero.Mcp.Extensions** | NuGet library - add MCP capabilities to any ASP.NET Core API |
| **McpPoc.Api** | Demo API showing full integration with Keycloak authentication |

## Features

- **Attribute-based tool registration** - Mark controllers with `[McpServerToolType]` and methods with `[McpServerTool]`
- **ActionResult unwrapping** - Automatic conversion of `ActionResult<T>` responses
- **Authorization integration** - Full support for `[Authorize]` policies and `[AllowAnonymous]`
- **Role-based tool filtering** - `tools/list` only returns tools the user can invoke
- **Keycloak integration** - JWT authentication with role mapping

## Quick Start

### Install the NuGet Package

```bash
dotnet add package Zero.Mcp.Extensions
```

### 1. Attribute Your Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
[McpServerToolType]  // Enable MCP for this controller
[Authorize]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    [McpServerTool]  // Expose as MCP tool
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
builder.Services.AddScoped<IAuthForMcpSupplier, YourAuthSupplier>();
builder.Services.AddScoped<IUserRoleResolver, YourRoleResolver>();

builder.Services.AddZeroMcpExtensions(options =>
{
    options.RequireAuthentication = true;
    options.UseAuthorization = true;
    options.FilterToolsByPermissions = true;  // Hide unauthorized tools
    options.McpEndpointPath = "/mcp";
});
```

### 3. Map the Endpoint

```csharp
app.MapZeroMcp();
```

That's it! Your API now speaks MCP at `/mcp`.

## Role-Based Tool Filtering

Users only see tools they're authorized to use:

| User Role | Visible Tools |
|-----------|---------------|
| Viewer | `get_by_id`, `get_all` (read-only) |
| Member | Above + `create` |
| Manager | Above + `update` |
| Admin | All tools including `promote_to_manager` |

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

    // Enforce [Authorize] policies (default: true)
    options.UseAuthorization = true;

    // Filter tools/list by user permissions (default: true)
    options.FilterToolsByPermissions = true;

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
