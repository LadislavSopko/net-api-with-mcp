# TDDAB Plan v3.1: Mechanical Implementation Notes

> **Critical Implementation Details from Codebase Verification**
> This addendum corrects v3 based on actual codebase inspection.

## Key Corrections from v3

### 1. IAuthForMcpSupplier Interface Signature (CORRECTED)

**v3 had (WRONG)**:
```csharp
public interface IAuthForMcpSupplier
{
    Task<bool> CheckAuthenticatedAsync(); // ❌ No parameter
    Task<bool> CheckPolicyAsync(AuthorizeAttribute attribute);
}
```

**v3.1 correct signature**:
```csharp
public interface IAuthForMcpSupplier
{
    /// <summary>
    /// Checks if the HttpContext has an authenticated user.
    /// </summary>
    /// <param name="httpContext">The HTTP context to check (can be null).</param>
    /// <returns>True if authenticated, false otherwise.</returns>
    Task<bool> CheckAuthenticatedAsync(HttpContext? httpContext);

    /// <summary>
    /// Checks if the user in the HttpContext satisfies the authorization policy.
    /// </summary>
    /// <param name="httpContext">The HTTP context containing the user.</param>
    /// <param name="attribute">The [Authorize] attribute from the controller method or class.</param>
    /// <returns>True if the policy is satisfied, false otherwise.</returns>
    Task<bool> CheckPolicyAsync(HttpContext? httpContext, AuthorizeAttribute attribute);
}
```

**Rationale**: Library must NOT depend on `IHttpContextAccessor` directly. Host passes `HttpContext` explicitly when invoking auth checks.

---

### 2. Source File to Extract (VERIFIED)

**Location**: `src/McpPoc.Api/Extensions/McpServerBuilderExtensions.cs`

**Contains**:
- `WithToolsFromAssemblyUnwrappingActionResult` extension method
- Invocation handler logic (authorization + marshaling + invocation)
- `MarshalResult` logic (NOT "ActionResultUnwrapper" - use correct name)
- Attribute definitions: `[McpServerToolType]`, `[McpServerTool]`

**Action**: Once library is ready, DELETE this file entirely. No other code depends on it.

---

### 3. MarshalResult Logic (Correct Name)

**v3 used wrong name**: `ActionResultUnwrapper`
**v3.1 correct name**: `MarshalResult`

**Requirements**:
1. Must return `null` for `Ok(null)` (nullable types)
2. Must unwrap nested `ActionResult<T>` before serialization
3. Must throw for error results (NotFoundResult, BadRequestResult)

**Implementation signature**:
```csharp
internal static class MarshalResult
{
    public static async ValueTask<object?> UnwrapAsync(object? result)
    {
        // Implementation from v3 TDDAB-2, but use this class name
    }
}
```

---

### 4. Name-Based Argument Binding (VERIFIED CRITICAL)

**Location**: Invocation handler in `WithToolsFromAssemblyUnwrappingActionResult`

**Current (positional)**: `args[i] = context.Arguments[i]` ❌
**Required (name-based)**: Match by parameter name from JSON

**Also update**: `tests/McpPoc.Api.Tests/McpClientHelper.CallToolAsync`
- Helper currently sends positional arguments
- Must be updated to send name-based JSON object

**Example**:
```csharp
// Old positional
await helper.CallToolAsync("get_by_id", userId);

// New name-based
await helper.CallToolAsync("get_by_id", new { id = userId });
```

---

### 5. GetPublicInfo Endpoint (REQUIRED in Host)

**Location**: `src/McpPoc.Api/Controllers/UsersController.cs`

**Add this method**:
```csharp
/// <summary>
/// Public information endpoint for testing [AllowAnonymous] with MCP.
/// </summary>
[HttpGet("public")]
[McpServerTool]
[AllowAnonymous]
public async Task<ActionResult<object>> GetPublicInfo()
{
    return Ok(new
    {
        message = "This is public information accessible without authentication",
        timestamp = DateTime.UtcNow,
        serverVersion = "1.8.0"
    });
}
```

**Also add**:
- HTTP integration test: `GET /api/users/public` (no auth required)
- MCP integration test: `tools/call get_public_info` (no auth required)

---

### 6. Remove Console.WriteLine from Tests

**Location**: `tests/McpPoc.Api.Tests/McpClientHelper.cs` and related test helpers

**Action**: When migrating helper code to new test project:
- Remove all `Console.WriteLine` diagnostics
- Use `ILogger` if diagnostics are needed
- Or drop them entirely for cleaner tests

---

### 7. Directory.Packages.props Updates (CONFIRMED)

Add to `/mnt/d/Projekty/AI_Works/net-api-with-mcp/Directory.Packages.props`:

```xml
<ItemGroup>
  <!-- For McpApiExtensions library -->
  <PackageVersion Include="Microsoft.AspNetCore.Authorization" Version="9.0.0" />
  <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />
  <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.0" />

  <!-- For McpApiExtensions.Tests -->
  <PackageVersion Include="Moq" Version="4.20.72" />
</ItemGroup>
```

---

### 8. Host Wiring Changes (MINIMAL)

**File**: `src/McpPoc.Api/Program.cs`

**Before (current)**:
```csharp
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssemblyUnwrappingActionResult(); // Old extension
```

**After (v3.1)**:
```csharp
using McpApiExtensions;
using McpPoc.Api.Infrastructure;

// Register auth supplier
builder.Services.AddScoped<IAuthForMcpSupplier, KeycloakAuthSupplier>();

// Use library extension
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssemblyUnwrappingActionResult(); // Now from library
```

**Then DELETE**: `src/McpPoc.Api/Extensions/McpServerBuilderExtensions.cs`

---

### 9. Library Dependency Constraints (CRITICAL)

**Library MUST NOT depend on**:
- ❌ `Microsoft.AspNetCore.Http` (for HttpContext)
- ❌ `Microsoft.AspNetCore.Authorization` (for IAuthorizationService)
- ❌ `Microsoft.Extensions.Logging` (for ILoggerFactory)

**Library MAY depend on**:
- ✅ `Microsoft.AspNetCore.Authorization` (for AuthorizeAttribute, AllowAnonymousAttribute)
- ✅ `Microsoft.Extensions.Logging.Abstractions` (for ILogger interface only)
- ✅ `Microsoft.Extensions.DependencyInjection.Abstractions` (for DI registration)
- ✅ `ModelContextProtocol.AspNetCore` (for MCP integration)

**Rationale**: Library provides mechanics, host provides dependencies (HttpContext, IAuthorizationService, logging).

---

### 10. KeycloakAuthSupplier Implementation (CORRECTED)

**Location**: `src/McpPoc.Api/Infrastructure/KeycloakAuthSupplier.cs`

**v3.1 corrected implementation**:
```csharp
using McpApiExtensions;
using Microsoft.AspNetCore.Authorization;

namespace McpPoc.Api.Infrastructure;

public class KeycloakAuthSupplier : IAuthForMcpSupplier
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<KeycloakAuthSupplier> _logger;

    public KeycloakAuthSupplier(
        IAuthorizationService authorizationService,
        ILogger<KeycloakAuthSupplier> logger)
    {
        _authorizationService = authorizationService;
        _logger = logger;
    }

    public Task<bool> CheckAuthenticatedAsync(HttpContext? httpContext)
    {
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext is null in KeycloakAuthSupplier");
            return Task.FromResult(false);
        }

        var isAuthenticated = httpContext.User?.Identity?.IsAuthenticated ?? false;

        _logger.LogTrace(
            "Authentication check: {IsAuthenticated} for user {User}",
            isAuthenticated,
            httpContext.User?.Identity?.Name ?? "anonymous");

        return Task.FromResult(isAuthenticated);
    }

    public async Task<bool> CheckPolicyAsync(HttpContext? httpContext, AuthorizeAttribute attribute)
    {
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext is null in KeycloakAuthSupplier.CheckPolicyAsync");
            return false;
        }

        if (string.IsNullOrEmpty(attribute.Policy))
        {
            _logger.LogWarning("Policy is null or empty in [Authorize] attribute");
            return false;
        }

        _logger.LogTrace(
            "Checking policy '{Policy}' for user {User}",
            attribute.Policy,
            httpContext.User?.Identity?.Name ?? "anonymous");

        var authResult = await _authorizationService.AuthorizeAsync(
            httpContext.User,
            null,
            attribute.Policy);

        if (authResult.Succeeded)
        {
            _logger.LogTrace("Policy '{Policy}' check succeeded", attribute.Policy);
        }
        else
        {
            _logger.LogWarning(
                "Policy '{Policy}' check failed for user {User}. Failures: {Failures}",
                attribute.Policy,
                httpContext.User?.Identity?.Name ?? "anonymous",
                string.Join(", ", authResult.Failure?.FailureReasons.Select(r => r.Message) ?? Array.Empty<string>()));
        }

        return authResult.Succeeded;
    }
}
```

---

### 11. McpAuthorizationPreFilter Invocation (CORRECTED)

**Location**: TDDAB-4, invocation handler

**v3.1 corrected invocation**:
```csharp
// Inside the invocation handler lambda
var httpContextAccessor = context.GetRequiredService<IHttpContextAccessor>();
var httpContext = httpContextAccessor.HttpContext
    ?? throw new InvalidOperationException("HttpContext is null");

var authSupplier = serviceProvider.GetRequiredService<IAuthForMcpSupplier>();
var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger(typeof(McpAuthorizationPreFilter));

var preFilter = new McpAuthorizationPreFilter(authSupplier, logger);

// CORRECTED: Pass httpContext to CheckAuthorizationAsync
var isAuthorized = await preFilter.CheckAuthorizationAsync(method, httpContext);
```

**McpAuthorizationPreFilter signature**:
```csharp
public async Task<bool> CheckAuthorizationAsync(MethodInfo methodInfo, HttpContext? httpContext)
{
    // Check [AllowAnonymous] first
    // ...

    // Get all [Authorize] attributes
    var authorizeAttributes = methodInfo.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
        .Concat(methodInfo.DeclaringType?.GetCustomAttributes<AuthorizeAttribute>(inherit: true) ?? ...)
        .ToList();

    // Check authentication
    var isAuthenticated = await _authSupplier.CheckAuthenticatedAsync(httpContext);

    // Check all policies
    foreach (var attr in authorizeAttributes)
    {
        if (!string.IsNullOrEmpty(attr.Policy))
        {
            var policyResult = await _authSupplier.CheckPolicyAsync(httpContext, attr);
            if (!policyResult) return false;
        }
    }

    return true;
}
```

---

## Summary of v3 → v3.1 Changes

| Item | v3 (Wrong) | v3.1 (Correct) | Impact |
|------|------------|----------------|--------|
| **IAuthForMcpSupplier** | No HttpContext param | Takes `HttpContext?` | CRITICAL |
| **Class name** | ActionResultUnwrapper | MarshalResult | Naming |
| **Source file** | Not specified | `Extensions/McpServerBuilderExtensions.cs` | Verified |
| **Helper tests** | Not mentioned | Must update `McpClientHelper.CallToolAsync` | CRITICAL |
| **GetPublicInfo** | Optional | REQUIRED with tests | Required |
| **Console.WriteLine** | Not mentioned | Must remove from tests | Cleanup |
| **Dependencies** | Unclear | Explicit constraints | Design |

---

## Implementation Order (Updated)

1. **TDDAB-1**: Create library project + IAuthForMcpSupplier with **corrected signature**
2. **TDDAB-2**: Move MarshalResult (NOT ActionResultUnwrapper) with null support
3. **TDDAB-3**: Move authorization pre-filter with **httpContext parameter**
4. **TDDAB-4**: Move invocation handler with **name-based binding**
5. **TDDAB-5**: Create KeycloakAuthSupplier with **corrected signature**
6. **TDDAB-6**: Refactor host (delete old file, use library)
7. **TDDAB-7**: Package metadata
8. **TDDAB-8**: Integration tests (**GetPublicInfo REQUIRED**)

---

## Ready for ACT

These mechanical corrections ensure the implementation aligns with the actual codebase structure. The plan is now mechanically verified and production-ready.

**Type ACT when ready to implement TDDAB-1 with these corrections!** 🚀
