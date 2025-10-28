# Complete MCP Integration with .NET API Guide

**Version:** 1.0
**Last Updated:** 2025-10-28
**Project:** McpPoc.Api

## Table of Contents

1. [Introduction](#introduction)
2. [What is MCP?](#what-is-mcp)
3. [System Architecture](#system-architecture)
4. [Prerequisites](#prerequisites)
5. [Infrastructure Setup](#infrastructure-setup)
6. [MCP Integration Steps](#mcp-integration-steps)
7. [Controller Integration Patterns](#controller-integration-patterns)
8. [Authentication & Authorization](#authentication--authorization)
9. [Testing Infrastructure](#testing-infrastructure)
10. [Request Flow & Execution](#request-flow--execution)
11. [Dependency Injection & Scoping](#dependency-injection--scoping)
12. [Complete Code Reference](#complete-code-reference)
13. [Common Patterns & Examples](#common-patterns--examples)
14. [Troubleshooting](#troubleshooting)
15. [Critical Discoveries](#critical-discoveries)

---

## Introduction

This guide provides **complete, production-ready documentation** for integrating Microsoft's Model Context Protocol (MCP) with ASP.NET Core APIs. It shows how to **seamlessly expose existing controllers as MCP tools** while maintaining full HTTP API compatibility.

### What This Guide Covers

- ✅ **Complete MCP integration** from scratch
- ✅ **Dual protocol support** - HTTP REST API and MCP tools simultaneously
- ✅ **Full authentication** with JWT Bearer (Keycloak)
- ✅ **Policy-based authorization** with role hierarchy
- ✅ **Testing infrastructure** for both protocols
- ✅ **DI scoping** verification for EF Core compatibility
- ✅ **Production patterns** and best practices
- ✅ **All code from real, working implementation** - no guessing

### Project Status

- **32/32 tests passing** (100%)
- **All phases complete** through Phase 4
- **Production-ready** authorization and authentication
- **Proven patterns** for seamless API integration

---

## What is MCP?

**Model Context Protocol (MCP)** is Microsoft's open protocol for exposing tools and resources to AI models. Think of it as OpenAPI/Swagger for AI agents.

### Key Concepts

```mermaid
graph LR
    A[AI Agent] -->|MCP Protocol| B[MCP Server]
    B -->|Tool Discovery| C[list_tools]
    B -->|Tool Invocation| D[call_tool]
    D -->|Pre-Filter Auth| E[Authorization Check]
    E -->|Create Controller| F[ASP.NET Controller]
    F -->|Return Result| D
```

#### MCP Components

1. **MCP Server** - Exposes tools via HTTP transport
2. **MCP Tools** - Individual operations (GET user, CREATE user, etc.)
3. **MCP Protocol** - JSON-RPC 2.0 based communication
4. **Tool Schema** - JSON Schema describing parameters

#### Why Use MCP with APIs?

- **AI-Native Interface** - AI agents can discover and invoke your API operations
- **Zero Duplication** - Same controllers work for both HTTP and MCP
- **Type Safety** - Automatic JSON Schema generation from C# types
- **Authorization Ready** - Inherits ASP.NET Core security pipeline

---

## System Architecture

### High-Level Architecture

```mermaid
graph TB
    subgraph External["External Systems"]
        AI[AI Agent]
        HTTP[HTTP Client]
        KC["Keycloak
        Identity Provider"]
    end

    subgraph App["ASP.NET Core Application"]
        subgraph Endpoints
            MCP["/mcp endpoint"]
            API["/api/* endpoints"]
        end

        subgraph Pipeline["Middleware Pipeline"]
            AUTH["Authentication
            JWT Bearer"]
            AUTHZ["Authorization
            Policies"]
        end

        subgraph MCPLayer["MCP Layer"]
            MCPSRV[MCP Server]
            PREFILTER["Pre-Filter
            Authorization"]
            UNWRAP["ActionResult
            Unwrapper"]
        end

        subgraph AppLayer["Application Layer"]
            CTRL["Controllers
            with McpServerTool"]
            SVC["Services
            IUserService"]
            DB[("In-Memory
            Data Store")]
        end
    end

    AI -->|MCP Protocol| MCP
    HTTP -->|REST API| API
    AI -->|Get Token| KC
    HTTP -->|Get Token| KC

    MCP --> AUTH
    API --> AUTH
    AUTH --> AUTHZ
    AUTHZ --> MCPSRV
    AUTHZ --> CTRL

    MCPSRV --> PREFILTER
    PREFILTER --> CTRL
    CTRL --> UNWRAP
    UNWRAP -->|Response| MCPSRV

    CTRL --> SVC
    SVC --> DB
```

### Component Responsibilities

| Component | Responsibility | Location |
|-----------|---------------|----------|
| **MCP Server** | Protocol handling, tool discovery | `builder.Services.AddMcpServer()` |
| **Pre-Filter** | Authorization BEFORE controller creation | `CreateControllerWithPreFilter()` |
| **ActionResult Unwrapper** | Extract values from `ActionResult<T>` | `UnwrapActionResult()` |
| **Controllers** | Business logic, both HTTP and MCP | `UsersController.cs` |
| **Authorization Handler** | Policy enforcement | `MinimumRoleRequirementHandler.cs` |
| **Services** | Business logic, data access | `UserService.cs` |

---

## Prerequisites

### Required Packages

```xml
<ItemGroup>
  <!-- MCP SDK -->
  <PackageReference Include="ModelContextProtocol.Server" Version="0.5.0" />
  <PackageReference Include="ModelContextProtocol.Server.HttpTransport" Version="0.5.0" />

  <!-- Authentication -->
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0" />

  <!-- Logging -->
  <PackageReference Include="Serilog.AspNetCore" Version="8.0.3" />
  <PackageReference Include="Serilog.Sinks.File" Version="6.0.0" />

  <!-- Testing -->
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.0" />
  <PackageReference Include="FluentAssertions" Version="7.0.0" />
  <PackageReference Include="xunit" Version="2.9.2" />
</ItemGroup>
```

### Development Environment

- **.NET 9.0 SDK** or later
- **Docker & Docker Compose** for infrastructure
- **IDE** with C# support (VS, VS Code, Rider)

### Infrastructure Requirements

- **PostgreSQL 16** (for Keycloak)
- **Keycloak 25.0.2** (OIDC provider)
- Port availability: 5432 (PostgreSQL), 8080 (Keycloak), 5001 (API)

---

## Infrastructure Setup

### Step 1: Docker Compose Configuration

Create `docker/docker-compose.yml`:

```yaml
services:
  postgres:
    image: postgres:16-alpine
    container_name: mcppoc-postgres
    environment:
      POSTGRES_DB: mcppoc_db
      POSTGRES_USER: mcppoc_user
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - mcppoc-network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U mcppoc_user -d mcppoc_db"]
      interval: 10s
      timeout: 5s
      retries: 5

  keycloak:
    image: quay.io/keycloak/keycloak:25.0.2
    container_name: mcppoc-keycloak
    environment:
      KC_DB: postgres
      KC_DB_URL: jdbc:postgresql://postgres:5432/keycloak
      KC_DB_USERNAME: mcppoc_user
      KC_DB_PASSWORD: ${POSTGRES_PASSWORD}
      KC_HOSTNAME: localhost
      KC_HOSTNAME_PORT: 8080
      KC_HOSTNAME_STRICT: false
      KC_HOSTNAME_STRICT_HTTPS: false
      KC_LOG_LEVEL: info
      KC_METRICS_ENABLED: true
      KC_HEALTH_ENABLED: true
      KEYCLOAK_ADMIN: ${KEYCLOAK_ADMIN_USER}
      KEYCLOAK_ADMIN_PASSWORD: ${KEYCLOAK_ADMIN_PASSWORD}
    command: start-dev --import-realm
    ports:
      - "8080:8080"
    volumes:
      - ./keycloak:/opt/keycloak/data/import:ro
    depends_on:
      postgres:
        condition: service_healthy
    networks:
      - mcppoc-network
    healthcheck:
      test: ["CMD-SHELL", "exec 3<>/dev/tcp/127.0.0.1/8080;echo -e 'GET /health/ready HTTP/1.1\\r\\nhost: 127.0.0.1\\r\\nConnection: close\\r\\n\\r\\n' >&3;if [ $? -eq 0 ]; then echo 'Healthcheck Successful';exit 0;else echo 'Healthcheck Failed';exit 1;fi;"]
      interval: 10s
      timeout: 10s
      retries: 15
      start_period: 90s

volumes:
  postgres_data:

networks:
  mcppoc-network:
    driver: bridge
```

### Step 2: Environment Configuration

Create `docker/.env`:

```env
POSTGRES_PASSWORD=your_secure_password_here
KEYCLOAK_ADMIN_USER=admin
KEYCLOAK_ADMIN_PASSWORD=admin
```

### Step 3: Keycloak Realm Configuration

Create `docker/keycloak/mcppoc-realm.json` with users and roles:

```json
{
  "realm": "mcppoc-realm",
  "enabled": true,
  "users": [
    {
      "username": "alice@example.com",
      "email": "alice@example.com",
      "enabled": true,
      "firstName": "Alice",
      "lastName": "Smith",
      "credentials": [{"type": "password", "value": "alice123"}],
      "realmRoles": ["user"]
    },
    {
      "username": "bob@example.com",
      "email": "bob@example.com",
      "enabled": true,
      "firstName": "Bob",
      "lastName": "Jones",
      "credentials": [{"type": "password", "value": "bob123"}],
      "realmRoles": ["user", "manager"]
    },
    {
      "username": "carol@example.com",
      "email": "carol@example.com",
      "enabled": true,
      "firstName": "Carol",
      "lastName": "White",
      "credentials": [{"type": "password", "value": "carol123"}],
      "realmRoles": ["user", "manager", "admin"]
    }
  ],
  "roles": {
    "realm": [
      {"name": "user"},
      {"name": "manager"},
      {"name": "admin"}
    ]
  },
  "clients": [
    {
      "clientId": "mcppoc-api",
      "enabled": true,
      "publicClient": false,
      "serviceAccountsEnabled": true,
      "directAccessGrantsEnabled": true,
      "secret": "your-client-secret-here",
      "redirectUris": ["http://127.0.0.1:5001/*"],
      "webOrigins": ["http://127.0.0.1:5001"]
    }
  ]
}
```

### Step 4: Start Infrastructure

```bash
cd docker
docker-compose up -d

# Verify services are healthy
docker-compose ps

# Check Keycloak is ready
curl http://127.0.0.1:8080/health/ready
```

---

## MCP Integration Steps

### Step 1: Application Configuration

Create `src/McpPoc.Api/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "McpPoc.Api": "Trace"
    }
  },
  "AllowedHosts": "*",
  "Keycloak": {
    "Authority": "http://127.0.0.1:8080/realms/mcppoc-realm",
    "Audience": "account",
    "RequireHttpsMetadata": false
  }
}
```

### Step 2: Program.cs - Complete Setup

File: `src/McpPoc.Api/Program.cs`

```csharp
using McpPoc.Api.Authorization;
using McpPoc.Api.Extensions;
using McpPoc.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Serilog;

// Configure Serilog for file logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.File("logs/mcppoc-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog
builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with OAuth2
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MCP POC API",
        Version = "v1",
        Description = "API with MCP tools and Keycloak authentication"
    });

    // Add OAuth2 security definition
    var keycloakAuthority = builder.Configuration["Keycloak:Authority"];
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            Implicit = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{keycloakAuthority}/protocol/openid-connect/auth"),
                TokenUrl = new Uri($"{keycloakAuthority}/protocol/openid-connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "openid", "OpenID Connect" },
                    { "profile", "User profile" },
                    { "email", "User email" }
                }
            }
        }
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "oauth2"
                }
            },
            new[] { "openid", "profile", "email" }
        }
    });
});

// Configure JWT Bearer authentication with Keycloak
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var keycloakAuthority = builder.Configuration["Keycloak:Authority"];

        options.Authority = keycloakAuthority;
        options.Audience = builder.Configuration["Keycloak:Audience"];
        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Keycloak:RequireHttpsMetadata");

        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateAudience = false,  // Keycloak uses 'azp' claim instead of 'aud'
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>()
                    .LogError(context.Exception, "Authentication failed");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>()
                    .LogInformation("Token validated for user: {User}",
                        context.Principal?.Identity?.Name ?? "Unknown");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// CRITICAL: Add custom authorization policies
builder.Services.AddMcpPocAuthorization();

// CRITICAL: Add HttpContextAccessor for pre-filter authorization
builder.Services.AddHttpContextAccessor();

// Register services - Scoped for test isolation and EF Core compatibility
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IScopedRequestTracker, ScopedRequestTracker>();

// ============================================
// MCP Server Configuration
// ============================================
builder.Services
    .AddMcpServer()                                      // Add MCP server
    .WithHttpTransport()                                 // Use HTTP transport
    .WithToolsFromAssemblyUnwrappingActionResult();      // Custom: Unwrap ActionResult<T>

var app = builder.Build();

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "MCP POC API v1");
        options.OAuthClientId("mcppoc-api");
        options.OAuthAppName("MCP POC API");
        options.OAuthUsePkce();
    });
}

// CRITICAL: Middleware order matters!
app.UseHttpsRedirection();
app.UseAuthentication();        // JWT validation
app.UseAuthorization();         // Policy enforcement
app.MapControllers();           // HTTP API routes

// ============================================
// MCP Endpoint - MUST be authenticated
// ============================================
app.MapMcp("/mcp").RequireAuthorization();

app.Logger.LogInformation("===========================================");
app.Logger.LogInformation("MCP + Controller Integration");
app.Logger.LogInformation("HTTP API: http://127.0.0.1:5001/api/users");
app.Logger.LogInformation("MCP Endpoint: http://127.0.0.1:5001/mcp");
app.Logger.LogInformation("Swagger: http://127.0.0.1:5001/swagger");
app.Logger.LogInformation("===========================================");

app.Run();

// Make Program accessible for WebApplicationFactory
public partial class Program { }
```

### Step 3: Custom MCP Extension - ActionResult Unwrapping

This is the **key piece** that makes controllers work as MCP tools.

File: `src/McpPoc.Api/Extensions/McpServerBuilderExtensions.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Server;
using System.Reflection;

namespace McpPoc.Api.Extensions;

public static class McpServerBuilderExtensions
{
    /// <summary>
    /// Adds MCP tools from assembly with ActionResult unwrapping and pre-filter authorization.
    /// This enables ASP.NET Core controllers to work as MCP tools.
    /// </summary>
    public static IMcpServerBuilder WithToolsFromAssemblyUnwrappingActionResult(
        this IMcpServerBuilder builder)
    {
        return builder.WithToolsFromAssembly(
            typeof(Program).Assembly,
            controllerFactory: CreateControllerWithPreFilter,
            resultMarshaller: UnwrapActionResult);
    }

    /// <summary>
    /// Creates controller instance with PRE-FILTER authorization check.
    /// Authorization happens BEFORE controller is instantiated.
    /// </summary>
    private static object CreateControllerWithPreFilter(
        IServiceProvider services,
        Type controllerType,
        MethodInfo method)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogTrace("CreateControllerWithPreFilter: {Controller}.{Method}",
            controllerType.Name, method.Name);

        // Get HttpContext via IHttpContextAccessor
        var httpContextAccessor = services.GetService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor?.HttpContext;

        if (httpContext == null)
        {
            logger.LogError("HttpContext not available in CreateControllerWithPreFilter");
            throw new InvalidOperationException(
                "HttpContext not available. Ensure IHttpContextAccessor is registered.");
        }

        // Check for [Authorize] attribute on method or class
        var methodAuthorize = method.GetCustomAttribute<AuthorizeAttribute>();
        var classAuthorize = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        var authorizeAttr = methodAuthorize ?? classAuthorize;

        if (authorizeAttr != null)
        {
            logger.LogTrace("Found [Authorize] attribute - performing pre-filter authorization");

            // 1. Check Authentication
            if (httpContext.User?.Identity?.IsAuthenticated != true)
            {
                logger.LogWarning("Authentication failed - user not authenticated");
                throw new UnauthorizedAccessException("Authentication required to access this tool");
            }

            logger.LogTrace("User authenticated: {User}", httpContext.User.Identity.Name);

            // 2. Check Policy Authorization
            if (!string.IsNullOrEmpty(authorizeAttr.Policy))
            {
                logger.LogTrace("Checking policy: {Policy}", authorizeAttr.Policy);

                var authService = services.GetRequiredService<IAuthorizationService>();

                // CRITICAL: Must use blocking call because factory is synchronous
                var authResult = authService.AuthorizeAsync(
                    httpContext.User,
                    httpContext,
                    authorizeAttr.Policy).GetAwaiter().GetResult();

                if (!authResult.Succeeded)
                {
                    var reasons = string.Join(", ", authResult.Failure?.FailureReasons.Select(r => r.Message) ?? []);
                    logger.LogWarning(
                        "Policy authorization failed: {Policy}. Reasons: {Reasons}",
                        authorizeAttr.Policy, reasons);

                    throw new UnauthorizedAccessException(
                        $"Access denied: Policy '{authorizeAttr.Policy}' not satisfied");
                }

                logger.LogInformation("Policy authorization succeeded: {Policy}", authorizeAttr.Policy);
            }

            // 3. Check Role Authorization
            if (!string.IsNullOrEmpty(authorizeAttr.Roles))
            {
                logger.LogTrace("Checking roles: {Roles}", authorizeAttr.Roles);

                var roles = authorizeAttr.Roles.Split(',').Select(r => r.Trim());
                var hasRole = roles.Any(role => httpContext.User.IsInRole(role));

                if (!hasRole)
                {
                    logger.LogWarning(
                        "Role authorization failed - required roles: {Roles}",
                        authorizeAttr.Roles);

                    throw new UnauthorizedAccessException(
                        $"Access denied: Required role '{authorizeAttr.Roles}'");
                }

                logger.LogInformation("Role authorization succeeded: {Roles}", authorizeAttr.Roles);
            }
        }

        // Authorization passed - create controller instance
        logger.LogTrace("Creating controller instance: {Controller}", controllerType.Name);
        return ActivatorUtilities.CreateInstance(services, controllerType);
    }

    /// <summary>
    /// Unwraps ActionResult&lt;T&gt; to extract the actual value for MCP response.
    /// MCP SDK expects plain objects, not ActionResult wrappers.
    /// </summary>
    private static ValueTask<object?> UnwrapActionResult(
        object? result,
        Type? resultType,
        CancellationToken cancellationToken)
    {
        if (result == null)
        {
            return ValueTask.FromResult<object?>(null);
        }

        var unwrapped = UnwrapIfActionResult(result);
        return ValueTask.FromResult(unwrapped);
    }

    /// <summary>
    /// Recursively unwraps ActionResult types to get the actual value.
    /// </summary>
    private static object? UnwrapIfActionResult(object? obj)
    {
        if (obj == null) return null;

        var type = obj.GetType();

        // Handle ActionResult<T>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ActionResult<>))
        {
            // Try Result property first (for explicit results)
            var resultProp = type.GetProperty("Result");
            if (resultProp != null)
            {
                var resultValue = resultProp.GetValue(obj);
                if (resultValue != null)
                {
                    return UnwrapIfActionResult(resultValue);
                }
            }

            // Try Value property (for implicit conversions)
            var valueProp = type.GetProperty("Value");
            if (valueProp != null)
            {
                var value = valueProp.GetValue(obj);
                if (value != null)
                {
                    return value;
                }
            }
        }

        // Handle ObjectResult (Ok, Created, etc.)
        if (obj is ObjectResult objectResult)
        {
            return objectResult.Value;
        }

        // Already unwrapped or not an ActionResult
        return obj;
    }
}
```

### Key Features of This Extension:

1. **Pre-Filter Authorization** - Checks `[Authorize]` BEFORE creating controller
2. **Synchronous Auth** - Uses `.GetAwaiter().GetResult()` because factory is sync
3. **ActionResult Unwrapping** - Extracts values from `ActionResult<T>`
4. **HttpContext Access** - Uses `IHttpContextAccessor` to get current request context
5. **Comprehensive Logging** - Traces every authorization decision

---

## Controller Integration Patterns

### Pattern 1: Basic Controller with MCP Tools

File: `src/McpPoc.Api/Controllers/UsersController.cs`

```csharp
using McpPoc.Api.Authorization;
using McpPoc.Api.Models;
using McpPoc.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpPoc.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]                  // ← Endpoint-level authentication
[McpServerToolType]          // ← Enables MCP tool exposure
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    private readonly IScopedRequestTracker _scopedTracker;

    public UsersController(
        IUserService userService,
        ILogger<UsersController> logger,
        IScopedRequestTracker scopedTracker)
    {
        _userService = userService;
        _logger = logger;
        _scopedTracker = scopedTracker;
    }

    // GET /api/users/{id} AND MCP tool "get_by_id"
    [HttpGet("{id}")]
    [McpServerTool, Description("Gets a user by their ID")]
    public async Task<ActionResult<User>> GetById(int id)
    {
        _logger.LogInformation("GetById called with id: {Id}", id);

        var user = await _userService.GetByIdAsync(id);

        if (user == null)
        {
            return NotFound(new { error = "User not found", id });
        }

        return Ok(user);
    }

    // GET /api/users AND MCP tool "get_all"
    [HttpGet]
    [McpServerTool, Description("Gets all users")]
    public async Task<ActionResult<List<User>>> GetAll()
    {
        _logger.LogInformation("GetAll called");

        var users = await _userService.GetAllAsync();
        return Ok(users);
    }

    // POST /api/users AND MCP tool "create" - Requires Member role
    [HttpPost]
    [McpServerTool, Description("Creates a new user - requires Member role")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    public async Task<ActionResult<User>> Create(
        [Description("User creation data")] CreateUserRequest request)
    {
        _logger.LogInformation("Create called with name: {Name}, email: {Email}",
            request.Name, request.Email);

        var user = await _userService.CreateAsync(request.Name, request.Email);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    // PUT /api/users/{id} AND MCP tool "update" - Requires Manager role
    [HttpPut("{id}")]
    [McpServerTool, Description("Updates a user - requires Manager role")]
    [Authorize(Policy = PolicyNames.RequireManager)]
    public async Task<ActionResult<User>> Update(
        int id,
        [Description("User update data")] UpdateUserRequest request)
    {
        _logger.LogInformation("Update called for id: {Id}", id);

        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { error = "User not found", id });
        }

        user.Name = request.Name;
        user.Email = request.Email;

        return Ok(user);
    }

    // POST /api/users/{id}/promote AND MCP tool "promote_to_manager" - Requires Admin
    [HttpPost("{id}/promote")]
    [McpServerTool, Description("Promotes a user to Manager - requires Admin role")]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public async Task<ActionResult<User>> PromoteToManager(int id)
    {
        _logger.LogInformation("Promote called for id: {Id}", id);

        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { error = "User not found", id });
        }

        user.Role = UserRole.Manager;
        return Ok(user);
    }

    // GET /api/users/scope-test AND MCP tool "get_scope_id"
    // Used to verify DI scoping works correctly
    [HttpGet("scope-test")]
    [McpServerTool, Description("Returns the current request scope ID for DI testing")]
    public ActionResult<ScopeIdResponse> GetScopeId()
    {
        _logger.LogInformation("GetScopeId called - RequestId: {RequestId}",
            _scopedTracker.RequestId);

        var response = new ScopeIdResponse(
            _scopedTracker.RequestId,
            _scopedTracker.CreatedAt,
            "Each call should return a different ID if scoping works correctly"
        );

        return Ok(response);
    }

    // DELETE /api/users/{id} - HTTP ONLY (no [McpServerTool])
    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id)
    {
        _logger.LogInformation("Delete called (NOT an MCP tool) with id: {Id}", id);
        return Task.FromResult((IActionResult)NoContent());
    }
}

// DTOs
public record ScopeIdResponse(Guid RequestId, DateTime CreatedAt, string Message);
public record CreateUserRequest(string Name, string Email);
public record UpdateUserRequest(string Name, string Email);
```

### Key Controller Patterns:

1. **`[McpServerToolType]`** on class - Enables tool exposure
2. **`[McpServerTool]`** on methods - Marks individual tools
3. **`[Description]`** - Provides tool and parameter descriptions
4. **`ActionResult<T>`** - MCP unwrapper extracts `T`
5. **Selective Exposure** - `Delete` has no `[McpServerTool]`, so it's HTTP-only

### Method Name Conversion

MCP SDK automatically converts C# method names to snake_case:

| C# Method Name | MCP Tool Name |
|----------------|---------------|
| `GetById` | `get_by_id` |
| `GetAll` | `get_all` |
| `Create` | `create` |
| `Update` | `update` |
| `PromoteToManager` | `promote_to_manager` |

### DTO Parameter Binding Pattern

**CRITICAL:** MCP SDK requires **nested parameter structure** for complex types (DTOs).

#### How It Works

When you define a controller method with a DTO parameter:

```csharp
[HttpPost]
[McpServerTool]
public async Task<ActionResult<User>> Create(CreateUserRequest request)
{
    // Controller code
}

public record CreateUserRequest(string Name, string Email);
```

The MCP SDK generates this JSON Schema:

```json
{
  "name": "create",
  "inputSchema": {
    "type": "object",
    "properties": {
      "request": {              // ← Parameter name becomes wrapper
        "type": "object",
        "properties": {
          "name": { "type": "string" },
          "email": { "type": "string" }
        }
      }
    }
  }
}
```

#### Calling from MCP Client

**❌ WRONG - Flat structure (will fail):**
```csharp
var args = new Dictionary<string, object?>
{
    ["name"] = "Test User",
    ["email"] = "test@example.com"
};
```

**✅ CORRECT - Nested structure:**
```csharp
var args = new Dictionary<string, object?>
{
    ["request"] = new Dictionary<string, object?>  // ← Nest inside parameter name
    {
        ["name"] = "Test User",
        ["email"] = "test@example.com"
    }
};

var result = await mcpClient.CallToolAsync("create", args);
```

#### Multiple Parameters

When you have multiple parameters, each DTO must be nested:

```csharp
[HttpPut("{id}")]
[McpServerTool]
public async Task<ActionResult<User>> Update(int id, UpdateUserRequest request)
{
    // Controller code
}
```

**Call with:**
```csharp
var args = new Dictionary<string, object?>
{
    ["id"] = 1,                                    // ← Simple types stay flat
    ["request"] = new Dictionary<string, object?>  // ← DTOs are nested
    {
        ["name"] = "Updated Name",
        ["email"] = "updated@example.com"
    }
};
```

#### Rule Summary

| Parameter Type | Structure | Example |
|----------------|-----------|---------|
| Simple (`int`, `string`, `bool`) | Flat | `["id"] = 1` |
| DTO/Complex Type | Nested | `["request"] = { ... }` |
| Multiple parameters | Mixed | `["id"] = 1, ["request"] = { ... }` |

#### Why This Pattern?

- **ASP.NET Core Inference:** Controllers don't need `[FromBody]` - it's inferred for complex types
- **MCP SDK Behavior:** SDK uses parameter names as JSON property names
- **Type Safety:** Preserves C# type information in JSON Schema

#### HTTP vs MCP Comparison

**HTTP API Call:**
```bash
POST /api/users
Content-Type: application/json

{
  "name": "Test User",
  "email": "test@example.com"
}
```

**MCP Tool Call:**
```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "create",
    "arguments": {
      "request": {              // ← Extra nesting for MCP
        "name": "Test User",
        "email": "test@example.com"
      }
    }
  }
}
```

---

## Authentication & Authorization

### JWT Bearer Authentication

Authentication flow with Keycloak:

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant Keycloak

    Client->>Keycloak: POST /token (username/password)
    Keycloak->>Client: JWT access token
    Client->>API: Request with Bearer token
    API->>API: Validate JWT signature
    API->>API: Check token expiration
    API->>API: Extract claims (preferred_username, etc.)
    API->>Client: Response (200 OK or 401 Unauthorized)
```

### Authorization Infrastructure

#### 1. Policy Names

File: `src/McpPoc.Api/Authorization/PolicyNames.cs`

```csharp
namespace McpPoc.Api.Authorization;

public static class PolicyNames
{
    public const string RequireMember = "RequireMember";
    public const string RequireManager = "RequireManager";
    public const string RequireAdmin = "RequireAdmin";
}
```

#### 2. Minimum Role Requirement

File: `src/McpPoc.Api/Authorization/MinimumRoleRequirement.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using McpPoc.Api.Models;

namespace McpPoc.Api.Authorization;

/// <summary>
/// Authorization requirement that checks minimum role level.
/// Supports role hierarchy: Member(1) &lt; Manager(2) &lt; Admin(3).
/// </summary>
public class MinimumRoleRequirement : IAuthorizationRequirement
{
    public UserRole MinimumRole { get; }

    public MinimumRoleRequirement(UserRole minimumRole)
    {
        MinimumRole = minimumRole;
    }
}
```

#### 3. Authorization Handler

File: `src/McpPoc.Api/Authorization/MinimumRoleRequirementHandler.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using McpPoc.Api.Models;
using McpPoc.Api.Services;

namespace McpPoc.Api.Authorization;

/// <summary>
/// Handles MinimumRoleRequirement by querying user service for role.
/// Implements role hierarchy where higher roles inherit lower permissions.
/// </summary>
public class MinimumRoleRequirementHandler
    : AuthorizationHandler<MinimumRoleRequirement>
{
    private readonly IUserService _userService;
    private readonly ILogger<MinimumRoleRequirementHandler> _logger;

    public MinimumRoleRequirementHandler(
        IUserService userService,
        ILogger<MinimumRoleRequirementHandler> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumRoleRequirement requirement)
    {
        _logger.LogTrace("Handling MinimumRoleRequirement for role: {Role}",
            requirement.MinimumRole);

        // 1. Check if user is authenticated
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogTrace("User not authenticated");
            return;
        }

        // 2. Get preferred_username claim (OIDC standard)
        var usernameClaim = context.User.FindFirst("preferred_username")?.Value;
        if (string.IsNullOrEmpty(usernameClaim))
        {
            _logger.LogTrace("No preferred_username claim found");
            return;
        }

        _logger.LogTrace("Found username claim: {Username}", usernameClaim);

        // 3. Query user from service
        var users = await _userService.GetAllAsync();
        var user = users.FirstOrDefault(u => u.Email == usernameClaim);

        if (user == null)
        {
            _logger.LogWarning("User not found in service: {Username}", usernameClaim);
            return;
        }

        _logger.LogTrace("User found with role: {Role}", user.Role);

        // 4. Check role hierarchy (>= allows inheritance)
        if (user.Role >= requirement.MinimumRole)
        {
            _logger.LogInformation(
                "User {Username} with role \"{UserRole}\" meets minimum role \"{MinRole}\"",
                usernameClaim, user.Role, requirement.MinimumRole);

            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "User {Username} with role \"{UserRole}\" does NOT meet minimum role \"{MinRole}\"",
                usernameClaim, user.Role, requirement.MinimumRole);
        }
    }
}
```

#### 4. Service Registration

File: `src/McpPoc.Api/Authorization/AuthorizationServiceExtensions.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using McpPoc.Api.Models;

namespace McpPoc.Api.Authorization;

public static class AuthorizationServiceExtensions
{
    /// <summary>
    /// Registers MCP POC authorization policies and handlers.
    /// </summary>
    public static IServiceCollection AddMcpPocAuthorization(
        this IServiceCollection services)
    {
        // Register handler as Scoped (depends on Scoped IUserService)
        services.AddScoped<IAuthorizationHandler, MinimumRoleRequirementHandler>();

        // Configure authorization policies
        services.AddAuthorizationCore(options =>
        {
            options.AddPolicy(PolicyNames.RequireMember, policy =>
                policy.Requirements.Add(new MinimumRoleRequirement(UserRole.Member)));

            options.AddPolicy(PolicyNames.RequireManager, policy =>
                policy.Requirements.Add(new MinimumRoleRequirement(UserRole.Manager)));

            options.AddPolicy(PolicyNames.RequireAdmin, policy =>
                policy.Requirements.Add(new MinimumRoleRequirement(UserRole.Admin)));
        });

        return services;
    }
}
```

### Authorization Flow

```mermaid
sequenceDiagram
    participant MCP as MCP Client
    participant Pre as Pre-Filter
    participant Auth as IAuthorizationService
    participant Handler as MinimumRoleRequirementHandler
    participant Svc as IUserService
    participant Ctrl as Controller

    MCP->>Pre: call_tool("create")
    Pre->>Pre: Check [Authorize(Policy="RequireMember")]
    Pre->>Auth: AuthorizeAsync(user, policy)
    Auth->>Handler: HandleRequirementAsync(MinimumRole=Member)
    Handler->>Handler: Extract preferred_username claim
    Handler->>Svc: GetAllAsync()
    Svc->>Handler: List<User>
    Handler->>Handler: Find user by email
    Handler->>Handler: Check user.Role >= MinimumRole
    alt Role sufficient
        Handler->>Auth: context.Succeed()
        Auth->>Pre: AuthorizationResult.Succeeded=true
        Pre->>Ctrl: CreateInstance(controller)
        Ctrl->>Pre: ActionResult<User>
        Pre->>Pre: Unwrap ActionResult
        Pre->>MCP: CallToolResult (success)
    else Role insufficient
        Handler->>Auth: (no Succeed call)
        Auth->>Pre: AuthorizationResult.Succeeded=false
        Pre->>MCP: CallToolResult.IsError=true
    end
```

### Role Hierarchy

The `>=` operator in the handler enables role hierarchy:

```csharp
if (user.Role >= requirement.MinimumRole)
```

| Policy | Required Role | Member | Manager | Admin |
|--------|--------------|--------|---------|-------|
| RequireMember | Member (1) | ✅ | ✅ | ✅ |
| RequireManager | Manager (2) | ❌ | ✅ | ✅ |
| RequireAdmin | Admin (3) | ❌ | ❌ | ✅ |

---

## Testing Infrastructure

### Test Fixture

File: `tests/McpPoc.Api.Tests/McpApiFixture.cs`

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;

namespace McpPoc.Api.Tests;

public class McpApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly KeycloakTokenHelper _tokenHelper;
    private readonly Dictionary<string, string> _tokenCache = new();

    public McpApiFixture()
    {
        _tokenHelper = new KeycloakTokenHelper();
    }

    public async Task InitializeAsync()
    {
        // Pre-warm Keycloak connection
        await _tokenHelper.GetClientCredentialsTokenAsync();
    }

    public new Task DisposeAsync()
    {
        _tokenCache.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets authenticated client using client_credentials flow (no user context).
    /// </summary>
    public async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        const string cacheKey = "client_credentials";

        if (!_tokenCache.TryGetValue(cacheKey, out var token))
        {
            token = await _tokenHelper.GetClientCredentialsTokenAsync();
            _tokenCache[cacheKey] = token;
        }

        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://127.0.0.1")
        });

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    /// <summary>
    /// Gets authenticated client using password flow (with user context for authorization).
    /// </summary>
    public async Task<HttpClient> GetAuthenticatedClientAsync(string username, string password)
    {
        string cacheKey = $"user:{username}";

        if (!_tokenCache.TryGetValue(cacheKey, out var token))
        {
            token = await _tokenHelper.GetPasswordTokenAsync(username, password);
            _tokenCache[cacheKey] = token;
        }

        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://127.0.0.1")
        });

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    /// <summary>
    /// Gets unauthenticated client for negative testing.
    /// </summary>
    public HttpClient GetUnauthenticatedClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://127.0.0.1")
        });
    }
}

[CollectionDefinition("McpApi")]
public class McpApiCollection : ICollectionFixture<McpApiFixture>
{
}
```

### Keycloak Token Helper

File: `tests/McpPoc.Api.Tests/KeycloakTokenHelper.cs`

```csharp
using System.Text.Json;

namespace McpPoc.Api.Tests;

public class KeycloakTokenHelper
{
    private readonly HttpClient _httpClient;
    private readonly string _tokenEndpoint;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public KeycloakTokenHelper()
    {
        _httpClient = new HttpClient();
        _tokenEndpoint = "http://127.0.0.1:8080/realms/mcppoc-realm/protocol/openid-connect/token";
        _clientId = "mcppoc-api";
        _clientSecret = "your-client-secret-here";
    }

    /// <summary>
    /// Gets token using client_credentials flow (machine-to-machine).
    /// No user context - cannot be used for authorization tests.
    /// </summary>
    public async Task<string> GetClientCredentialsTokenAsync()
    {
        var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret
        });

        var response = await _httpClient.PostAsync(_tokenEndpoint, requestContent);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

        return tokenResponse?.AccessToken
            ?? throw new InvalidOperationException("Failed to get access token");
    }

    /// <summary>
    /// Gets token using password flow (user authentication).
    /// Provides user context with preferred_username claim for authorization.
    /// </summary>
    public async Task<string> GetPasswordTokenAsync(string username, string password)
    {
        var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["username"] = username,
            ["password"] = password
        });

        var response = await _httpClient.PostAsync(_tokenEndpoint, requestContent);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

        return tokenResponse?.AccessToken
            ?? throw new InvalidOperationException("Failed to get access token");
    }

    private class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
    }
}
```

### MCP Client Helper

File: `tests/McpPoc.Api.Tests/McpClientHelper.cs`

```csharp
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace McpPoc.Api.Tests;

/// <summary>
/// Helper for making MCP protocol requests.
/// Manages MCP client lifecycle and provides convenient methods for testing.
/// </summary>
public class McpClientHelper : IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private McpClient? _client;

    public McpClientHelper(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private async Task<McpClient> GetConnectedClientAsync()
    {
        if (_client != null) return _client;

        // Create HTTP transport pointing to /mcp endpoint
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(_httpClient.BaseAddress!, "mcp"),
                TransportMode = HttpTransportMode.AutoDetect
            },
            _httpClient,
            ownsHttpClient: false  // We manage HttpClient lifecycle
        );

        _client = await McpClient.CreateAsync(transport);
        return _client;
    }

    /// <summary>
    /// Lists all available MCP tools.
    /// </summary>
    public async Task<IList<McpClientTool>> ListToolsAsync()
    {
        var client = await GetConnectedClientAsync();
        return await client.ListToolsAsync();
    }

    /// <summary>
    /// Calls an MCP tool with optional arguments.
    /// </summary>
    public async Task<CallToolResult> CallToolAsync(
        string toolName,
        IReadOnlyDictionary<string, object?>? arguments = null)
    {
        var client = await GetConnectedClientAsync();
        return await client.CallToolAsync(toolName, arguments);
    }

    public async ValueTask DisposeAsync()
    {
        if (_client != null)
        {
            await _client.DisposeAsync();
        }
    }
}
```

### Example Test - Tool Discovery

File: `tests/McpPoc.Api.Tests/McpToolDiscoveryTests.cs`

```csharp
namespace McpPoc.Api.Tests;

[Collection("McpApi")]
public class McpToolDiscoveryTests : IAsyncLifetime
{
    private readonly McpApiFixture _fixture;
    private McpClientHelper _mcpClient = null!;

    public McpToolDiscoveryTests(McpApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var httpClient = await _fixture.GetAuthenticatedClientAsync();
        _mcpClient = new McpClientHelper(httpClient);
    }

    public async Task DisposeAsync()
    {
        await _mcpClient.DisposeAsync();
    }

    [Fact]
    public async Task Should_DiscoverSixMcpTools_WhenListingTools()
    {
        // Act
        var tools = await _mcpClient.ListToolsAsync();

        // Assert
        tools.Should().NotBeNull();
        tools.Should().HaveCount(6,
            "GetById, GetAll, Create, Update, PromoteToManager, and GetScopeId should be exposed");

        // Verify expected tool names (SDK converts to snake_case)
        var toolNames = tools.Select(t => t.Name).ToList();
        toolNames.Should().Contain("get_by_id");
        toolNames.Should().Contain("get_all");
        toolNames.Should().Contain("create");
        toolNames.Should().Contain("update");
        toolNames.Should().Contain("promote_to_manager");
        toolNames.Should().Contain("get_scope_id");
    }

    [Fact]
    public async Task Should_NotExposeDeleteEndpoint_AsAnMcpTool()
    {
        // Act
        var tools = await _mcpClient.ListToolsAsync();

        // Assert
        tools
            .Should().NotContain(t => t.Name.Contains("delete", StringComparison.OrdinalIgnoreCase),
                "Delete endpoint should NOT have [McpServerTool] attribute");
    }
}
```

### Example Test - Authorization

File: `tests/McpPoc.Api.Tests/PolicyAuthorizationTests.cs` (excerpt)

```csharp
[Collection("McpApi")]
public class PolicyAuthorizationTests : IAsyncLifetime
{
    private readonly McpApiFixture _fixture;
    private McpClientHelper _memberClient = null!;
    private McpClientHelper _managerClient = null!;
    private McpClientHelper _adminClient = null!;

    public PolicyAuthorizationTests(McpApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Create authenticated clients for each role
        var memberHttp = await _fixture.GetAuthenticatedClientAsync("alice@example.com", "alice123");
        _memberClient = new McpClientHelper(memberHttp);

        var managerHttp = await _fixture.GetAuthenticatedClientAsync("bob@example.com", "bob123");
        _managerClient = new McpClientHelper(managerHttp);

        var adminHttp = await _fixture.GetAuthenticatedClientAsync("carol@example.com", "carol123");
        _adminClient = new McpClientHelper(adminHttp);
    }

    [Fact]
    public async Task Should_AllowCreate_WhenUserIsMember()
    {
        // Arrange - MCP SDK expects nested structure
        var args = new Dictionary<string, object?>
        {
            ["request"] = new Dictionary<string, object?>
            {
                ["name"] = "Test User",
                ["email"] = "test@example.com"
            }
        };

        // Act
        var result = await _memberClient.CallToolAsync("create", args);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().NotBe(true, "Member should be able to create users");
        result.Content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_BlockUpdate_WhenUserIsMember()
    {
        // Arrange
        var args = new Dictionary<string, object?>
        {
            ["id"] = 1,
            ["request"] = new Dictionary<string, object?>
            {
                ["name"] = "Updated Name",
                ["email"] = "updated@example.com"
            }
        };

        // Act
        var result = await _memberClient.CallToolAsync("update", args);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().Be(true,
            "Member should NOT be able to update users - authorization should block this");
        result.Content.Should().NotBeEmpty("error response should contain error details");
    }
}
```

---

## Request Flow & Execution

### HTTP vs MCP Request Flow

```mermaid
graph TB
    subgraph "HTTP Request Flow"
        H1[HTTP Client] -->|GET /api/users/1| H2[ASP.NET Pipeline]
        H2 --> H3[Authentication Middleware]
        H3 --> H4[Authorization Middleware]
        H4 --> H5[Controller Routing]
        H5 --> H6[Create Controller]
        H6 --> H7[Execute Action]
        H7 --> H8[Return ActionResult]
        H8 --> H9[Serialize to JSON]
        H9 --> H10[HTTP Response 200 OK]
    end

    subgraph "MCP Request Flow"
        M1[MCP Client] -->|call_tool get_by_id| M2[/mcp Endpoint]
        M2 --> M3[Require Authorization]
        M3 --> M4[MCP Server]
        M4 --> M5[Find Tool]
        M5 --> M6[Pre-Filter Authorization]
        M6 -->|Check Authorize Attributes| M7{Policy Check}
        M7 -->|Authorized| M8[Create Controller]
        M7 -->|Denied| M9[Return Error]
        M8 --> M10[Execute Method]
        M10 --> M11[ActionResult Unwrapper]
        M11 --> M12[Extract Value]
        M12 --> M13[Serialize to JSON]
        M13 --> M14[MCP Response]
    end

    style M6 fill:#ff9900
    style M11 fill:#ff9900
```

### Detailed MCP Tool Invocation Sequence

```mermaid
sequenceDiagram
    autonumber
    participant C as MCP Client
    participant E as /mcp Endpoint
    participant A as Auth Middleware
    participant M as MCP Server
    participant P as Pre-Filter
    participant AS as IAuthorizationService
    participant H as MinimumRoleRequirementHandler
    participant S as IUserService
    participant CT as Controller
    participant U as Unwrapper

    C->>E: POST /mcp - call_tool("create", args)
    E->>A: Check RequireAuthorization
    A->>A: Validate JWT token
    alt Token Invalid
        A->>C: 401 Unauthorized
    end
    A->>M: Request authorized
    M->>M: Find tool "create"
    M->>P: CreateControllerWithPreFilter(UsersController, Create)
    P->>P: Check [Authorize(Policy="RequireMember")]
    P->>AS: AuthorizeAsync(user, policy="RequireMember")
    AS->>H: HandleRequirementAsync(MinimumRole=Member)
    H->>H: Extract preferred_username claim
    H->>S: GetAllAsync()
    S->>H: List<User>
    H->>H: Find user by email
    H->>H: Check user.Role >= Member
    alt Role Insufficient
        H->>AS: (no Succeed call)
        AS->>P: AuthorizationResult.Succeeded=false
        P->>C: UnauthorizedAccessException → CallToolResult.IsError=true
    end
    H->>AS: context.Succeed(requirement)
    AS->>P: AuthorizationResult.Succeeded=true
    P->>CT: ActivatorUtilities.CreateInstance(UsersController)
    P->>CT: await Create(request)
    CT->>S: CreateAsync(name, email)
    S->>CT: User
    CT->>P: ActionResult<User> (CreatedAtAction)
    P->>U: UnwrapActionResult(result)
    U->>U: Extract Value from ActionResult
    U->>M: User object
    M->>M: Serialize to JSON
    M->>C: CallToolResult{IsError=null, Content=[TextBlock]}
```

### Key Decision Points

1. **Step 2-3**: Endpoint-level authentication via `.RequireAuthorization()`
2. **Step 7-8**: Pre-filter checks `[Authorize]` attributes
3. **Step 10-15**: Authorization handler queries user service for role
4. **Step 19**: Controller only created AFTER authorization passes
5. **Step 23-24**: ActionResult unwrapping for MCP response

---

## Dependency Injection & Scoping

### Scoping Verification

```mermaid
graph LR
    subgraph "Request 1"
        R1[HttpContext] --> S1[RequestServices]
        S1 --> I1[IUserService Instance 1]
        S1 --> T1["IScopedRequestTracker
        RequestId=abc-123"]
    end

    subgraph "Request 2"
        R2[HttpContext] --> S2[RequestServices]
        S2 --> I2[IUserService Instance 2]
        S2 --> T2["IScopedRequestTracker
        RequestId=def-456"]
    end

    style T1 fill:#90EE90
    style T2 fill:#90EE90
```

### Scoped Request Tracker

File: `src/McpPoc.Api/Services/ScopedRequestTracker.cs`

```csharp
namespace McpPoc.Api.Services;

/// <summary>
/// Service to test DI scoping behavior in MCP tool invocations.
/// Each instance gets a unique RequestId when created.
/// If scoping works correctly, each MCP tool call should get a different instance.
/// </summary>
public interface IScopedRequestTracker
{
    Guid RequestId { get; }
    DateTime CreatedAt { get; }
}

public class ScopedRequestTracker : IScopedRequestTracker
{
    public Guid RequestId { get; } = Guid.NewGuid();
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
}
```

### Scoping Test

File: `tests/McpPoc.Api.Tests/DIScopingTests.cs` (excerpt)

```csharp
[Fact]
public async Task Should_CreateNewScope_PerToolInvocation()
{
    // This is the CRITICAL test for DI scoping behavior
    // If scoping works correctly: each call gets different RequestId
    // If scoping is broken: same RequestId returned (shared scope)

    // Act - Call the scope test tool twice
    var result1 = await _mcpClient.CallToolAsync("get_scope_id");
    var result2 = await _mcpClient.CallToolAsync("get_scope_id");

    // Assert
    result1.IsError.Should().NotBe(true, "first tool call should succeed");
    result2.IsError.Should().NotBe(true, "second tool call should succeed");

    // Extract RequestIds from responses
    var json1 = JsonSerializer.Deserialize<JsonElement>(textBlock1.Text);
    var json2 = JsonSerializer.Deserialize<JsonElement>(textBlock2.Text);

    var requestId1 = json1.GetProperty("request_id").GetString();
    var requestId2 = json2.GetProperty("request_id").GetString();

    // CRITICAL ASSERTION: RequestIds must be DIFFERENT
    requestId1.Should().NotBe(requestId2,
        "each MCP tool invocation should create a NEW scope with DIFFERENT RequestId. " +
        "If this fails, DI scoping is broken and EF Core DbContext will have tracking issues!");
}
```

### Why Scoping Matters

**Scoped services are CRITICAL for:**

1. **EF Core DbContext** - Each request must have its own context
2. **Test Isolation** - Tests shouldn't share state
3. **Authorization Handler** - Must query fresh user data per request
4. **Request Tracking** - Unique identifiers per request

**Service Lifetime Choices:**

```csharp
// ✅ Correct - Scoped services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IScopedRequestTracker, ScopedRequestTracker>();
builder.Services.AddScoped<IAuthorizationHandler, MinimumRoleRequirementHandler>();

// ❌ Wrong - Would break test isolation and DbContext tracking
builder.Services.AddSingleton<IUserService, UserService>();
```

---

## Complete Code Reference

### Project Structure

```
net-api-with-mcp/
├── src/
│   └── McpPoc.Api/
│       ├── Authorization/
│       │   ├── AuthorizationServiceExtensions.cs
│       │   ├── MinimumRoleRequirement.cs
│       │   ├── MinimumRoleRequirementHandler.cs
│       │   └── PolicyNames.cs
│       ├── Controllers/
│       │   └── UsersController.cs
│       ├── Extensions/
│       │   └── McpServerBuilderExtensions.cs
│       ├── Models/
│       │   └── User.cs
│       ├── Services/
│       │   ├── IUserService.cs
│       │   └── ScopedRequestTracker.cs
│       ├── appsettings.json
│       └── Program.cs
├── tests/
│   └── McpPoc.Api.Tests/
│       ├── ActionResultSerializationTest.cs
│       ├── AuthenticationTests.cs
│       ├── DIScopingTests.cs
│       ├── HttpAuthorizationTests.cs
│       ├── HttpCoexistenceTests.cs
│       ├── KeycloakTokenHelper.cs
│       ├── McpApiFixture.cs
│       ├── McpClientHelper.cs
│       ├── McpToolDiscoveryTests.cs
│       ├── McpToolInvocationTests.cs
│       └── PolicyAuthorizationTests.cs
├── docker/
│   ├── docker-compose.yml
│   ├── .env
│   └── keycloak/
│       └── mcppoc-realm.json
└── docs/
    ├── MCP-AUTHORIZATION-COMPLETE-GUIDE.md
    └── MCP-COMPLETE-INTEGRATION-GUIDE.md (this file)
```

### Model - User Entity

File: `src/McpPoc.Api/Models/User.cs`

```csharp
namespace McpPoc.Api.Models;

public enum UserRole
{
    Member = 1,
    Manager = 2,
    Admin = 3
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public UserRole Role { get; set; } = UserRole.Member;
}
```

### Service - User Service

File: `src/McpPoc.Api/Services/IUserService.cs`

```csharp
using McpPoc.Api.Models;

namespace McpPoc.Api.Services;

public interface IUserService
{
    Task<User?> GetByIdAsync(int id);
    Task<List<User>> GetAllAsync();
    Task<User> CreateAsync(string name, string email);
}

public class UserService : IUserService
{
    private readonly List<User> _users = new()
    {
        new User { Id = 1, Name = "Alice Smith", Email = "alice@example.com", Role = UserRole.Member },
        new User { Id = 2, Name = "Bob Jones", Email = "bob@example.com", Role = UserRole.Manager },
        new User { Id = 3, Name = "Carol White", Email = "carol@example.com", Role = UserRole.Admin }
    };

    private int _nextId = 4;

    public Task<User?> GetByIdAsync(int id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        return Task.FromResult(user);
    }

    public Task<List<User>> GetAllAsync()
    {
        return Task.FromResult(_users.ToList());
    }

    public Task<User> CreateAsync(string name, string email)
    {
        var user = new User
        {
            Id = _nextId++,
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow,
            Role = UserRole.Member
        };

        _users.Add(user);
        return Task.FromResult(user);
    }
}
```

---

## Common Patterns & Examples

### Pattern: Read-Only Tool

```csharp
[HttpGet]
[McpServerTool, Description("Gets statistics - read-only")]
public async Task<ActionResult<Stats>> GetStats()
{
    var stats = await _statsService.GetCurrentStatsAsync();
    return Ok(stats);
}
```

### Pattern: Tool with Complex Parameters

```csharp
[HttpPost("search")]
[McpServerTool, Description("Searches users with filters")]
public async Task<ActionResult<List<User>>> Search(
    [Description("Search criteria")] SearchRequest request)
{
    var results = await _userService.SearchAsync(request);
    return Ok(results);
}

public record SearchRequest(
    string? Name,
    string? Email,
    UserRole? MinRole
);
```

### Pattern: Tool with Multiple DTOs

```csharp
[HttpPost("bulk")]
[McpServerTool, Description("Creates multiple users at once")]
public async Task<ActionResult<BulkCreateResponse>> BulkCreate(
    [Description("List of users to create")] BulkCreateRequest request)
{
    var created = new List<User>();
    var errors = new List<string>();

    foreach (var userReq in request.Users)
    {
        try
        {
            var user = await _userService.CreateAsync(userReq.Name, userReq.Email);
            created.Add(user);
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to create {userReq.Name}: {ex.Message}");
        }
    }

    return Ok(new BulkCreateResponse(created, errors));
}

public record BulkCreateRequest(List<CreateUserRequest> Users);
public record BulkCreateResponse(List<User> Created, List<string> Errors);
```

### Pattern: Tool with Optional Parameters

```csharp
[HttpGet("list")]
[McpServerTool, Description("Lists users with pagination")]
public async Task<ActionResult<PagedResponse<User>>> ListUsers(
    [Description("Page number (default: 1)")] int page = 1,
    [Description("Page size (default: 10)")] int pageSize = 10)
{
    var users = await _userService.GetPagedAsync(page, pageSize);
    return Ok(users);
}
```

### Pattern: Conditional Authorization

```csharp
// Method-level policy overrides class-level
[ApiController]
[Route("api/[controller]")]
[Authorize]  // Default: just authenticated
[McpServerToolType]
public class DocumentsController : ControllerBase
{
    [HttpGet("{id}")]
    [McpServerTool]
    [AllowAnonymous]  // Override: allow public access
    public async Task<ActionResult<Document>> GetPublic(int id)
    {
        // ...
    }

    [HttpPost]
    [McpServerTool]
    [Authorize(Policy = PolicyNames.RequireManager)]  // Override: stricter
    public async Task<ActionResult<Document>> Create(CreateDocumentRequest request)
    {
        // ...
    }
}
```

---

## Troubleshooting

### Issue 1: Tool Not Discovered

**Symptom:** Tool doesn't appear in `list_tools` response

**Checklist:**
- ✅ Controller has `[McpServerToolType]` attribute
- ✅ Method has `[McpServerTool]` attribute
- ✅ Method is public
- ✅ Controller is in assembly being scanned

**Solution:**
```csharp
// Ensure both attributes are present
[McpServerToolType]  // ← On controller class
public class UsersController : ControllerBase
{
    [McpServerTool]  // ← On method
    public async Task<ActionResult<User>> GetById(int id)
    {
        // ...
    }
}
```

### Issue 2: Authorization Always Fails

**Symptom:** All MCP tool calls return "Access denied"

**Debugging Steps:**

1. Check logs for authentication failures:
```bash
tail -f logs/mcppoc-*.log | grep -i "auth"
```

2. Verify JWT token has required claims:
```bash
# Decode token at https://jwt.io or:
echo $TOKEN | cut -d. -f2 | base64 -d | jq
```

3. Check `preferred_username` claim exists:
```json
{
  "preferred_username": "alice@example.com",
  "email": "alice@example.com"
}
```

4. Verify user exists in UserService:
```csharp
// Add logging to handler
_logger.LogTrace("Looking for user: {Username}", usernameClaim);
var user = users.FirstOrDefault(u => u.Email == usernameClaim);
_logger.LogTrace("User found: {Found}, Role: {Role}", user != null, user?.Role);
```

### Issue 3: Parameter Binding Fails

**Symptom:** Tool call returns error about missing or invalid parameters

**Root Cause:** MCP SDK requires nested parameter structure for DTOs

**Wrong:**
```csharp
var args = new Dictionary<string, object?>
{
    ["name"] = "Test User",
    ["email"] = "test@example.com"
};
```

**Correct:**
```csharp
var args = new Dictionary<string, object?>
{
    ["request"] = new Dictionary<string, object?>
    {
        ["name"] = "Test User",
        ["email"] = "test@example.com"
    }
};
```

### Issue 4: IsError Assertion Fails

**Symptom:** Test fails with "Expected False but found <null>"

**Root Cause:** MCP SDK returns `IsError = null` for success, not `false`

**Wrong:**
```csharp
result.IsError.Should().BeFalse();  // ❌ Fails when null
```

**Correct:**
```csharp
result.IsError.Should().NotBe(true);  // ✅ Works with null
```

### Issue 5: DI Scoping Issues

**Symptom:** Tests fail with shared state between test runs

**Root Cause:** Services registered as Singleton instead of Scoped

**Wrong:**
```csharp
builder.Services.AddSingleton<IUserService, UserService>();
```

**Correct:**
```csharp
builder.Services.AddScoped<IUserService, UserService>();
```

**Verification Test:**
```csharp
[Fact]
public async Task Should_CreateNewScope_PerToolInvocation()
{
    var result1 = await _mcpClient.CallToolAsync("get_scope_id");
    var result2 = await _mcpClient.CallToolAsync("get_scope_id");

    var id1 = ExtractRequestId(result1);
    var id2 = ExtractRequestId(result2);

    id1.Should().NotBe(id2, "each call should get new scope");
}
```

### Issue 6: Keycloak Connection Slow

**Symptom:** Tests take 5-10 seconds per authentication

**Root Cause:** DNS lookup for "localhost" is slow

**Wrong:**
```csharp
_tokenEndpoint = "http://localhost:8080/realms/...";  // Slow DNS
```

**Correct:**
```csharp
_tokenEndpoint = "http://127.0.0.1:8080/realms/...";  // Fast, direct IP
```

**Performance Impact:**
- localhost: ~5000ms per token request
- 127.0.0.1: ~39ms per token request

### Issue 7: 401 on /mcp Endpoint

**Symptom:** MCP client gets 401 Unauthorized when connecting

**Root Cause:** Forgot `.RequireAuthorization()` or token not provided

**Check:**
```csharp
// Program.cs - Ensure endpoint requires auth
app.MapMcp("/mcp").RequireAuthorization();

// Test - Ensure client has token
var client = await _fixture.GetAuthenticatedClientAsync();
// NOT: var client = _fixture.GetUnauthenticatedClient();
```

---

## Critical Discoveries

### 1. ActionResult Marshaller Bug

**Issue:** Using `new ValueTask<object?>(result)` loses the value

**Solution:** Use `ValueTask.FromResult(result)`

```csharp
// ❌ Wrong - loses value
private static ValueTask<object?> UnwrapActionResult(object? result, ...)
{
    return new ValueTask<object?>(UnwrapIfActionResult(result));
}

// ✅ Correct - preserves value
private static ValueTask<object?> UnwrapActionResult(object? result, ...)
{
    var unwrapped = UnwrapIfActionResult(result);
    return ValueTask.FromResult(unwrapped);
}
```

### 2. Keycloak Audience Claim

**Issue:** Keycloak uses `azp` claim, not `aud`

**Solution:** Disable audience validation

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateAudience = false,  // Keycloak uses 'azp' instead of 'aud'
    ValidateIssuer = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true
};
```

### 3. MCP SDK Metadata Collection

**Feature:** SDK automatically collects `[Authorize]` and `[Description]` attributes

The SDK introspects method attributes and includes them in tool metadata:

```csharp
[HttpPost]
[McpServerTool, Description("Creates a new user")]
[Authorize(Policy = PolicyNames.RequireMember)]
public async Task<ActionResult<User>> Create(CreateUserRequest request)
```

Generates tool schema:
```json
{
  "name": "create",
  "description": "Creates a new user",
  "inputSchema": {
    "type": "object",
    "properties": {
      "request": {
        "type": "object",
        "properties": {
          "name": { "type": "string" },
          "email": { "type": "string" }
        }
      }
    }
  }
}
```

### 4. MCP DTO Parameter Binding Requires Nesting

**Critical Pattern:** MCP SDK requires DTOs to be nested inside parameter name wrapper

When you define a controller method with a DTO parameter:

```csharp
public async Task<ActionResult<User>> Create(CreateUserRequest request)
```

The MCP client MUST pass arguments like this:

```csharp
// ✅ Correct - nested structure
var args = new Dictionary<string, object?>
{
    ["request"] = new Dictionary<string, object?>  // Parameter name wraps DTO properties
    {
        ["name"] = "Test User",
        ["email"] = "test@example.com"
    }
};

// ❌ Wrong - flat structure (will fail with parameter binding error)
var args = new Dictionary<string, object?>
{
    ["name"] = "Test User",
    ["email"] = "test@example.com"
};
```

**Why:** ASP.NET Core infers `[FromBody]` for complex types, and MCP SDK uses the parameter name as a JSON property wrapper. This differs from HTTP API calls where the body is sent directly without a wrapper.

**Rule:** Simple types (`int`, `string`) stay flat; complex types (DTOs) must be nested under their parameter name.

### 5. Snake Case Conversion

**Feature:** MCP SDK converts C# method names to snake_case

| C# Name | MCP Tool Name |
|---------|---------------|
| `GetById` | `get_by_id` |
| `PromoteToManager` | `promote_to_manager` |

This is automatic and cannot be disabled.

### 6. FromServices Not Supported in MCP Tools

**Issue:** `[FromServices]` parameter attribute doesn't work in MCP context

**Solution:** Use constructor injection only

```csharp
// ❌ Wrong - doesn't work for MCP tools
public async Task<ActionResult<User>> Create(
    CreateUserRequest request,
    [FromServices] IUserService userService)
{
    // ...
}

// ✅ Correct - use constructor injection
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<ActionResult<User>> Create(CreateUserRequest request)
    {
        var user = await _userService.CreateAsync(...);
        // ...
    }
}
```

### 7. Client Credentials Has No User Context

**Issue:** Token from client_credentials flow has no `preferred_username` claim

**Solution:** Use password grant for authorization tests

```csharp
// ❌ Wrong - no user context
var client = await _fixture.GetAuthenticatedClientAsync();

// ✅ Correct - has preferred_username claim
var client = await _fixture.GetAuthenticatedClientAsync("alice@example.com", "alice123");
```

### 8. HttpContext.RequestServices Already Scoped

**Discovery:** No need for manual scope creation - `RequestServices` is already scoped

The pre-filter uses `HttpContext.RequestServices` which is automatically scoped per request:

```csharp
private static object CreateControllerWithPreFilter(
    IServiceProvider services,  // This is already HttpContext.RequestServices!
    Type controllerType,
    MethodInfo method)
{
    // No manual scope creation needed
    return ActivatorUtilities.CreateInstance(services, controllerType);
}
```

**Result:** Each MCP tool call gets a fresh scope, just like HTTP requests.

---

## Summary

This guide provides **complete, production-ready documentation** for integrating MCP with ASP.NET Core APIs:

✅ **Infrastructure** - Docker Compose with PostgreSQL + Keycloak
✅ **MCP Server** - Custom extension with ActionResult unwrapping
✅ **Controllers** - Dual protocol support (HTTP + MCP)
✅ **Authentication** - JWT Bearer with Keycloak OIDC
✅ **Authorization** - Policy-based with role hierarchy
✅ **Pre-Filter** - Authorization before controller creation
✅ **Testing** - Complete infrastructure for both protocols
✅ **DI Scoping** - Verified for EF Core compatibility
✅ **32/32 Tests Passing** - Full coverage

### Key Takeaways

1. **Seamless Integration** - Same controllers work for both HTTP and MCP
2. **Pre-Filter Authorization** - Security checks BEFORE instantiation
3. **ActionResult Unwrapping** - Custom marshaller extracts values
4. **DTO Nested Parameters** - Complex types must be nested under parameter name
5. **Scoped Services** - Critical for EF Core and test isolation
6. **No Code Duplication** - Single codebase, dual protocols

### Next Steps

- **Phase 5**: Add EF Core with real database (scoping proven ready)
- **Phase 6**: Advanced authorization (claims, custom requirements)
- **Production**: Deploy with verified security and scoping

---

**Version:** 1.0
**Last Updated:** 2025-10-28
**Status:** Production Ready ✅
