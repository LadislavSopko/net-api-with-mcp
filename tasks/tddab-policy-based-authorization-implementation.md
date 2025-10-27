# Agent-Optimized TDDAB Plan: Policy-Based Authorization for MCP Tools

§TDDAB:Agent-Optimized
@created::2025-10-27
@approach::FilterPipeline+PolicyBased+RoleHierarchy
@extends::existing-authorization-pattern

## Context

@current::DI-Scoping-Verified{22/22-tests✓}
@problem::MCP-tools-need-policy-based-authorization
@pattern::User's-existing-MinimumRoleRequirement+IAuthorizationHandler
@goal::Implement-pre-filter-authorization-check-BEFORE-tool-execution

## Prerequisites

✅ DI scoping verified (scoped services work correctly)
✅ IHttpContextAccessor registered (Program.cs:110)
✅ Endpoint-level auth working (MapMcp("/mcp").RequireAuthorization())
✅ 22/22 tests passing

## Architecture Overview

```
MCP Request Flow (with Pre-Filter):
Client → /mcp [RequireAuthorization ✓]
       → HttpContext available with User.Identity
       → McpServer
       → AIFunction.Invoke
       → TargetFactory (line 75) [PRE-FILTER HERE ✓]
          ├─ Check [Authorize] attribute
          ├─ Verify User.Identity.IsAuthenticated
          ├─ Check Policy (if specified)
          ├─ Check Roles (if specified)
          └─ Success → Create Controller Instance
       → Controller method execution
       → MarshalResult (line 109) [POST-FILTER]
       → Return result
```

---

## TDDAB-1: Add Authorization Infrastructure

### 1.1 Implementation (Following User's Pattern)

**Create:** `/mnt/d/Projekty/AI_Works/net-api-with-mcp/src/McpPoc.Api/Authorization/PolicyNames.cs`

```csharp
namespace McpPoc.Api.Authorization;

public static class PolicyNames
{
    public const string RequireMember = "RequireMember";
    public const string RequireManager = "RequireManager";
    public const string RequireAdmin = "RequireAdmin";
}
```

**Create:** `/mnt/d/Projekty/AI_Works/net-api-with-mcp/src/McpPoc.Api/Authorization/MinimumRoleRequirement.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using McpPoc.Api.Models;

namespace McpPoc.Api.Authorization;

public class MinimumRoleRequirement : IAuthorizationRequirement
{
    public UserRole MinimumRole { get; }

    public MinimumRoleRequirement(UserRole minimumRole)
    {
        MinimumRole = minimumRole;
    }
}
```

**Create:** `/mnt/d/Projekty/AI_Works/net-api-with-mcp/src/McpPoc.Api/Authorization/MinimumRoleRequirementHandler.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using McpPoc.Api.Services;

namespace McpPoc.Api.Authorization;

public class MinimumRoleRequirementHandler : AuthorizationHandler<MinimumRoleRequirement>
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
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("User is not authenticated");
            return;
        }

        // For POC: Get user by email from claims (Keycloak provides email in token)
        var emailClaim = context.User.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(emailClaim))
        {
            _logger.LogWarning("No email claim found in token");
            return;
        }

        // Get user from service
        var users = await _userService.GetAllAsync();
        var user = users.FirstOrDefault(u => u.Email == emailClaim);

        if (user != null && user.Role >= requirement.MinimumRole)
        {
            _logger.LogInformation(
                "User {Email} with role {Role} meets minimum role {MinRole}",
                user.Email, user.Role, requirement.MinimumRole);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "User {Email} with role {Role} does NOT meet minimum role {MinRole}",
                emailClaim, user?.Role, requirement.MinimumRole);
        }
    }
}
```

**Create:** `/mnt/d/Projekty/AI_Works/net-api-with-mcp/src/McpPoc.Api/Authorization/AuthorizationServiceExtensions.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using McpPoc.Api.Models;

namespace McpPoc.Api.Authorization;

public static class AuthorizationServiceExtensions
{
    public static IServiceCollection AddMcpPocAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, MinimumRoleRequirementHandler>();

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

**Update:** `/mnt/d/Projekty/AI_Works/net-api-with-mcp/src/McpPoc.Api/Models/User.cs`

Add UserRole enum (if not exists):

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
    public DateTime CreatedAt { get; set; }
    public UserRole Role { get; set; } = UserRole.Member;  // Add this property
}
```

**Update:** `/mnt/d/Projekty/AI_Works/net-api-with-mcp/src/McpPoc.Api/Services/UserService.cs`

Update existing users to have roles:

```csharp
public UserService()
{
    _users =
    [
        new User { Id = 1, Name = "Alice Smith", Email = "alice@example.com", CreatedAt = DateTime.UtcNow, Role = UserRole.Member },
        new User { Id = 2, Name = "Bob Jones", Email = "bob@example.com", CreatedAt = DateTime.UtcNow, Role = UserRole.Manager },
        new User { Id = 3, Name = "Charlie Brown", Email = "charlie@example.com", CreatedAt = DateTime.UtcNow, Role = UserRole.Admin }
    ];
}
```

**Update:** `/mnt/d/Projekty/AI_Works/net-api-with-mcp/src/McpPoc.Api/Program.cs`

Add authorization registration after line 107:

```csharp
builder.Services.AddAuthorization();

// Add MCP POC authorization policies
builder.Services.AddMcpPocAuthorization();

// Add HttpContextAccessor for accessing HttpContext in filter pipeline
builder.Services.AddHttpContextAccessor();
```

### 1.2 Verification (Agent-Optimized)

```bash
Use build-agent to build McpPoc.Api
→ Expected: ✅ CLEAN (0 errors, 0 warnings)
```

---

## TDDAB-2: Implement Pre-Filter Authorization Check

### 2.1 Implementation (Filter Pipeline)

**Update:** `/mnt/d/Projekty/AI_Works/net-api-with-mcp/src/McpPoc.Api/Extensions/McpServerBuilderExtensions.cs`

Add using statements at top:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Reflection;
```

Replace the instance method block (around line 71-89) with:

```csharp
else
{
    // Instance method - capture MethodInfo for pre-filter authorization
    var methodCopy = method; // Capture in closure

    builder.Services.AddSingleton<McpServerTool>(services =>
    {
        var aiFunction = AIFunctionFactory.Create(
            methodCopy,
            args => CreateControllerWithPreFilter(args.Services!, toolType, methodCopy),
            new AIFunctionFactoryOptions
            {
                Name = ConvertToSnakeCase(methodCopy.Name),
                MarshalResult = UnwrapActionResult,
                SerializerOptions = serializerOptions
            });

        return McpServerTool.Create(aiFunction, new McpServerToolCreateOptions
        {
            Services = services
        });
    });
}
```

Add new method before `CreateControllerInstance` method:

```csharp
/// <summary>
/// Creates a controller instance with pre-filter authorization check.
/// Checks [Authorize] attribute BEFORE creating controller.
/// </summary>
private static object CreateControllerWithPreFilter(
    IServiceProvider services,
    Type controllerType,
    MethodInfo method)
{
    var logger = services.GetService<ILogger<McpServerBuilderExtensions>>();

    // Get HttpContext via IHttpContextAccessor
    var httpContextAccessor = services.GetService<IHttpContextAccessor>();
    var httpContext = httpContextAccessor?.HttpContext;

    if (httpContext == null)
    {
        logger?.LogError("HttpContext is null - cannot perform authorization check");
        throw new InvalidOperationException("HttpContext not available for authorization");
    }

    // Check for [Authorize] attribute on method or class
    var methodAuthorize = method.GetCustomAttribute<AuthorizeAttribute>();
    var classAuthorize = controllerType.GetCustomAttribute<AuthorizeAttribute>();
    var authorizeAttr = methodAuthorize ?? classAuthorize;

    if (authorizeAttr != null)
    {
        // 1. Check authentication
        if (!httpContext.User?.Identity?.IsAuthenticated ?? true)
        {
            logger?.LogWarning("Authorization failed: User not authenticated for {Method}", method.Name);
            throw new UnauthorizedAccessException("Authentication required");
        }

        logger?.LogTrace("User authenticated: {User}", httpContext.User.Identity.Name);

        // 2. Check policy if specified
        if (!string.IsNullOrEmpty(authorizeAttr.Policy))
        {
            var authService = services.GetRequiredService<IAuthorizationService>();

            // Synchronous auth check (IAuthorizationService.AuthorizeAsync is async)
            // We need to block here because TargetFactory is synchronous
            var authResult = authService.AuthorizeAsync(
                httpContext.User,
                httpContext,
                authorizeAttr.Policy).GetAwaiter().GetResult();

            if (!authResult.Succeeded)
            {
                var reasons = string.Join(", ", authResult.Failure?.FailureReasons.Select(r => r.Message) ?? []);
                logger?.LogWarning(
                    "Authorization failed: Policy {Policy} denied for {Method}. Reasons: {Reasons}",
                    authorizeAttr.Policy, method.Name, reasons);
                throw new UnauthorizedAccessException(
                    $"Access denied: Policy '{authorizeAttr.Policy}' not satisfied");
            }

            logger?.LogInformation(
                "Policy authorization succeeded: {Policy} for {Method}",
                authorizeAttr.Policy, method.Name);
        }

        // 3. Check roles if specified
        if (!string.IsNullOrEmpty(authorizeAttr.Roles))
        {
            var roles = authorizeAttr.Roles.Split(',').Select(r => r.Trim());
            var hasRole = roles.Any(role => httpContext.User.IsInRole(role));

            if (!hasRole)
            {
                logger?.LogWarning(
                    "Authorization failed: User does not have required role {Roles} for {Method}",
                    authorizeAttr.Roles, method.Name);
                throw new UnauthorizedAccessException(
                    $"Access denied: Required role '{authorizeAttr.Roles}'");
            }

            logger?.LogInformation(
                "Role authorization succeeded: {Roles} for {Method}",
                authorizeAttr.Roles, method.Name);
        }
    }

    // Authorization passed - create controller instance
    return ActivatorUtilities.CreateInstance(services, controllerType);
}
```

### 2.2 Verification (Agent-Optimized)

```bash
Use build-agent to build McpPoc.Api
→ Expected: ✅ CLEAN (0 errors, 0 warnings)
```

---

## TDDAB-3: Add Test Users with Different Roles to Keycloak

### 3.1 Implementation

**Update:** `/mnt/d/Projekty/AI_Works/net-api-with-mcp/docker/keycloak/mcppoc-realm.json`

Add test users with different roles in the `users` section:

```json
{
  "username": "member@test.com",
  "email": "member@test.com",
  "enabled": true,
  "emailVerified": true,
  "credentials": [
    {
      "type": "password",
      "value": "member123",
      "temporary": false
    }
  ]
},
{
  "username": "manager@test.com",
  "email": "manager@test.com",
  "enabled": true,
  "emailVerified": true,
  "credentials": [
    {
      "type": "password",
      "value": "manager123",
      "temporary": false
    }
  ]
},
{
  "username": "admin@test.com",
  "email": "admin@test.com",
  "enabled": true,
  "emailVerified": true,
  "credentials": [
    {
      "type": "password",
      "value": "admin123",
      "temporary": false
    }
  ]
}
```

**Update:** `/mnt/d/Projekty/AI_Works/net-api-with-mcp/src/McpPoc.Api/Services/UserService.cs`

Match user emails to Keycloak users:

```csharp
_users =
[
    new User { Id = 1, Name = "Member User", Email = "member@test.com", CreatedAt = DateTime.UtcNow, Role = UserRole.Member },
    new User { Id = 2, Name = "Manager User", Email = "manager@test.com", CreatedAt = DateTime.UtcNow, Role = UserRole.Manager },
    new User { Id = 3, Name = "Admin User", Email = "admin@test.com", CreatedAt = DateTime.UtcNow, Role = UserRole.Admin }
];
```

### 3.2 Manual Verification

```bash
# Restart Keycloak to load new realm configuration
docker compose restart keycloak

# Wait for Keycloak to start
sleep 20

# Verify Keycloak is ready
curl http://127.0.0.1:8080
```

---

## TDDAB-4: Add Policy-Protected Tools to Controller

### 4.1 Implementation

**Update:** `/mnt/d/Projekty/AI_Works/net-api-with-mcp/src/McpPoc.Api/Controllers/UsersController.cs`

Add using statement:

```csharp
using McpPoc.Api.Authorization;
```

Update existing methods and add new ones:

```csharp
// EXISTING: Anyone authenticated (endpoint-level auth)
[HttpGet("{id}")]
[McpServerTool, Description("Gets a user by their ID")]
public async Task<ActionResult<User>> GetById(int id)
{
    // ... existing code
}

// EXISTING: Anyone authenticated
[HttpGet]
[McpServerTool, Description("Gets all users")]
public async Task<ActionResult<List<User>>> GetAll()
{
    // ... existing code
}

// NEW: Only Members and above
[HttpPost]
[McpServerTool, Description("Creates a new user - requires Member role")]
[Authorize(Policy = PolicyNames.RequireMember)]
public async Task<ActionResult<User>> Create(
    [Description("User's full name")] string name,
    [Description("User's email address")] string email)
{
    _logger.LogInformation("Create called with name: {Name}, email: {Email}", name, email);

    var user = await _userService.CreateAsync(name, email);
    return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
}

// NEW: Only Managers and above
[HttpPut("{id}")]
[McpServerTool, Description("Updates a user - requires Manager role")]
[Authorize(Policy = PolicyNames.RequireManager)]
public async Task<ActionResult<User>> Update(
    int id,
    [Description("Updated name")] string name,
    [Description("Updated email")] string email)
{
    _logger.LogInformation("Update called for id: {Id}", id);

    var user = await _userService.GetByIdAsync(id);
    if (user == null)
    {
        return NotFound(new { error = "User not found", id });
    }

    user.Name = name;
    user.Email = email;

    return Ok(user);
}

// EXISTING: Only Admins (no [McpServerTool] - HTTP only)
[HttpDelete("{id}")]
public Task<IActionResult> Delete(int id)
{
    // ... existing code
}

// NEW: Only Admins can promote users
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
```

### 4.2 Verification (Agent-Optimized)

```bash
Use build-agent to build McpPoc.Api
→ Expected: ✅ CLEAN (0 errors, 0 warnings)
```

---

## TDDAB-5: Add Policy Authorization Tests

### 5.1 Tests First (These will VERIFY policies work)

**Create:** `/mnt/d/Projekty/AI_Works/net-api-with-mcp/tests/McpPoc.Api.Tests/PolicyAuthorizationTests.cs`

```csharp
using System.Text.Json;
using FluentAssertions;
using ModelContextProtocol.Protocol;

namespace McpPoc.Api.Tests;

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
        var memberHttp = await _fixture.GetAuthenticatedClientAsync("member@test.com", "member123");
        _memberClient = new McpClientHelper(memberHttp);

        var managerHttp = await _fixture.GetAuthenticatedClientAsync("manager@test.com", "manager123");
        _managerClient = new McpClientHelper(managerHttp);

        var adminHttp = await _fixture.GetAuthenticatedClientAsync("admin@test.com", "admin123");
        _adminClient = new McpClientHelper(adminHttp);
    }

    public async Task DisposeAsync()
    {
        await _memberClient.DisposeAsync();
        await _managerClient.DisposeAsync();
        await _adminClient.DisposeAsync();
    }

    [Fact]
    public async Task Should_AllowCreate_WhenUserIsMember()
    {
        // Arrange - Member user calling Member-protected tool
        var args = new Dictionary<string, object?>
        {
            ["name"] = "Test User",
            ["email"] = "test@example.com"
        };

        // Act
        var result = await _memberClient.CallToolAsync("create", args);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse("Member should be able to create users");
        result.Content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_AllowUpdate_WhenUserIsManager()
    {
        // Arrange - Manager user calling Manager-protected tool
        var args = new Dictionary<string, object?>
        {
            ["id"] = 1,
            ["name"] = "Updated Name",
            ["email"] = "updated@example.com"
        };

        // Act
        var result = await _managerClient.CallToolAsync("update", args);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse("Manager should be able to update users");
        result.Content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_BlockUpdate_WhenUserIsMember()
    {
        // Arrange - Member user trying to call Manager-protected tool
        var args = new Dictionary<string, object?>
        {
            ["id"] = 1,
            ["name"] = "Updated Name",
            ["email"] = "updated@example.com"
        };

        // Act
        var result = await _memberClient.CallToolAsync("update", args);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue("Member should NOT be able to update users");

        var textBlock = result.Content.First().Should().BeOfType<TextContentBlock>().Subject;
        textBlock.Text.Should().Contain("Access denied", "error message should indicate access denied");
        textBlock.Text.Should().Contain("RequireManager", "error should mention the policy");
    }

    [Fact]
    public async Task Should_AllowPromote_WhenUserIsAdmin()
    {
        // Arrange - Admin user calling Admin-protected tool
        var args = new Dictionary<string, object?>
        {
            ["id"] = 1
        };

        // Act
        var result = await _adminClient.CallToolAsync("promote_to_manager", args);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse("Admin should be able to promote users");
        result.Content.Should().NotBeEmpty();

        var textBlock = result.Content.First().Should().BeOfType<TextContentBlock>().Subject;
        var json = JsonSerializer.Deserialize<JsonElement>(textBlock.Text);
        json.GetProperty("role").GetInt32().Should().Be(2, "user should be promoted to Manager (role 2)");
    }

    [Fact]
    public async Task Should_BlockPromote_WhenUserIsManager()
    {
        // Arrange - Manager user trying to call Admin-protected tool
        var args = new Dictionary<string, object?>
        {
            ["id"] = 1
        };

        // Act
        var result = await _managerClient.CallToolAsync("promote_to_manager", args);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue("Manager should NOT be able to promote users");

        var textBlock = result.Content.First().Should().BeOfType<TextContentBlock>().Subject;
        textBlock.Text.Should().Contain("Access denied", "error message should indicate access denied");
        textBlock.Text.Should().Contain("RequireAdmin", "error should mention the policy");
    }

    [Fact]
    public async Task Should_BlockPromote_WhenUserIsMember()
    {
        // Arrange - Member user trying to call Admin-protected tool
        var args = new Dictionary<string, object?>
        {
            ["id"] = 1
        };

        // Act
        var result = await _memberClient.CallToolAsync("promote_to_manager", args);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue("Member should NOT be able to promote users");

        var textBlock = result.Content.First().Should().BeOfType<TextContentBlock>().Subject;
        textBlock.Text.Should().Contain("Access denied");
    }
}
```

### 5.2 Verification (Agent-Optimized)

```bash
Use build-agent to build McpPoc.Api.Tests
→ Expected: ✅ CLEAN (0 errors, 0 warnings)

Use test-agent to run tests for PolicyAuthorizationTests
→ Expected: ✅ ALL PASS (7 tests: 1 allow member, 2 allow manager, 1 allow admin, 3 denials)
```

---

## TDDAB-6: Verify All Tests Still Pass

### 6.1 No Implementation Needed

Existing tests should continue to pass because:
- They use default authenticated client (has access)
- GetById and GetAll don't have policy restrictions (only endpoint-level auth)
- GetScopeId doesn't have policy restrictions

### 6.2 Verification (Agent-Optimized)

```bash
Use test-agent to run tests for McpPoc.Api.Tests
→ Expected: ✅ ALL PASS (29 tests: 22 existing + 7 new)
```

---

## Summary

### Files Created

1. `src/McpPoc.Api/Authorization/PolicyNames.cs` - Policy name constants
2. `src/McpPoc.Api/Authorization/MinimumRoleRequirement.cs` - Custom requirement
3. `src/McpPoc.Api/Authorization/MinimumRoleRequirementHandler.cs` - Authorization handler
4. `src/McpPoc.Api/Authorization/AuthorizationServiceExtensions.cs` - Service registration
5. `tests/McpPoc.Api.Tests/PolicyAuthorizationTests.cs` - Policy tests (7 tests)

### Files Modified

1. `src/McpPoc.Api/Models/User.cs` - Add UserRole enum and Role property
2. `src/McpPoc.Api/Services/UserService.cs` - Update users with roles matching Keycloak
3. `src/McpPoc.Api/Program.cs` - Register authorization policies
4. `src/McpPoc.Api/Extensions/McpServerBuilderExtensions.cs` - Implement pre-filter authorization
5. `src/McpPoc.Api/Controllers/UsersController.cs` - Add policy-protected tools
6. `docker/keycloak/mcppoc-realm.json` - Add test users with different emails
7. `tests/McpPoc.Api.Tests/McpToolDiscoveryTests.cs` - Update to expect 6 tools (was 4)

### Test Summary

**Before:** 22 tests (100% pass)
**After:** 29 tests (100% expected)

**New Tests:**
- PolicyAuthorizationTests: 7 tests
  - 1 test: Member can create (RequireMember policy)
  - 1 test: Manager can update (RequireManager policy)
  - 1 test: Member CANNOT update (blocked by RequireManager)
  - 1 test: Admin can promote (RequireAdmin policy)
  - 1 test: Manager CANNOT promote (blocked by RequireAdmin)
  - 1 test: Member CANNOT promote (blocked by RequireAdmin)

**Tool Count:**
- get_by_id (no policy - endpoint auth only)
- get_all (no policy - endpoint auth only)
- get_scope_id (no policy - endpoint auth only)
- **create** (RequireMember policy)
- **update** (RequireManager policy)
- **promote_to_manager** (RequireAdmin policy)

### Architecture

**Pre-Filter Authorization Flow:**
```
MCP Tool Call
  → TargetFactory (CreateControllerWithPreFilter)
     → Get HttpContext via IHttpContextAccessor
     → Find [Authorize] attribute on method/class
     → If Policy specified:
        → Use IAuthorizationService.AuthorizeAsync()
        → MinimumRoleRequirementHandler checks user role
        → Success: Continue | Failure: Throw UnauthorizedAccessException
     → Create Controller Instance
  → Method Execution
```

**Role Hierarchy:**
- Member (1) - Can create
- Manager (2) - Can create + update
- Admin (3) - Can create + update + promote

### Critical Implementation Details

1. **Synchronous Auth Check**: `GetAwaiter().GetResult()` used because TargetFactory is synchronous
2. **User Lookup**: Matches Keycloak email claim to UserService users
3. **Error Propagation**: Throws `UnauthorizedAccessException` which SDK wraps in MCP error response
4. **Logging**: Comprehensive logging at Trace/Information/Warning levels
5. **Policy Evaluation**: Uses ASP.NET Core's `IAuthorizationService` - standard pattern

### Benefits

✅ **Per-Tool Authorization**: Each tool can have different policy requirements
✅ **Role Hierarchy**: Manager inherits Member permissions, Admin inherits all
✅ **Extensible**: Easy to add new policies or requirements
✅ **Follows User's Pattern**: Exactly matches existing authorization code structure
✅ **Testable**: Integration tests verify each policy works correctly
✅ **Production Ready**: Same authorization as regular ASP.NET Core APIs
