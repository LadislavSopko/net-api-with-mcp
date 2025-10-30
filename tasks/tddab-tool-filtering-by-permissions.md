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
    public UserRole? MinimumRole { get; init; }
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

## TDDAB Block 1: Tool Metadata System

### Objective
Capture and store authorization metadata during tool registration

### RED Phase - Tests

**Test 1.1**: Extract minimum role from policy attribute
```csharp
[Fact]
public void Should_ExtractMinimumRole_FromAuthorizePolicyAttribute()
{
    // Test method with [Authorize(Policy = "MinimumRole:Member")]
    // Expected: MinimumRole = UserRole.Member
}
```

**Test 1.2**: Detect authentication requirement
```csharp
[Fact]
public void Should_DetectAuthenticationRequired_FromAuthorizeAttribute()
{
    // Test method with [Authorize]
    // Expected: RequiresAuthentication = true
}
```

**Test 1.3**: Store and retrieve tool metadata
```csharp
[Fact]
public void Should_StoreAndRetrieve_ToolMetadata()
{
    // Register metadata for tool "create"
    // Should retrieve same metadata by name
}
```

### GREEN Phase - Implementation

**File**: `src/Zero.Mcp.Extensions/ToolMetadata.cs`
```csharp
namespace Zero.Mcp.Extensions;

public class ToolMetadata
{
    public string ToolName { get; init; } = string.Empty;
    public UserRole? MinimumRole { get; init; }
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
        var authorizeAttrs = method.GetCustomAttributes<AuthorizeAttribute>().ToArray();

        var requiresAuth = authorizeAttrs.Any();

        UserRole? minimumRole = null;
        var policies = new List<string>();

        foreach (var attr in authorizeAttrs)
        {
            if (!string.IsNullOrEmpty(attr.Policy))
            {
                policies.Add(attr.Policy);

                // Parse "MinimumRole:Member" → UserRole.Member
                if (attr.Policy.StartsWith("MinimumRole:"))
                {
                    var roleStr = attr.Policy.Substring("MinimumRole:".Length);
                    if (Enum.TryParse<UserRole>(roleStr, out var role))
                    {
                        minimumRole = role;
                    }
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
- Add metadata extraction during tool registration
- Register IToolMetadataStore as singleton
- Store metadata for each tool

### VERIFY Phase
```bash
Use build-agent to build Zero.Mcp.Extensions
Use test-agent to run tests for ToolMetadataTests
```

**Expected**: 3/3 tests passing, CLEAN build

---

## TDDAB Block 2: Tool Filtering Service

### Objective
Implement service that filters tools based on user permissions

### RED Phase - Tests

**Test 2.1**: Filter tools for Viewer role
```csharp
[Fact]
public async Task Should_ReturnOnlyReadTools_ForViewer()
{
    // User role: Viewer (0)
    // Tools: get_by_id, get_all, create, update, promote
    // Expected: [get_by_id, get_all]
}
```

**Test 2.2**: Filter tools for Member role
```csharp
[Fact]
public async Task Should_ReturnReadAndCreateTools_ForMember()
{
    // User role: Member (1)
    // Expected: [get_by_id, get_all, create]
}
```

**Test 2.3**: Filter tools for Manager role
```csharp
[Fact]
public async Task Should_ReturnReadCreateUpdateTools_ForManager()
{
    // User role: Manager (2)
    // Expected: [get_by_id, get_all, create, update]
}
```

**Test 2.4**: Return all tools for Admin role
```csharp
[Fact]
public async Task Should_ReturnAllTools_ForAdmin()
{
    // User role: Admin (3)
    // Expected: [get_by_id, get_all, create, update, promote]
}
```

**Test 2.5**: Handle unauthenticated user
```csharp
[Fact]
public async Task Should_ReturnEmpty_ForUnauthenticatedUser()
{
    // No user context
    // Expected: [] (empty list)
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
- Register IToolFilterService as scoped

### VERIFY Phase
```bash
Use build-agent to build Zero.Mcp.Extensions
Use test-agent to run tests for ToolFilterServiceTests
```

**Expected**: 5/5 tests passing, CLEAN build

---

## TDDAB Block 3: MCP Tools/List Interceptor

### Objective
Intercept `tools/list` MCP requests and apply filtering

### RED Phase - Tests

**Test 3.1**: Intercept tools/list request
```csharp
[Fact]
public async Task Should_InterceptToolsList_Request()
{
    // MCP request with method = "tools/list"
    // Expected: Interceptor invoked
}
```

**Test 3.2**: Apply filtering to tools/list response
```csharp
[Fact]
public async Task Should_FilterToolsList_Response()
{
    // User: Viewer
    // Original list: [get_by_id, get_all, create, update, promote]
    // Expected filtered: [get_by_id, get_all]
}
```

**Test 3.3**: Pass through non-tools/list requests
```csharp
[Fact]
public async Task Should_PassThrough_NonToolsListRequests()
{
    // MCP request with method = "tools/call"
    // Expected: No interception, normal processing
}
```

### GREEN Phase - Implementation

**Challenge**: MCP SDK might not expose interceptor hooks for `tools/list`

**Solution Option 1**: Wrap IMcpServer with decorator
```csharp
public class FilteredMcpServerDecorator : IMcpServer
{
    private readonly IMcpServer _inner;
    private readonly IToolFilterService _filterService;

    public async Task<ToolListResult> ListToolsAsync(CancellationToken ct)
    {
        var allTools = await _inner.ListToolsAsync(ct);
        var authorizedToolNames = await _filterService.GetAuthorizedToolsAsync(
            allTools.Tools.Select(t => t.Name), ct);

        var filteredTools = allTools.Tools
            .Where(t => authorizedToolNames.Contains(t.Name))
            .ToList();

        return new ToolListResult { Tools = filteredTools };
    }

    // Delegate other methods to _inner
}
```

**Solution Option 2**: Custom middleware in MapMcp
```csharp
public static IEndpointConventionBuilder MapZeroMcp(
    this IEndpointRouteBuilder app,
    string? path = null)
{
    var options = app.ServiceProvider.GetService<ZeroMcpOptions>() ?? new ZeroMcpOptions();
    var effectivePath = path ?? options.McpEndpointPath;

    // Register filtering middleware
    app.Use(async (context, next) =>
    {
        if (context.Request.Path == effectivePath)
        {
            // Parse MCP request
            // If method == "tools/list", apply filtering
            // Otherwise pass through
        }
        await next();
    });

    var builder = app.MapMcp(effectivePath);

    if (options.RequireAuthentication)
    {
        builder.RequireAuthorization();
    }

    return builder;
}
```

**Implementation**: Use Solution Option 1 (cleaner, more testable)

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
            allTools.Tools.Select(t => t.Name),
            cancellationToken);

        var filteredTools = allTools.Tools
            .Where(t => authorizedToolNames.Contains(t.Name))
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

    // ... other IMcpServer methods
}
```

**Modification**: `src/Zero.Mcp.Extensions/McpServerBuilderExtensions.cs`
- Wrap registered IMcpServer with FilteredMcpServer decorator
- Only if options.UseAuthorization is true

### VERIFY Phase
```bash
Use build-agent to build Zero.Mcp.Extensions
Use test-agent to run tests for FilteredMcpServerTests
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
    // Authenticate as viewer
    var result = await _mcpClient.ListToolsAsync();

    result.Tools.Should().HaveCount(2);
    result.Tools.Select(t => t.Name).Should().BeEquivalentTo(
        new[] { "get_by_id", "get_all" });
}
```

**Test 4.2**: Member sees read + create tools
```csharp
[Fact]
public async Task Should_ShowReadAndCreateTools_ForMemberRole()
{
    // Authenticate as alice (Member)
    var result = await _mcpClient.ListToolsAsync();

    result.Tools.Should().HaveCount(3);
    result.Tools.Select(t => t.Name).Should().BeEquivalentTo(
        new[] { "get_by_id", "get_all", "create" });
}
```

**Test 4.3**: Manager sees read + create + update tools
```csharp
[Fact]
public async Task Should_ShowReadCreateUpdateTools_ForManagerRole()
{
    // Authenticate as bob (Manager)
    var result = await _mcpClient.ListToolsAsync();

    result.Tools.Should().HaveCount(4);
    result.Tools.Select(t => t.Name).Should().BeEquivalentTo(
        new[] { "get_by_id", "get_all", "create", "update" });
}
```

**Test 4.4**: Admin sees all tools
```csharp
[Fact]
public async Task Should_ShowAllTools_ForAdminRole()
{
    // Authenticate as admin
    var result = await _mcpClient.ListToolsAsync();

    result.Tools.Should().HaveCount(5);
    result.Tools.Select(t => t.Name).Should().BeEquivalentTo(
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
    var response = await client.PostAsync("/mcp", ...);
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

**Test 4.6**: get_public_info visible to all
```csharp
[Fact]
public async Task Should_ShowPublicTool_ToAllRoles()
{
    // For each role (Viewer, Member, Manager, Admin)
    // get_public_info should appear in tools/list
}
```

**Test 4.7**: Viewer cannot call filtered-out tools
```csharp
[Fact]
public async Task Should_Return403_WhenViewerCallsFilteredTool()
{
    // Authenticate as viewer
    // tools/list shows [get_by_id, get_all]

    // Try to call create (not in list)
    var result = await _mcpClient.CallToolAsync("create", args);

    // Should still get 403 (double protection: filter + authorization)
    result.IsError.Should().BeTrue();
}
```

**Test 4.8**: Tool filtering respects role hierarchy
```csharp
[Fact]
public async Task Should_RespectRoleHierarchy_InToolFiltering()
{
    // Manager sees all Member tools + Manager tools
    var managerTools = await GetToolsFor("bob@example.com");
    var memberTools = await GetToolsFor("alice@example.com");

    managerTools.Should().Contain(memberTools);
    managerTools.Should().HaveCountGreaterThan(memberTools.Count());
}
```

### GREEN Phase - Implementation

**File**: `tests/McpPoc.Api.Tests/ToolFilteringTests.cs`
- New test class with 8 integration tests
- Uses McpApiFixture with role-based clients
- Verifies tools/list returns correct tools per role

**Note**: Most implementation is already done in Blocks 1-3, this block is primarily verification

### VERIFY Phase
```bash
Use build-agent to build McpPoc.Api
Use test-agent to run tests for ToolFilteringTests
```

**Expected**: 8/8 tests passing, CLEAN build

**Final verification**:
```bash
Use test-agent to run all tests for McpPoc.Api.Tests
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
