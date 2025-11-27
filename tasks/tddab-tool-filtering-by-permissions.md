# TDDAB Plan: MCP Tool Filtering by User Permissions (v2 - Simplified)

## Problem
```
Viewer calls tools/list → sees 5 tools → tries "create" → 403 Forbidden 😠
```

## Solution
Use SDK's `AddListToolsFilter()` to filter tools/list response based on user role.

## Target
- 3 TDDAB blocks (simplified from 4)
- ~10 new tests
- 54/54 total tests (44 existing + 10 new)

---

## Architecture (Simple!)

```
STARTUP:
  Scan [Authorize(Policy="RequireX")] → store in Dictionary<toolName, minRole>

RUNTIME (tools/list):
  SDK calls our filter → we check user role → return only authorized tools
```

**Components:**
1. `ToolAuthorizationMetadata` - simple record
2. `IToolAuthorizationStore` - dictionary wrapper
3. `AddListToolsFilter()` - SDK's built-in hook

---

## TDDAB Block 1: Metadata Capture

### Goal
Capture authorization requirements during tool registration.

### 1.1 Tests First

**File**: `tests/Zero.Mcp.Extensions.Tests/ToolAuthorizationMetadataTests.cs`

```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

public class ToolAuthorizationMetadataTests
{
    [Fact]
    public void Should_ExtractMinimumRole_FromRequireMemberPolicy()
    {
        // Arrange
        var method = typeof(TestController).GetMethod(nameof(TestController.MemberOnly))!;

        // Act
        var metadata = ToolAuthorizationMetadata.FromMethod(method, "member_only");

        // Assert
        metadata.MinimumRole.Should().Be(1);
    }

    [Fact]
    public void Should_ExtractMinimumRole_FromRequireManagerPolicy()
    {
        var method = typeof(TestController).GetMethod(nameof(TestController.ManagerOnly))!;
        var metadata = ToolAuthorizationMetadata.FromMethod(method, "manager_only");
        metadata.MinimumRole.Should().Be(2);
    }

    [Fact]
    public void Should_ExtractMinimumRole_FromRequireAdminPolicy()
    {
        var method = typeof(TestController).GetMethod(nameof(TestController.AdminOnly))!;
        var metadata = ToolAuthorizationMetadata.FromMethod(method, "admin_only");
        metadata.MinimumRole.Should().Be(3);
    }

    [Fact]
    public void Should_ReturnNullMinimumRole_ForAuthenticatedOnly()
    {
        var method = typeof(TestController).GetMethod(nameof(TestController.AuthenticatedOnly))!;
        var metadata = ToolAuthorizationMetadata.FromMethod(method, "authenticated_only");
        metadata.MinimumRole.Should().BeNull();
    }

    [Fact]
    public void Should_StoreAndRetrieve_Metadata()
    {
        // Arrange
        var store = new ToolAuthorizationStore();

        // Act
        store.Register("create", new ToolAuthorizationMetadata("create", 1));
        store.Register("update", new ToolAuthorizationMetadata("update", 2));

        // Assert
        store.GetMinimumRole("create").Should().Be(1);
        store.GetMinimumRole("update").Should().Be(2);
        store.GetMinimumRole("unknown").Should().BeNull();
    }
}

// Test fixtures
internal class TestController
{
    [Authorize(Policy = "RequireMember")]
    public void MemberOnly() { }

    [Authorize(Policy = "RequireManager")]
    public void ManagerOnly() { }

    [Authorize(Policy = "RequireAdmin")]
    public void AdminOnly() { }

    [Authorize]
    public void AuthenticatedOnly() { }
}
```

### 1.2 Implementation

**File**: `src/Zero.Mcp.Extensions/ToolAuthorizationMetadata.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using System.Reflection;

namespace Zero.Mcp.Extensions;

/// <summary>
/// Authorization metadata for a tool.
/// </summary>
public record ToolAuthorizationMetadata(string ToolName, int? MinimumRole)
{
    /// <summary>
    /// Extracts authorization metadata from method attributes.
    /// </summary>
    public static ToolAuthorizationMetadata FromMethod(MethodInfo method, string toolName)
    {
        // Check method and class level [Authorize] attributes
        var authorizeAttrs = method.GetCustomAttributes<AuthorizeAttribute>()
            .Concat(method.DeclaringType?.GetCustomAttributes<AuthorizeAttribute>()
                    ?? Enumerable.Empty<AuthorizeAttribute>());

        int? minimumRole = null;

        foreach (var attr in authorizeAttrs)
        {
            if (string.IsNullOrEmpty(attr.Policy)) continue;

            var role = attr.Policy switch
            {
                "RequireMember" => 1,
                "RequireManager" => 2,
                "RequireAdmin" => 3,
                _ => (int?)null
            };

            if (role.HasValue)
            {
                minimumRole = minimumRole.HasValue
                    ? Math.Max(minimumRole.Value, role.Value)
                    : role;
            }
        }

        return new ToolAuthorizationMetadata(toolName, minimumRole);
    }
}

/// <summary>
/// Simple store for tool authorization metadata.
/// </summary>
public interface IToolAuthorizationStore
{
    void Register(string toolName, ToolAuthorizationMetadata metadata);
    int? GetMinimumRole(string toolName);
}

public class ToolAuthorizationStore : IToolAuthorizationStore
{
    private readonly Dictionary<string, ToolAuthorizationMetadata> _store = new();

    public void Register(string toolName, ToolAuthorizationMetadata metadata)
        => _store[toolName] = metadata;

    public int? GetMinimumRole(string toolName)
        => _store.TryGetValue(toolName, out var meta) ? meta.MinimumRole : null;
}
```

### 1.3 Integration

**Modify**: `src/Zero.Mcp.Extensions/McpServerBuilderExtensions.cs`

In `WithToolsFromAssemblyUnwrappingActionResult`, add metadata capture:

```csharp
// At the start of the method, get or create the store
var store = new ToolAuthorizationStore();

// Inside the foreach loop, after getting toolName:
var metadata = ToolAuthorizationMetadata.FromMethod(method, toolName);
store.Register(toolName, metadata);

// Register the store as singleton (return it for later use)
```

### 1.4 Verify

```bash
Use build-agent to build Zero.Mcp.Extensions
Use test-agent to run tests for ToolAuthorizationMetadataTests
```

**Expected**: 5/5 tests pass, CLEAN build

---

## TDDAB Block 2: ListTools Filter

### Goal
Filter tools/list response based on user role using SDK's filter hook.

### 2.1 Tests First

**File**: `tests/Zero.Mcp.Extensions.Tests/ToolFilteringTests.cs`

```csharp
using FluentAssertions;
using System.Security.Claims;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

public class ToolFilteringTests
{
    [Theory]
    [InlineData(0, new[] { "get_by_id", "get_all" })]                                    // Viewer
    [InlineData(1, new[] { "get_by_id", "get_all", "create" })]                          // Member
    [InlineData(2, new[] { "get_by_id", "get_all", "create", "update" })]                // Manager
    [InlineData(3, new[] { "get_by_id", "get_all", "create", "update", "promote" })]     // Admin
    public void Should_FilterTools_ByUserRole(int userRole, string[] expectedTools)
    {
        // Arrange
        var store = CreateTestStore();
        var allTools = new[] { "get_by_id", "get_all", "create", "update", "promote" };

        // Act
        var filtered = ToolListFilter.FilterByRole(allTools, userRole, store);

        // Assert
        filtered.Should().BeEquivalentTo(expectedTools);
    }

    [Fact]
    public void Should_ExtractRole_FromClaimsPrincipal()
    {
        // Arrange
        var claims = new[] { new Claim("role", "Manager") };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var role = ToolListFilter.GetUserRole(principal);

        // Assert
        role.Should().Be(2);
    }

    [Fact]
    public void Should_ReturnNull_WhenNoRoleClaim()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var role = ToolListFilter.GetUserRole(principal);
        role.Should().BeNull();
    }

    private static ToolAuthorizationStore CreateTestStore()
    {
        var store = new ToolAuthorizationStore();
        store.Register("get_by_id", new ToolAuthorizationMetadata("get_by_id", null));
        store.Register("get_all", new ToolAuthorizationMetadata("get_all", null));
        store.Register("create", new ToolAuthorizationMetadata("create", 1));
        store.Register("update", new ToolAuthorizationMetadata("update", 2));
        store.Register("promote", new ToolAuthorizationMetadata("promote", 3));
        return store;
    }
}
```

### 2.2 Implementation

**File**: `src/Zero.Mcp.Extensions/ToolListFilter.cs`

```csharp
using System.Security.Claims;

namespace Zero.Mcp.Extensions;

/// <summary>
/// Filters tool lists based on user authorization.
/// </summary>
public static class ToolListFilter
{
    /// <summary>
    /// Filters tools to only those the user is authorized to use.
    /// </summary>
    public static IEnumerable<string> FilterByRole(
        IEnumerable<string> allTools,
        int? userRole,
        IToolAuthorizationStore store)
    {
        if (userRole == null)
            return Enumerable.Empty<string>();

        return allTools.Where(tool =>
        {
            var minRole = store.GetMinimumRole(tool);
            // null minRole = any authenticated user can access
            return minRole == null || userRole >= minRole;
        });
    }

    /// <summary>
    /// Extracts role value from ClaimsPrincipal.
    /// </summary>
    public static int? GetUserRole(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var roleClaim = user.FindFirst("role")?.Value;
        if (string.IsNullOrEmpty(roleClaim))
            return null;

        return roleClaim switch
        {
            "Viewer" => 0,
            "Member" => 1,
            "Manager" => 2,
            "Admin" => 3,
            _ => null
        };
    }
}
```

### 2.3 Integration

**Modify**: `src/Zero.Mcp.Extensions/McpServerBuilderExtensions.cs`

Add the filter in `AddZeroMcpExtensions`:

```csharp
// After WithToolsFromAssemblyUnwrappingActionResult(options):

if (options.FilterToolsByPermissions)
{
    builder.AddListToolsFilter(next => async (request, cancellationToken) =>
    {
        var result = await next(request, cancellationToken);

        var store = request.Services.GetRequiredService<IToolAuthorizationStore>();
        var userRole = ToolListFilter.GetUserRole(request.User);

        var authorizedTools = ToolListFilter.FilterByRole(
            result.Tools.Select(t => t.Name),
            userRole,
            store);

        return new ListToolsResult
        {
            Tools = result.Tools
                .Where(t => authorizedTools.Contains(t.Name))
                .ToList()
        };
    });
}
```

**Modify**: `src/Zero.Mcp.Extensions/ZeroMcpOptions.cs`

```csharp
/// <summary>
/// Filter tools/list response based on user permissions.
/// Default: true
/// </summary>
public bool FilterToolsByPermissions { get; set; } = true;
```

### 2.4 Verify

```bash
Use build-agent to build Zero.Mcp.Extensions
Use test-agent to run tests for ToolFilteringTests
```

**Expected**: 6/6 tests pass (4 role tests + 2 claim tests), CLEAN build

---

## TDDAB Block 3: Integration Tests

### Goal
End-to-end verification with real HTTP requests.

### 3.1 Tests

**File**: `tests/McpPoc.Api.Tests/ToolVisibilityTests.cs`

```csharp
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace McpPoc.Api.Tests;

[Collection("McpApi")]
public class ToolVisibilityTests
{
    private readonly McpApiFixture _fixture;

    public ToolVisibilityTests(McpApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Viewer_Should_SeeOnly_ReadTools()
    {
        var client = await _fixture.GetAuthenticatedClientAsync("viewer", "viewer123");
        var tools = await GetToolsListAsync(client);

        tools.Should().BeEquivalentTo(new[] { "get_by_id", "get_all" });
    }

    [Fact]
    public async Task Member_Should_See_ReadAndCreateTools()
    {
        var client = await _fixture.GetAuthenticatedClientAsync("alice@example.com", "alice123");
        var tools = await GetToolsListAsync(client);

        tools.Should().BeEquivalentTo(new[] { "get_by_id", "get_all", "create" });
    }

    [Fact]
    public async Task Manager_Should_See_ReadCreateUpdateTools()
    {
        var client = await _fixture.GetAuthenticatedClientAsync("bob@example.com", "bob123");
        var tools = await GetToolsListAsync(client);

        tools.Should().BeEquivalentTo(new[] { "get_by_id", "get_all", "create", "update" });
    }

    [Fact]
    public async Task Admin_Should_See_AllTools()
    {
        var client = await _fixture.GetAuthenticatedClientAsync("carol@example.com", "carol123");
        var tools = await GetToolsListAsync(client);

        tools.Should().BeEquivalentTo(new[] { "get_by_id", "get_all", "create", "update", "promote_to_manager" });
    }

    private static async Task<string[]> GetToolsListAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/mcp", new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/list"
        });

        response.Should().BeSuccessful();

        var result = await response.Content.ReadFromJsonAsync<ToolsListResponse>();
        return result?.Result?.Tools?.Select(t => t.Name).ToArray() ?? Array.Empty<string>();
    }
}

// Simple DTOs
file record ToolsListResponse(ToolsListResult? Result);
file record ToolsListResult(List<ToolInfo>? Tools);
file record ToolInfo(string Name);
```

### 3.2 Verify

```bash
Use build-agent to build McpPoc.Api
Use test-agent to run tests for ToolVisibilityTests
Use test-agent to run all tests
```

**Expected**: 4/4 new tests pass, 54/54 total tests pass

---

## Summary

### What We're Building

| Component | Lines | Purpose |
|-----------|-------|---------|
| `ToolAuthorizationMetadata.cs` | ~50 | Record + FromMethod extractor |
| `ToolListFilter.cs` | ~40 | Static filter + role extraction |
| Modify `McpServerBuilderExtensions.cs` | ~15 | Capture metadata + add filter |
| Modify `ZeroMcpOptions.cs` | ~5 | Add config option |
| **Total new code** | **~110** | Simple, focused |

### Test Count

| Block | New Tests |
|-------|-----------|
| 1 - Metadata | 5 |
| 2 - Filtering | 6 |
| 3 - Integration | 4 |
| **Total new** | **15** |
| **Grand total** | **59** (44 + 15) |

### Why This is Better

1. **Uses SDK's hook** - `AddListToolsFilter()` is designed for this
2. **~110 lines vs ~400** - Much less code to maintain
3. **3 blocks vs 4** - Faster to implement
4. **No decorator pattern** - No fighting the framework
5. **Testable static methods** - Easy unit testing

---

## Execution Order

```
1. Block 1: ToolAuthorizationMetadata + Store (5 tests)
   → build-agent → test-agent

2. Block 2: ToolListFilter + SDK integration (6 tests)
   → build-agent → test-agent

3. Block 3: Integration tests (4 tests)
   → build-agent → test-agent → ALL tests
```

Ready to start with Block 1?
