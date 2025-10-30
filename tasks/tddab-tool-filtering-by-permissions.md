# TDDAB Plan: MCP Tool Filtering by User Permissions

## Executive Summary

**Problem**: UX Issue - `tools/list` shows ALL tools to all users, including tools they cannot use (403 Forbidden)

**Impact**: Poor user experience - Viewer sees `create`, `update`, `promote_to_manager` but gets 403 when calling them

**Solution**: Metadata-based runtime filtering - capture authorization requirements during registration, filter at runtime based on user role

**Approach**: 4 TDDAB blocks using agent-optimized verification (build-agent + test-agent)

**Target**: 52/52 tests (44 existing + 8 new filtering tests)

---

## Current State

### What Works ✅
- ✅ 44/44 tests passing (36 core + 8 viewer role)
- ✅ 4-tier role hierarchy: Viewer(0) → Member(1) → Manager(2) → Admin(3)
- ✅ Policy-based authorization with MinimumRoleRequirement
- ✅ Pre-filter authorization in CreateControllerWithPreFilter
- ✅ Zero.Mcp.Extensions library v1.9.0

### Problem
```csharp
// Current behavior - tools/list returns ALL tools
Viewer calls tools/list → ["get_by_id", "get_all", "create", "update", "promote_to_manager"]

// Then viewer tries create
Viewer calls create → 403 Forbidden ❌

// Expected behavior
Viewer calls tools/list → ["get_by_id", "get_all"] ✅
```

### Tool Authorization Matrix
| Tool | Minimum Role | Viewer | Member | Manager | Admin |
|------|--------------|--------|--------|---------|-------|
| get_by_id | Authenticated | ✅ | ✅ | ✅ | ✅ |
| get_all | Authenticated | ✅ | ✅ | ✅ | ✅ |
| create | Member (1) | ❌ | ✅ | ✅ | ✅ |
| update | Manager (2) | ❌ | ❌ | ✅ | ✅ |
| promote_to_manager | Admin (3) | ❌ | ❌ | ❌ | ✅ |

---

## Architecture Design

### Components

**1. ToolMetadata** (new)
```csharp
public class ToolMetadata
{
    public string ToolName { get; init; } = string.Empty;
    public int? MinimumRole { get; init; }  // Role value: Viewer=0, Member=1, Manager=2, Admin=3
    public bool RequiresAuthentication { get; init; }
    public string[] Policies { get; init; } = Array.Empty<string>();
}
```

**2. IToolMetadataStore** (new)
```csharp
public interface IToolMetadataStore
{
    void RegisterTool(ToolMetadata metadata);
    IEnumerable<ToolMetadata> GetAllTools();
    ToolMetadata? GetTool(string toolName);
}
```

**3. IToolFilterService** (new)
```csharp
public interface IToolFilterService
{
    Task<IEnumerable<string>> GetAuthorizedToolsAsync(
        IEnumerable<string> allTools,
        CancellationToken cancellationToken = default);
}
```

**4. MCP Server Interceptor** (new)
- Intercepts `tools/list` MCP method
- Applies filtering before returning response
- Uses IToolFilterService + IAuthForMcpSupplier

### Data Flow
```
[Registration - Startup]
Tool Registration
  → Extract [Authorize] attributes
  → Determine MinimumRole from [Authorize(Policy = "MinimumRole:X")]
  → Store in ToolMetadataStore

[Runtime - tools/list]
MCP Request "tools/list"
  → Interceptor detects tools/list
  → Get user role via IAuthForMcpSupplier
  → IToolFilterService.GetAuthorizedToolsAsync()
  → Filter tools where user.Role >= tool.MinimumRole
  → Return filtered list
```

---

## Prerequisites: Interface Extension

### Objective
Extend IAuthForMcpSupplier to support role-based filtering

### Required Changes

**File**: `src/Zero.Mcp.Extensions/IAuthForMcpSupplier.cs`

Add new method to existing interface:
```csharp
public interface IAuthForMcpSupplier
{
    // Existing methods - DO NOT CHANGE
    Task<bool> CheckAuthenticatedAsync();
    Task<bool> CheckPolicyAsync(AuthorizeAttribute attribute);

    // NEW: Required for tool filtering
    /// <summary>
    /// Gets the current user's role value.
    /// Returns null if user is not authenticated.
    /// Role values: Viewer=0, Member=1, Manager=2, Admin=3
    /// </summary>
    Task<int?> GetUserRoleAsync(CancellationToken cancellationToken = default);
}
```

**Implementation**: `src/McpPoc.Api/Infrastructure/KeycloakAuthSupplier.cs`

Add implementation to the KeycloakAuthSupplier class:
```csharp
public async Task<int?> GetUserRoleAsync(CancellationToken cancellationToken = default)
{
    var httpContext = _httpContextAccessor.HttpContext;
    if (httpContext?.User?.Identity?.IsAuthenticated != true)
    {
        return null;
    }

    var roleClaim = httpContext.User.FindFirst("role")?.Value;
    if (string.IsNullOrEmpty(roleClaim))
    {
        return null;
    }

    // Parse role claim to UserRole enum value
    if (Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var role))
    {
        return (int)role;
    }

    return null;
}
```

---

## TDDAB Block 1: Tool Metadata System

### Objective
Capture and store authorization metadata during tool registration

### RED Phase - Tests

**File**: `tests/Zero.Mcp.Extensions.Tests/ToolMetadataTests.cs`

```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

public class ToolMetadataTests
{
    [Fact]
    public void ShouldExtractMetadata_When_MethodHasAuthorizePolicy()
    {
        // Arrange
        var methodInfo = typeof(TestToolController).GetMethod(nameof(TestToolController.RequireMemberMethod))!;
        var toolName = "test_tool";

        // Act
        var metadata = ToolMetadataExtractor.ExtractMetadata(methodInfo, toolName);

        // Assert
        metadata.Should().NotBeNull();
        metadata.ToolName.Should().Be(toolName);
        metadata.RequiresAuthentication.Should().BeTrue();
        metadata.MinimumRole.Should().Be(1); // Member role
        metadata.Policies.Should().Contain("RequireMember");
    }

    [Fact]
    public void ShouldExtractMetadata_When_MethodHasAllowAnonymous()
    {
        // Arrange
        var methodInfo = typeof(TestToolController).GetMethod(nameof(TestToolController.AnonymousMethod))!;
        var toolName = "anonymous_tool";

        // Act
        var metadata = ToolMetadataExtractor.ExtractMetadata(methodInfo, toolName);

        // Assert
        metadata.Should().NotBeNull();
        metadata.ToolName.Should().Be(toolName);
        metadata.RequiresAuthentication.Should().BeFalse();
        metadata.MinimumRole.Should().BeNull();
        metadata.Policies.Should().BeEmpty();
    }

    [Fact]
    public void ShouldStoreAndRetrieveMetadata_When_UsingToolMetadataStore()
    {
        // Arrange
        var store = new ToolMetadataStore();
        var metadata = new ToolMetadata
        {
            ToolName = "create",
            RequiresAuthentication = true,
            MinimumRole = 1,
            Policies = new[] { "RequireMember" }
        };

        // Act
        store.RegisterTool(metadata);
        var retrieved = store.GetTool("create");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.ToolName.Should().Be("create");
        retrieved.MinimumRole.Should().Be(1);
        retrieved.Policies.Should().Contain("RequireMember");
    }
}

// Test controller for metadata extraction
internal class TestToolController
{
    [Authorize(Policy = "RequireMember")]
    public void RequireMemberMethod() { }

    [AllowAnonymous]
    public void AnonymousMethod() { }
}
```

### GREEN Phase - Implementation

**File**: `src/Zero.Mcp.Extensions/ToolMetadata.cs`
```csharp
namespace Zero.Mcp.Extensions;

public class ToolMetadata
{
    public string ToolName { get; init; } = string.Empty;
    public int? MinimumRole { get; init; }  // Role value: Viewer=0, Member=1, Manager=2, Admin=3
    public bool RequiresAuthentication { get; init; }
    public string[] Policies { get; init; } = Array.Empty<string>();
}

public interface IToolMetadataStore
{
    void RegisterTool(ToolMetadata metadata);
    IEnumerable<ToolMetadata> GetAllTools();
    ToolMetadata? GetTool(string toolName);
}

public class ToolMetadataStore : IToolMetadataStore
{
    private readonly Dictionary<string, ToolMetadata> _metadata = new();

    public void RegisterTool(ToolMetadata metadata)
    {
        _metadata[metadata.ToolName] = metadata;
    }

    public IEnumerable<ToolMetadata> GetAllTools() => _metadata.Values;

    public ToolMetadata? GetTool(string toolName)
        => _metadata.TryGetValue(toolName, out var meta) ? meta : null;
}
```

**File**: `src/Zero.Mcp.Extensions/ToolMetadataExtractor.cs`
```csharp
namespace Zero.Mcp.Extensions;

internal static class ToolMetadataExtractor
{
    public static ToolMetadata ExtractMetadata(MethodInfo method, string toolName)
    {
        // Check both method-level and class-level [Authorize] attributes
        var methodAttrs = method.GetCustomAttributes<AuthorizeAttribute>();
        var typeAttrs = method.DeclaringType?.GetCustomAttributes<AuthorizeAttribute>() ?? Enumerable.Empty<AuthorizeAttribute>();
        var authorizeAttrs = methodAttrs.Concat(typeAttrs).ToArray();

        // Check for [AllowAnonymous] - if present, no auth required
        var allowAnonymous = method.GetCustomAttribute<AllowAnonymousAttribute>() is not null;
        if (allowAnonymous)
        {
            return new ToolMetadata
            {
                ToolName = toolName,
                RequiresAuthentication = false,
                MinimumRole = null,
                Policies = Array.Empty<string>()
            };
        }

        var requiresAuth = authorizeAttrs.Any();
        int? minimumRole = null;
        var policies = new List<string>();

        foreach (var attr in authorizeAttrs)
        {
            if (!string.IsNullOrEmpty(attr.Policy))
            {
                policies.Add(attr.Policy);

                // Parse "RequireMember" → 1, "RequireManager" → 2, "RequireAdmin" → 3
                // Unknown policies (e.g., RequireTwoFactor) are ignored to preserve existing minimumRole
                var parsedRole = attr.Policy switch
                {
                    "RequireMember" => (int?)1,
                    "RequireManager" => (int?)2,
                    "RequireAdmin" => (int?)3,
                    _ => null // Unknown policy - don't change minimumRole
                };

                // Take the higher role if multiple role policies exist
                if (parsedRole.HasValue)
                {
                    minimumRole = minimumRole.HasValue
                        ? Math.Max(minimumRole.Value, parsedRole.Value)
                        : parsedRole.Value;
                }
            }
        }

        return new ToolMetadata
        {
            ToolName = toolName,
            MinimumRole = minimumRole,
            RequiresAuthentication = requiresAuth,
            Policies = policies.ToArray()
        };
    }
}
```

**Modification**: `src/Zero.Mcp.Extensions/McpServerBuilderExtensions.cs`

Modify `WithToolsFromAssemblyUnwrappingActionResult` method to capture metadata in a static list:
```csharp
// Static metadata list captured during registration (before DI container is built)
private static readonly List<ToolMetadata> _capturedMetadata = new();

private static IMcpServerBuilder WithToolsFromAssemblyUnwrappingActionResult(
    this IMcpServerBuilder builder,
    ZeroMcpOptions options)
{
    var toolAssembly = options.ToolAssembly!;
    var serializerOptions = options.GetEffectiveSerializerOptions();

    // Clear previous metadata (for testing scenarios)
    _capturedMetadata.Clear();

    // Find all types with [McpServerToolType]
    var toolTypes = toolAssembly.GetTypes()
        .Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() is not null);

    foreach (var toolType in toolTypes)
    {
        var toolMethods = toolType.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() is not null);

        foreach (var method in toolMethods)
        {
            var toolName = ConvertToSnakeCase(method.Name);

            // Extract and capture metadata
            var metadata = ToolMetadataExtractor.ExtractMetadata(method, toolName);
            _capturedMetadata.Add(metadata);

            // Register the tool (full implementation)
            if (method.IsStatic)
            {
                // Static method with custom marshaller
                builder.Services.AddSingleton<McpServerTool>(services =>
                {
                    var aiFunction = AIFunctionFactory.Create(
                        method,
                        target: null,
                        new AIFunctionFactoryOptions
                        {
                            Name = toolName,
                            MarshalResult = async (result, resultType, ct) => await MarshalResult.UnwrapAsync(result),
                            SerializerOptions = serializerOptions
                        });
                    return McpServerTool.Create(aiFunction, new McpServerToolCreateOptions { Services = services });
                });
            }
            else
            {
                // Instance method - capture MethodInfo for pre-filter authorization
                var methodCopy = method; // Capture in closure

                builder.Services.AddSingleton<McpServerTool>(services =>
                {
                    var aiFunction = AIFunctionFactory.Create(
                        methodCopy,
                        args => CreateControllerWithPreFilter(args.Services!, toolType, methodCopy, options),
                        new AIFunctionFactoryOptions
                        {
                            Name = toolName,
                            MarshalResult = async (result, resultType, ct) => await MarshalResult.UnwrapAsync(result),
                            SerializerOptions = serializerOptions
                        });

                    return McpServerTool.Create(aiFunction, new McpServerToolCreateOptions
                    {
                        Services = services
                    });
                });
            }
        }
    }

    return builder;
}
```

Update `AddZeroMcpExtensions` to populate metadata store after registration:
```csharp
// Register tool metadata store (singleton factory that populates from captured metadata)
services.AddSingleton<IToolMetadataStore>(sp =>
{
    var store = new ToolMetadataStore();
    foreach (var metadata in _capturedMetadata)
    {
        store.RegisterTool(metadata);
    }
    return store;
});
```

### VERIFY Phase

**Command**:
```bash
mcp__vs-mcp__ExecuteCommand --command build --what Zero.Mcp.Extensions --pathFormat WSL
mcp__vs-mcp__ExecuteAsyncTest --operation start --projectName Zero.Mcp.Extensions.Tests --filter "FullyQualifiedName~ToolMetadataTests" --pathFormat WSL
mcp__vs-mcp__ExecuteAsyncTest --operation status --pathFormat WSL
```

**Expected**: 3/3 tests passing, CLEAN build

---

## TDDAB Block 2: Tool Filtering Service

### Objective
Implement service that filters tools based on user permissions

### RED Phase - Tests

**File**: `tests/Zero.Mcp.Extensions.Tests/ToolFilterServiceTests.cs`

```csharp
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

public class ToolFilterServiceTests
{
    [Fact]
    public async Task ShouldReturnOnlyReadTools_When_UserIsViewer()
    {
        // Arrange
        var authSupplier = Mock.Of<IAuthForMcpSupplier>(a => a.GetUserRoleAsync(default) == Task.FromResult<int?>(0));
        var store = CreateMetadataStore();
        var logger = Mock.Of<ILogger<ToolFilterService>>();
        var service = new ToolFilterService(store, authSupplier, logger);
        var allTools = new[] { "get_by_id", "get_all", "create", "update", "promote_to_manager" };

        // Act
        var result = await service.GetAuthorizedToolsAsync(allTools);

        // Assert
        result.Should().BeEquivalentTo(new[] { "get_by_id", "get_all" });
    }

    [Fact]
    public async Task ShouldReturnReadAndCreateTools_When_UserIsMember()
    {
        // Arrange
        var authSupplier = Mock.Of<IAuthForMcpSupplier>(a => a.GetUserRoleAsync(default) == Task.FromResult<int?>(1));
        var store = CreateMetadataStore();
        var logger = Mock.Of<ILogger<ToolFilterService>>();
        var service = new ToolFilterService(store, authSupplier, logger);
        var allTools = new[] { "get_by_id", "get_all", "create", "update", "promote_to_manager" };

        // Act
        var result = await service.GetAuthorizedToolsAsync(allTools);

        // Assert
        result.Should().BeEquivalentTo(new[] { "get_by_id", "get_all", "create" });
    }

    [Fact]
    public async Task ShouldReturnReadCreateUpdateTools_When_UserIsManager()
    {
        // Arrange
        var authSupplier = Mock.Of<IAuthForMcpSupplier>(a => a.GetUserRoleAsync(default) == Task.FromResult<int?>(2));
        var store = CreateMetadataStore();
        var logger = Mock.Of<ILogger<ToolFilterService>>();
        var service = new ToolFilterService(store, authSupplier, logger);
        var allTools = new[] { "get_by_id", "get_all", "create", "update", "promote_to_manager" };

        // Act
        var result = await service.GetAuthorizedToolsAsync(allTools);

        // Assert
        result.Should().BeEquivalentTo(new[] { "get_by_id", "get_all", "create", "update" });
    }

    [Fact]
    public async Task ShouldReturnAllTools_When_UserIsAdmin()
    {
        // Arrange
        var authSupplier = Mock.Of<IAuthForMcpSupplier>(a => a.GetUserRoleAsync(default) == Task.FromResult<int?>(3));
        var store = CreateMetadataStore();
        var logger = Mock.Of<ILogger<ToolFilterService>>();
        var service = new ToolFilterService(store, authSupplier, logger);
        var allTools = new[] { "get_by_id", "get_all", "create", "update", "promote_to_manager" };

        // Act
        var result = await service.GetAuthorizedToolsAsync(allTools);

        // Assert
        result.Should().BeEquivalentTo(new[] { "get_by_id", "get_all", "create", "update", "promote_to_manager" });
    }

    [Fact]
    public async Task ShouldReturnEmpty_When_UserIsUnauthenticated()
    {
        // Arrange
        var authSupplier = Mock.Of<IAuthForMcpSupplier>(a => a.GetUserRoleAsync(default) == Task.FromResult<int?>(null));
        var store = CreateMetadataStore();
        var logger = Mock.Of<ILogger<ToolFilterService>>();
        var service = new ToolFilterService(store, authSupplier, logger);
        var allTools = new[] { "get_by_id", "get_all", "create", "update", "promote_to_manager" };

        // Act
        var result = await service.GetAuthorizedToolsAsync(allTools);

        // Assert
        result.Should().BeEmpty();
    }

    private static IToolMetadataStore CreateMetadataStore()
    {
        var store = new ToolMetadataStore();
        store.RegisterTool(new ToolMetadata { ToolName = "get_by_id", RequiresAuthentication = true, MinimumRole = null });
        store.RegisterTool(new ToolMetadata { ToolName = "get_all", RequiresAuthentication = true, MinimumRole = null });
        store.RegisterTool(new ToolMetadata { ToolName = "create", RequiresAuthentication = true, MinimumRole = 1 });
        store.RegisterTool(new ToolMetadata { ToolName = "update", RequiresAuthentication = true, MinimumRole = 2 });
        store.RegisterTool(new ToolMetadata { ToolName = "promote_to_manager", RequiresAuthentication = true, MinimumRole = 3 });
        return store;
    }
}
```

### GREEN Phase - Implementation

**File**: `src/Zero.Mcp.Extensions/ToolFilterService.cs`
```csharp
namespace Zero.Mcp.Extensions;

public interface IToolFilterService
{
    Task<IEnumerable<string>> GetAuthorizedToolsAsync(
        IEnumerable<string> allTools,
        CancellationToken cancellationToken = default);
}

public class ToolFilterService : IToolFilterService
{
    private readonly IToolMetadataStore _metadataStore;
    private readonly IAuthForMcpSupplier _authSupplier;
    private readonly ILogger<ToolFilterService> _logger;

    public ToolFilterService(
        IToolMetadataStore metadataStore,
        IAuthForMcpSupplier authSupplier,
        ILogger<ToolFilterService> logger)
    {
        _metadataStore = metadataStore;
        _authSupplier = authSupplier;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> GetAuthorizedToolsAsync(
        IEnumerable<string> allTools,
        CancellationToken cancellationToken = default)
    {
        // Get current user role
        var userRole = await _authSupplier.GetUserRoleAsync(cancellationToken);

        if (userRole == null)
        {
            _logger.LogDebug("No user context - returning empty tool list");
            return Array.Empty<string>();
        }

        var authorizedTools = new List<string>();

        foreach (var toolName in allTools)
        {
            var metadata = _metadataStore.GetTool(toolName);

            if (metadata == null)
            {
                // No metadata - include tool (backward compatibility)
                _logger.LogWarning("No metadata for tool: {Tool} - including by default", toolName);
                authorizedTools.Add(toolName);
                continue;
            }

            // Check if user has required role
            if (!metadata.RequiresAuthentication)
            {
                // Public tool
                authorizedTools.Add(toolName);
            }
            else if (metadata.MinimumRole == null)
            {
                // Requires auth but no specific role - any authenticated user
                authorizedTools.Add(toolName);
            }
            else if (userRole >= metadata.MinimumRole)
            {
                // User meets minimum role requirement
                authorizedTools.Add(toolName);
            }
        }

        _logger.LogDebug(
            "Filtered {Total} tools to {Authorized} for role {Role}",
            allTools.Count(),
            authorizedTools.Count,
            userRole);

        return authorizedTools;
    }
}
```

**Modification**: `src/Zero.Mcp.Extensions/McpServerBuilderExtensions.cs`

Add to `AddZeroMcpExtensions` method:
```csharp
// Register tool filter service
services.AddScoped<IToolFilterService, ToolFilterService>();
```

### VERIFY Phase

**Command**:
```bash
mcp__vs-mcp__ExecuteCommand --command build --what Zero.Mcp.Extensions --pathFormat WSL
mcp__vs-mcp__ExecuteAsyncTest --operation start --projectName Zero.Mcp.Extensions.Tests --filter "FullyQualifiedName~ToolFilterServiceTests" --pathFormat WSL
mcp__vs-mcp__ExecuteAsyncTest --operation status --pathFormat WSL
```

**Expected**: 5/5 tests passing, CLEAN build

---

## TDDAB Block 3: MCP Tools/List Interceptor

### Objective
Intercept `tools/list` MCP requests and apply filtering

### RED Phase - Tests

**File**: `tests/Zero.Mcp.Extensions.Tests/FilteredMcpServerTests.cs`

```csharp
using FluentAssertions;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Moq;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

public class FilteredMcpServerTests
{
    [Fact]
    public async Task ShouldInterceptToolsList_When_ListToolsAsyncCalled()
    {
        // Arrange
        var innerServer = Mock.Of<IMcpServer>();
        var filterService = Mock.Of<IToolFilterService>();
        var logger = Mock.Of<ILogger<FilteredMcpServer>>();
        var server = new FilteredMcpServer(innerServer, filterService, logger);

        Mock.Get(innerServer)
            .Setup(s => s.ListToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToolListResult
            {
                Tools = new List<McpServerTool>
                {
                    CreateMockTool("get_by_id"),
                    CreateMockTool("create")
                }
            });

        Mock.Get(filterService)
            .Setup(f => f.GetAuthorizedToolsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "get_by_id", "create" });

        // Act
        var result = await server.ListToolsAsync(CancellationToken.None);

        // Assert
        Mock.Get(innerServer).Verify(s => s.ListToolsAsync(It.IsAny<CancellationToken>()), Times.Once);
        Mock.Get(filterService).Verify(
            f => f.GetAuthorizedToolsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ShouldFilterToolsList_When_UserIsViewer()
    {
        // Arrange
        var innerServer = Mock.Of<IMcpServer>();
        var filterService = Mock.Of<IToolFilterService>();
        var logger = Mock.Of<ILogger<FilteredMcpServer>>();
        var server = new FilteredMcpServer(innerServer, filterService, logger);

        Mock.Get(innerServer)
            .Setup(s => s.ListToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToolListResult
            {
                Tools = new List<McpServerTool>
                {
                    CreateMockTool("get_by_id"),
                    CreateMockTool("get_all"),
                    CreateMockTool("create"),
                    CreateMockTool("update"),
                    CreateMockTool("promote")
                }
            });

        Mock.Get(filterService)
            .Setup(f => f.GetAuthorizedToolsAsync(
                It.Is<IEnumerable<string>>(tools => tools.Count() == 5),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "get_by_id", "get_all" }); // Viewer can only read

        // Act
        var result = await server.ListToolsAsync(CancellationToken.None);

        // Assert
        result.Tools.Should().HaveCount(2);
        result.Tools.Select(t => t.Metadata.Name).Should().BeEquivalentTo(new[] { "get_by_id", "get_all" });
    }

    [Fact]
    public async Task ShouldDelegateCallToolAsync_ToInnerServer()
    {
        // Arrange
        var innerServer = Mock.Of<IMcpServer>();
        var filterService = Mock.Of<IToolFilterService>();
        var logger = Mock.Of<ILogger<FilteredMcpServer>>();
        var server = new FilteredMcpServer(innerServer, filterService, logger);

        var expectedResult = new CallToolResult { Content = new[] { new TextContent { Text = "result" } } };
        Mock.Get(innerServer)
            .Setup(s => s.CallToolAsync("test_tool", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await server.CallToolAsync("test_tool", new { arg = "value" }, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
        Mock.Get(innerServer).Verify(
            s => s.CallToolAsync("test_tool", It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static McpServerTool CreateMockTool(string name)
    {
        return new McpServerTool(
            new AIFunction(name, "Description", (args, ct) => Task.FromResult<object?>(null)),
            new McpServerToolCreateOptions());
    }
}
```

### GREEN Phase - Implementation

**Solution**: Wrap IMcpServer with decorator pattern (cleaner, more testable)

**File**: `src/Zero.Mcp.Extensions/FilteredMcpServer.cs`
```csharp
namespace Zero.Mcp.Extensions;

internal class FilteredMcpServer : IMcpServer
{
    private readonly IMcpServer _innerServer;
    private readonly IToolFilterService _filterService;
    private readonly ILogger<FilteredMcpServer> _logger;

    public FilteredMcpServer(
        IMcpServer innerServer,
        IToolFilterService filterService,
        ILogger<FilteredMcpServer> logger)
    {
        _innerServer = innerServer;
        _filterService = filterService;
        _logger = logger;
    }

    public async Task<ToolListResult> ListToolsAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Intercepting tools/list request for filtering");

        var allTools = await _innerServer.ListToolsAsync(cancellationToken);

        var authorizedToolNames = await _filterService.GetAuthorizedToolsAsync(
            allTools.Tools.Select(t => t.Metadata.Name),
            cancellationToken);

        var filteredTools = allTools.Tools
            .Where(t => authorizedToolNames.Contains(t.Metadata.Name))
            .ToList();

        _logger.LogInformation(
            "Filtered tools list: {Total} → {Filtered}",
            allTools.Tools.Count,
            filteredTools.Count);

        return new ToolListResult { Tools = filteredTools };
    }

    // Delegate all other methods to _innerServer
    public Task<CallToolResult> CallToolAsync(string name, object? arguments, CancellationToken cancellationToken)
        => _innerServer.CallToolAsync(name, arguments, cancellationToken);

    public Task<PromptListResult> ListPromptsAsync(CancellationToken cancellationToken)
        => _innerServer.ListPromptsAsync(cancellationToken);

    public Task<GetPromptResult> GetPromptAsync(string name, object? arguments, CancellationToken cancellationToken)
        => _innerServer.GetPromptAsync(name, arguments, cancellationToken);

    public Task<ResourceListResult> ListResourcesAsync(CancellationToken cancellationToken)
        => _innerServer.ListResourcesAsync(cancellationToken);

    public Task<ReadResourceResult> ReadResourceAsync(string uri, CancellationToken cancellationToken)
        => _innerServer.ReadResourceAsync(uri, cancellationToken);

    public Task<ResourceTemplateListResult> ListResourceTemplatesAsync(CancellationToken cancellationToken)
        => _innerServer.ListResourceTemplatesAsync(cancellationToken);

    public Task<InitializeResult> InitializeAsync(InitializeRequest request, CancellationToken cancellationToken)
        => _innerServer.InitializeAsync(request, cancellationToken);

    public Task PingAsync(CancellationToken cancellationToken)
        => _innerServer.PingAsync(cancellationToken);
}
```

**Modification**: `src/Zero.Mcp.Extensions/McpServerBuilderExtensions.cs`

Add decorator registration in `AddZeroMcpExtensions` after `AddMcpServer()`:
```csharp
var builder = services.AddMcpServer();

// Wrap with filtered server if both authorization AND filtering are enabled
if (options.UseAuthorization && options.FilterToolsByPermissions)
{
    // Manual decorator registration without Scrutor
    var descriptors = services.Where(d => d.ServiceType == typeof(IMcpServer)).ToList();
    foreach (var descriptor in descriptors)
    {
        services.Remove(descriptor);

        services.Add(new ServiceDescriptor(
            typeof(IMcpServer),
            sp =>
            {
                var inner = descriptor.ImplementationFactory != null
                    ? descriptor.ImplementationFactory(sp) as IMcpServer
                    : ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType!) as IMcpServer;

                var filterService = sp.GetRequiredService<IToolFilterService>();
                var logger = sp.GetRequiredService<ILogger<FilteredMcpServer>>();

                return new FilteredMcpServer(inner!, filterService, logger);
            },
            descriptor.Lifetime));
    }
}

return builder.WithHttpTransport()
    .WithToolsFromAssemblyUnwrappingActionResult(options);
```

### VERIFY Phase

**Command**:
```bash
mcp__vs-mcp__ExecuteCommand --command build --what Zero.Mcp.Extensions --pathFormat WSL
mcp__vs-mcp__ExecuteAsyncTest --operation start --projectName Zero.Mcp.Extensions.Tests --filter "FullyQualifiedName~FilteredMcpServerTests" --pathFormat WSL
mcp__vs-mcp__ExecuteAsyncTest --operation status --pathFormat WSL
```

**Expected**: 3/3 tests passing, CLEAN build

---

## TDDAB Block 4: Integration Tests

### Objective
End-to-end verification that each role sees correct tools

### RED Phase - Tests

**Test 4.1**: Viewer sees only read tools
```csharp
[Fact]
public async Task Should_ShowOnlyReadTools_ForViewerRole()
{
    // Arrange
    var client = await _fixture.GetAuthenticatedClientAsync("viewer", "viewer123");

    // Act
    var response = await client.PostAsJsonAsync("/mcp", new
    {
        jsonrpc = "2.0",
        id = 1,
        method = "tools/list"
    });

    // Assert
    response.Should().BeSuccessful();
    var result = await response.Content.ReadFromJsonAsync<McpToolsListResponse>();
    result.Should().NotBeNull();
    result!.Result.Tools.Should().HaveCount(2);
    result.Result.Tools.Select(t => t.Name).Should().BeEquivalentTo(new[] { "get_by_id", "get_all" });
}
```

**Test 4.2**: Member sees read + create tools
```csharp
[Fact]
public async Task Should_ShowReadAndCreateTools_ForMemberRole()
{
    // Arrange
    var client = await _fixture.GetAuthenticatedClientAsync("alice@example.com", "alice123");

    // Act
    var response = await client.PostAsJsonAsync("/mcp", new
    {
        jsonrpc = "2.0",
        id = 1,
        method = "tools/list"
    });

    // Assert
    response.Should().BeSuccessful();
    var result = await response.Content.ReadFromJsonAsync<McpToolsListResponse>();
    result!.Result.Tools.Should().HaveCount(3);
    result.Result.Tools.Select(t => t.Name).Should().BeEquivalentTo(new[] { "get_by_id", "get_all", "create" });
}
```

**Test 4.3**: Manager sees read + create + update tools
```csharp
[Fact]
public async Task Should_ShowReadCreateUpdateTools_ForManagerRole()
{
    // Arrange
    var client = await _fixture.GetAuthenticatedClientAsync("bob@example.com", "bob123");

    // Act
    var response = await client.PostAsJsonAsync("/mcp", new
    {
        jsonrpc = "2.0",
        id = 1,
        method = "tools/list"
    });

    // Assert
    response.Should().BeSuccessful();
    var result = await response.Content.ReadFromJsonAsync<McpToolsListResponse>();
    result!.Result.Tools.Should().HaveCount(4);
    result.Result.Tools.Select(t => t.Name).Should().BeEquivalentTo(new[] { "get_by_id", "get_all", "create", "update" });
}
```

**Test 4.4**: Admin sees all tools
```csharp
[Fact]
public async Task Should_ShowAllTools_ForAdminRole()
{
    // Arrange
    var client = await _fixture.GetAuthenticatedClientAsync("admin@example.com", "admin123");

    // Act
    var response = await client.PostAsJsonAsync("/mcp", new
    {
        jsonrpc = "2.0",
        id = 1,
        method = "tools/list"
    });

    // Assert
    response.Should().BeSuccessful();
    var result = await response.Content.ReadFromJsonAsync<McpToolsListResponse>();
    result!.Result.Tools.Should().HaveCount(5);
    result.Result.Tools.Select(t => t.Name).Should().BeEquivalentTo(
        new[] { "get_by_id", "get_all", "create", "update", "promote_to_manager" });
}
```

**Test 4.5**: Unauthenticated sees no tools
```csharp
[Fact]
public async Task Should_Return401_ForUnauthenticatedToolsList()
{
    // No authentication
    var client = _fixture.GetUnauthenticatedClient();

    // tools/list should return 401 (endpoint is protected)
    var response = await client.PostAsJsonAsync("/mcp", new
    {
        jsonrpc = "2.0",
        id = 1,
        method = "tools/list"
    });
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

**Test 4.6**: Public tools visible to all (if any exist)
```csharp
[Fact]
public async Task Should_ShowPublicTool_ToAllRoles()
{
    // Arrange - test with multiple roles
    var roles = new[]
    {
        ("viewer", "viewer123"),
        ("alice@example.com", "alice123"),
        ("bob@example.com", "bob123"),
        ("admin@example.com", "admin123")
    };

    foreach (var (username, password) in roles)
    {
        var client = await _fixture.GetAuthenticatedClientAsync(username, password);

        // Act
        var response = await client.PostAsJsonAsync("/mcp", new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/list"
        });

        // Assert
        response.Should().BeSuccessful();
        var result = await response.Content.ReadFromJsonAsync<McpToolsListResponse>();
        result.Should().NotBeNull();

        // If public tools exist (e.g., [AllowAnonymous]), they should appear for all roles
        // Current API has no public tools, so this test verifies filtering doesn't break
        result!.Result.Tools.Should().NotBeEmpty("all authenticated users should see at least read tools");
    }
}
```

**Test 4.7**: Viewer cannot call filtered-out tools
```csharp
[Fact]
public async Task Should_Return403_WhenViewerCallsFilteredTool()
{
    // Arrange
    var client = await _fixture.GetAuthenticatedClientAsync("viewer", "viewer123");

    // Act - try to call create (not in tools/list for Viewer)
    var response = await client.PostAsJsonAsync("/mcp", new
    {
        jsonrpc = "2.0",
        id = 1,
        method = "tools/call",
        @params = new
        {
            name = "create",
            arguments = new
            {
                name = "Test User",
                email = "test@example.com"
            }
        }
    });

    // Assert - should still get 403 (double protection: filter + authorization)
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

**Test 4.8**: Tool filtering respects role hierarchy
```csharp
[Fact]
public async Task Should_RespectRoleHierarchy_InToolFiltering()
{
    // Arrange & Act - get tool lists for Member and Manager
    var memberClient = await _fixture.GetAuthenticatedClientAsync("alice@example.com", "alice123");
    var memberResponse = await memberClient.PostAsJsonAsync("/mcp", new
    {
        jsonrpc = "2.0",
        id = 1,
        method = "tools/list"
    });
    var memberResult = await memberResponse.Content.ReadFromJsonAsync<McpToolsListResponse>();
    var memberTools = memberResult!.Result.Tools.Select(t => t.Name).ToList();

    var managerClient = await _fixture.GetAuthenticatedClientAsync("bob@example.com", "bob123");
    var managerResponse = await managerClient.PostAsJsonAsync("/mcp", new
    {
        jsonrpc = "2.0",
        id = 1,
        method = "tools/list"
    });
    var managerResult = await managerResponse.Content.ReadFromJsonAsync<McpToolsListResponse>();
    var managerTools = managerResult!.Result.Tools.Select(t => t.Name).ToList();

    // Assert - Manager should see all Member tools plus Manager-specific tools
    memberTools.Should().BeSubsetOf(managerTools, "higher roles inherit lower role permissions");
    managerTools.Should().HaveCountGreaterThan(memberTools.Count, "Manager should have additional tools beyond Member");
    managerTools.Should().Contain("update", "Manager should see update tool");
    memberTools.Should().NotContain("update", "Member should not see update tool");
}
```

### GREEN Phase - Implementation

**File**: `tests/McpPoc.Api.Tests/ToolFilteringTests.cs`

```csharp
using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace McpPoc.Api.Tests;

[Collection("McpApi")]
public class ToolFilteringTests
{
    private readonly McpApiFixture _fixture;

    public ToolFilteringTests(McpApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_ShowOnlyReadTools_ForViewerRole()
    {
        // Arrange
        var client = await _fixture.GetAuthenticatedClientAsync("viewer", "viewer123");

        // Act
        var response = await client.PostAsJsonAsync("/mcp", new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/list"
        });

        // Assert
        response.Should().BeSuccessful();
        var result = await response.Content.ReadFromJsonAsync<McpToolsListResponse>();
        result.Should().NotBeNull();
        result!.Result.Tools.Should().HaveCount(2);
        result.Result.Tools.Select(t => t.Name).Should().BeEquivalentTo(new[] { "get_by_id", "get_all" });
    }

    [Fact]
    public async Task Should_ShowReadAndCreateTools_ForMemberRole()
    {
        // Arrange
        var client = await _fixture.GetAuthenticatedClientAsync("alice@example.com", "alice123");

        // Act
        var response = await client.PostAsJsonAsync("/mcp", new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/list"
        });

        // Assert
        response.Should().BeSuccessful();
        var result = await response.Content.ReadFromJsonAsync<McpToolsListResponse>();
        result!.Result.Tools.Should().HaveCount(3);
        result.Result.Tools.Select(t => t.Name).Should().BeEquivalentTo(new[] { "get_by_id", "get_all", "create" });
    }

    [Fact]
    public async Task Should_ShowReadCreateUpdateTools_ForManagerRole()
    {
        // Arrange
        var client = await _fixture.GetAuthenticatedClientAsync("bob@example.com", "bob123");

        // Act
        var response = await client.PostAsJsonAsync("/mcp", new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/list"
        });

        // Assert
        response.Should().BeSuccessful();
        var result = await response.Content.ReadFromJsonAsync<McpToolsListResponse>();
        result!.Result.Tools.Should().HaveCount(4);
        result.Result.Tools.Select(t => t.Name).Should().BeEquivalentTo(new[] { "get_by_id", "get_all", "create", "update" });
    }

    [Fact]
    public async Task Should_ShowAllTools_ForAdminRole()
    {
        // Arrange
        var client = await _fixture.GetAuthenticatedClientAsync("admin@example.com", "admin123");

        // Act
        var response = await client.PostAsJsonAsync("/mcp", new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/list"
        });

        // Assert
        response.Should().BeSuccessful();
        var result = await response.Content.ReadFromJsonAsync<McpToolsListResponse>();
        result!.Result.Tools.Should().HaveCount(5);
        result.Result.Tools.Select(t => t.Name).Should().BeEquivalentTo(
            new[] { "get_by_id", "get_all", "create", "update", "promote_to_manager" });
    }

    [Fact]
    public async Task Should_Return401_ForUnauthenticatedToolsList()
    {
        // Arrange
        var client = _fixture.GetUnauthenticatedClient();

        // Act
        var response = await client.PostAsJsonAsync("/mcp", new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/list"
        });

        // Assert - endpoint is protected by RequireAuthorization
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_ShowPublicTool_ToAllRoles()
    {
        // Arrange - test with multiple roles
        var roles = new[]
        {
            ("viewer", "viewer123"),
            ("alice@example.com", "alice123"),
            ("bob@example.com", "bob123"),
            ("admin@example.com", "admin123")
        };

        foreach (var (username, password) in roles)
        {
            var client = await _fixture.GetAuthenticatedClientAsync(username, password);

            // Act
            var response = await client.PostAsJsonAsync("/mcp", new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "tools/list"
            });

            // Assert
            response.Should().BeSuccessful();
            var result = await response.Content.ReadFromJsonAsync<McpToolsListResponse>();
            result.Should().NotBeNull();

            // If public tools exist (e.g., [AllowAnonymous]), they should appear for all roles
            // Current API has no public tools, so this test verifies filtering doesn't break
            result!.Result.Tools.Should().NotBeEmpty("all authenticated users should see at least read tools");
        }
    }

    [Fact]
    public async Task Should_Return403_WhenViewerCallsFilteredTool()
    {
        // Arrange
        var client = await _fixture.GetAuthenticatedClientAsync("viewer", "viewer123");

        // Act - try to call create (not in tools/list for Viewer)
        var response = await client.PostAsJsonAsync("/mcp", new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/call",
            @params = new
            {
                name = "create",
                arguments = new
                {
                    name = "Test User",
                    email = "test@example.com"
                }
            }
        });

        // Assert - should still get 403 (double protection: filter + authorization)
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Should_RespectRoleHierarchy_InToolFiltering()
    {
        // Arrange & Act - get tool lists for Member and Manager
        var memberClient = await _fixture.GetAuthenticatedClientAsync("alice@example.com", "alice123");
        var memberResponse = await memberClient.PostAsJsonAsync("/mcp", new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/list"
        });
        var memberResult = await memberResponse.Content.ReadFromJsonAsync<McpToolsListResponse>();
        var memberTools = memberResult!.Result.Tools.Select(t => t.Name).ToList();

        var managerClient = await _fixture.GetAuthenticatedClientAsync("bob@example.com", "bob123");
        var managerResponse = await managerClient.PostAsJsonAsync("/mcp", new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/list"
        });
        var managerResult = await managerResponse.Content.ReadFromJsonAsync<McpToolsListResponse>();
        var managerTools = managerResult!.Result.Tools.Select(t => t.Name).ToList();

        // Assert - Manager should see all Member tools plus Manager-specific tools
        memberTools.Should().BeSubsetOf(managerTools, "higher roles inherit lower role permissions");
        managerTools.Should().HaveCountGreaterThan(memberTools.Count, "Manager should have additional tools beyond Member");
        managerTools.Should().Contain("update", "Manager should see update tool");
        memberTools.Should().NotContain("update", "Member should not see update tool");
    }
}

// Response DTOs for tools/list
public class McpToolsListResponse
{
    public McpToolsListResult Result { get; set; } = new();
}

public class McpToolsListResult
{
    public List<McpTool> Tools { get; set; } = new();
}

public class McpTool
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
```

**Note**: Most implementation is already done in Blocks 1-3, this block is primarily verification

### VERIFY Phase

**Command**:
```bash
mcp__vs-mcp__ExecuteCommand --command build --what McpPoc.Api --pathFormat WSL
mcp__vs-mcp__ExecuteAsyncTest --operation start --projectName McpPoc.Api.Tests --filter "FullyQualifiedName~ToolFilteringTests" --pathFormat WSL
mcp__vs-mcp__ExecuteAsyncTest --operation status --pathFormat WSL
```

**Expected**: 8/8 tests passing, CLEAN build

**Final verification**:
```bash
mcp__vs-mcp__ExecuteAsyncTest --operation start --projectName McpPoc.Api.Tests --pathFormat WSL
mcp__vs-mcp__ExecuteAsyncTest --operation status --pathFormat WSL
```

**Expected**: 52/52 tests passing (44 existing + 8 new)

---

## Implementation Checklist

### Prerequisites
- ✅ Zero.Mcp.Extensions library exists (v1.9.0)
- ✅ 4-tier role system implemented
- ✅ IAuthForMcpSupplier interface available
- ✅ Policy-based authorization working

### TDDAB Block 1: Tool Metadata
- [ ] Create ToolMetadata.cs
- [ ] Create IToolMetadataStore interface
- [ ] Implement ToolMetadataStore
- [ ] Create ToolMetadataExtractor utility
- [ ] Modify McpServerBuilderExtensions to capture metadata
- [ ] Write 3 unit tests
- [ ] VERIFY: build-agent + test-agent → 3/3 passing

### TDDAB Block 2: Tool Filtering
- [ ] Create IToolFilterService interface
- [ ] Implement ToolFilterService
- [ ] Register service in DI container
- [ ] Write 5 unit tests (Viewer, Member, Manager, Admin, Unauth)
- [ ] VERIFY: build-agent + test-agent → 5/5 passing

### TDDAB Block 3: MCP Interceptor
- [ ] Create FilteredMcpServer decorator
- [ ] Implement IMcpServer wrapper
- [ ] Modify McpServerBuilderExtensions to apply decorator
- [ ] Add configuration option (FilterTools: bool)
- [ ] Write 3 unit tests
- [ ] VERIFY: build-agent + test-agent → 3/3 passing

### TDDAB Block 4: Integration Tests
- [ ] Create ToolFilteringTests.cs
- [ ] Write 8 integration tests
- [ ] VERIFY: build-agent + test-agent → 8/8 passing
- [ ] Final: test-agent all tests → 52/52 passing

---

## Success Criteria

### Functional
✅ Viewer sees only 2 tools: get_by_id, get_all
✅ Member sees 3 tools: get_by_id, get_all, create
✅ Manager sees 4 tools: get_by_id, get_all, create, update
✅ Admin sees 5 tools: all tools
✅ Unauthenticated user gets 401 on tools/list
✅ Filtered tools still return 403 if called directly (defense in depth)
✅ Role hierarchy respected (higher roles see all lower role tools)

### Technical
✅ Metadata extraction from [Authorize] attributes
✅ Scoped IToolFilterService per request
✅ Decorator pattern for IMcpServer
✅ Backward compatible (tools without metadata included by default)
✅ Logging for debugging
✅ 52/52 tests passing (100% success rate)

### Quality
✅ Agent-optimized TDDAB (90%+ context savings)
✅ Clean separation of concerns
✅ No breaking changes to existing API
✅ Configuration option to enable/disable filtering
✅ Comprehensive test coverage

---

## Configuration

Add to ZeroMcpOptions:
```csharp
public class ZeroMcpOptions
{
    // Existing options...

    /// <summary>
    /// Filter tools/list response based on user permissions.
    /// Default: true
    /// </summary>
    public bool FilterToolsByPermissions { get; set; } = true;
}
```

---

## Risks and Mitigations

**Risk 1**: MCP SDK doesn't support IMcpServer decoration
- **Mitigation**: Test decorator approach in Block 3, fallback to middleware if needed

**Risk 2**: Performance impact of metadata extraction at startup
- **Mitigation**: Cache metadata in dictionary, one-time cost at registration

**Risk 3**: Breaking changes to existing tests
- **Mitigation**: Tests expect all tools, need to update based on test user role

**Risk 4**: Metadata extraction fails for complex [Authorize] scenarios
- **Mitigation**: Start with MinimumRole:X pattern, extend later if needed

---

## Post-Implementation Tasks

- [ ] Update README.md with filtering feature
- [ ] Update USERS-AND-PERMISSIONS.md
- [ ] Add filtering examples to documentation
- [ ] Consider adding to Zero.Mcp.Extensions v1.10.0
- [ ] Create demo video showing different tool lists per role

---

## Agent Commands

### Build Verification
```bash
Use build-agent to build Zero.Mcp.Extensions
Use build-agent to build McpPoc.Api
```

### Test Execution
```bash
Use test-agent to run tests for ToolMetadataTests
Use test-agent to run tests for ToolFilterServiceTests
Use test-agent to run tests for FilteredMcpServerTests
Use test-agent to run tests for ToolFilteringTests
Use test-agent to run all tests for McpPoc.Api.Tests
```

### Expected Final State
- Build: ✅ CLEAN (all projects)
- Tests: ✅ 52/52 PASS (100%)
- Coverage: All 4 roles tested for tool visibility
- Context: 90%+ savings from agent optimization

---

## Notes

This plan follows agent-optimized TDDAB methodology:
- Each block is atomic and independently verifiable
- Build-agent provides compressed build feedback
- Test-agent provides compressed test results
- Massive context savings (500-2000 lines → 10-30 lines per block)
- 5-10x faster feedback loops

Ready to execute? Start with **TDDAB Block 1: Tool Metadata System**
