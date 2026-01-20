# TDDAB Plan: Tool Naming Convention + IMcpRequestContext

## Overview

**Features:**
1. **Tool Naming Convention** - Configurable naming with ControllerPrefix support
2. **IMcpRequestContext** - Header access + automatic `x-mcp-call` marker

**Current State:**
- Tests: 97/97 passing
- Library: Zero.Mcp.Extensions v2.0.0 (.NET 10)

**Target State:**
- Tests: ~115 (97 + ~18 new)
- Both features fully tested from unit to e2e

---

## Feature 1: Tool Naming Convention

### Problem
Generic controllers produce duplicate tool names:
- `ProductsController.GetById()` → `get_by_id`
- `OrdersController.GetById()` → `get_by_id`
- Last one wins, first is lost

### Solution
```csharp
public enum ToolNamingConvention
{
    MethodOnly,        // get_by_id (default, backward compatible)
    ControllerPrefix   // products_get_by_id
}

public class ZeroMcpOptions
{
    // Existing...
    public ToolNamingConvention NamingConvention { get; set; } = ToolNamingConvention.MethodOnly;
    public string ToolNameSeparator { get; set; } = "_";
}
```

**Priority:**
1. `[McpServerTool(Name = "explicit")]` → always wins
2. `NamingConvention` → applied if Name not specified

---

## Feature 2: IMcpRequestContext

### Problem
- Need to know if request came via MCP endpoint
- Need to read custom headers from MCP client config
- `x-mcp-call` header should be guaranteed present

### Solution
```csharp
public interface IMcpRequestContext
{
    bool IsMcpCall { get; }
    string? GetHeader(string name);
    IHeaderDictionary? Headers { get; }
}

internal class McpRequestContext : IMcpRequestContext
{
    private readonly IHttpContextAccessor _accessor;

    public bool IsMcpCall =>
        _accessor.HttpContext?.Items.ContainsKey("__McpCall") == true;

    public string? GetHeader(string name) =>
        IsMcpCall ? _accessor.HttpContext?.Request.Headers[name].FirstOrDefault() : null;

    public IHeaderDictionary? Headers =>
        IsMcpCall ? _accessor.HttpContext?.Request.Headers : null;
}
```

**x-mcp-call Header:**
- Added automatically server-side via middleware
- Guaranteed present on every MCP call
- Client doesn't need to remember to send it

---

## TDDAB Blocks

### Block 1: ToolNamingConvention Enum + Options (Unit)
**Files:** `ZeroMcpOptions.cs`
**Tests:** 4
**LOC:** ~15

```csharp
// Tests
[Fact] NamingConvention_DefaultsTo_MethodOnly()
[Fact] ToolNameSeparator_DefaultsTo_Underscore()
[Fact] NamingConvention_CanBeSet_ToControllerPrefix()
[Fact] ToolNameSeparator_CanBeCustomized()
```

### Block 2: ToolNameGenerator Helper (Unit)
**Files:** `ToolNameGenerator.cs` (new)
**Tests:** 6
**LOC:** ~40

```csharp
internal static class ToolNameGenerator
{
    public static string GenerateName(
        MethodInfo method,
        Type controllerType,
        ZeroMcpOptions options);

    internal static string GetControllerPrefix(Type controllerType);
    internal static string ToSnakeCase(string name);
}

// Tests
[Fact] GenerateName_WithMethodOnly_ReturnsMethodNameOnly()
[Fact] GenerateName_WithControllerPrefix_ReturnsControllerPrefixedName()
[Fact] GenerateName_WithExplicitAttribute_IgnoresConvention()
[Fact] GetControllerPrefix_RemovesControllerSuffix()
[Fact] GetControllerPrefix_ConvertsToSnakeCase()
[Fact] ToSnakeCase_ConvertsCorrectly()
```

### Block 3: Integration in McpServerBuilderExtensions (Unit)
**Files:** `McpServerBuilderExtensions.cs`
**Tests:** 3
**LOC:** ~20

```csharp
// Modify existing tool registration to use ToolNameGenerator

// Tests
[Fact] WithToolsFromAssembly_UsesMethodOnly_ByDefault()
[Fact] WithToolsFromAssembly_UsesControllerPrefix_WhenConfigured()
[Fact] WithToolsFromAssembly_ExplicitName_OverridesConvention()
```

### Block 4: IMcpRequestContext Interface + Implementation (Unit)
**Files:** `IMcpRequestContext.cs`, `McpRequestContext.cs` (new)
**Tests:** 5
**LOC:** ~35

```csharp
// Tests
[Fact] IsMcpCall_ReturnsFalse_WhenNotMcpRequest()
[Fact] IsMcpCall_ReturnsTrue_WhenMcpMarkerPresent()
[Fact] GetHeader_ReturnsNull_WhenNotMcpCall()
[Fact] GetHeader_ReturnsValue_WhenMcpCall()
[Fact] Headers_ReturnsNull_WhenNotMcpCall()
```

### Block 5: MCP Middleware (x-mcp-call header injection)
**Files:** `McpEndpointExtensions.cs` (modify MapZeroMcp)
**Tests:** 3
**LOC:** ~25

```csharp
// Middleware adds Items["__McpCall"] = true and header x-mcp-call

// Tests
[Fact] McpEndpoint_SetsItemsMarker()
[Fact] McpEndpoint_AddsXMcpCallHeader()
[Fact] NonMcpEndpoint_NoMarkerOrHeader()
```

### Block 6: Service Registration
**Files:** `McpServerBuilderExtensions.cs`
**Tests:** 2
**LOC:** ~10

```csharp
// AddZeroMcpExtensions registers:
// - IHttpContextAccessor (if not already)
// - IMcpRequestContext as Scoped

// Tests
[Fact] AddZeroMcpExtensions_RegistersIMcpRequestContext()
[Fact] IMcpRequestContext_IsScopedLifetime()
```

### Block 7: E2E Tool Naming (Integration)
**Files:** `ToolNamingTests.cs` (new in McpPoc.Api.Tests)
**Tests:** 4
**LOC:** ~80

```csharp
// Need two test controllers with same method names
[McpServerToolType]
public class AlphaController { [McpServerTool] GetAll(), GetById() }

[McpServerToolType]
public class BetaController { [McpServerTool] GetAll(), GetById() }

// Tests
[Fact] MethodOnly_Convention_LastControllerWins() // documents current behavior
[Fact] ControllerPrefix_Convention_BothToolsDiscovered()
[Fact] ControllerPrefix_Convention_ToolsHaveCorrectNames()
[Fact] ExplicitName_Attribute_OverridesConvention()
```

### Block 8: E2E MCP Context (Integration)
**Files:** `McpRequestContextTests.cs` (new in McpPoc.Api.Tests)
**Tests:** 4
**LOC:** ~60

```csharp
// Add test endpoint that returns IMcpRequestContext info

// Tests
[Fact] McpCall_IsMcpCall_ReturnsTrue()
[Fact] McpCall_XMcpCallHeader_IsPresent()
[Fact] McpCall_CustomHeader_CanBeRead()
[Fact] HttpCall_IsMcpCall_ReturnsFalse()
```

---

## Summary

| Block | Focus | Tests | LOC |
|-------|-------|-------|-----|
| 1 | Options enum + defaults | 4 | ~15 |
| 2 | ToolNameGenerator | 6 | ~40 |
| 3 | Builder integration | 3 | ~20 |
| 4 | IMcpRequestContext | 5 | ~35 |
| 5 | Middleware x-mcp-call | 3 | ~25 |
| 6 | Service registration | 2 | ~10 |
| 7 | E2E Tool Naming | 4 | ~80 |
| 8 | E2E MCP Context | 4 | ~60 |
| **Total** | | **31** | **~285** |

**Final test count:** 97 + 31 = **128 tests**

---

## Execution Order

1. Block 1 → Options (foundation)
2. Block 2 → ToolNameGenerator (core logic)
3. Block 4 → IMcpRequestContext interface (parallel track)
4. Block 5 → Middleware
5. Block 6 → Registration
6. Block 3 → Builder integration
7. Block 7 → E2E naming
8. Block 8 → E2E context

---

## Files Changed/Created

**New files:**
- `src/Zero.Mcp.Extensions/ToolNamingConvention.cs`
- `src/Zero.Mcp.Extensions/ToolNameGenerator.cs`
- `src/Zero.Mcp.Extensions/IMcpRequestContext.cs`
- `src/Zero.Mcp.Extensions/McpRequestContext.cs`
- `tests/Zero.Mcp.Extensions.Tests/ToolNameGeneratorTests.cs`
- `tests/Zero.Mcp.Extensions.Tests/McpRequestContextTests.cs`
- `tests/McpPoc.Api.Tests/ToolNamingTests.cs`
- `tests/McpPoc.Api.Tests/McpRequestContextE2ETests.cs`

**Modified files:**
- `src/Zero.Mcp.Extensions/ZeroMcpOptions.cs`
- `src/Zero.Mcp.Extensions/McpServerBuilderExtensions.cs`
- `src/Zero.Mcp.Extensions/McpEndpointExtensions.cs`

---

## Block 9: Version Bump + Release Notes

**Files:** `Version.props`, `Zero.Mcp.Extensions.csproj`
**Tests:** 0 (verification only)

```xml
<!-- Version.props -->
<MainVersion>2.1.0</MainVersion>

<!-- Zero.Mcp.Extensions.csproj -->
<PackageReleaseNotes>
v2.1.0:
- Tool naming conventions (ControllerPrefix) for generic controllers
- IMcpRequestContext for header access and MCP call detection
- Automatic x-mcp-call header injection
</PackageReleaseNotes>
```

**Verification:**
- `dotnet pack` succeeds
- Package version is 2.1.0
- Release notes included

---

## Summary Updated

| Block | Focus | Tests | LOC |
|-------|-------|-------|-----|
| 1 | Options enum + defaults | 4 | ~15 |
| 2 | ToolNameGenerator | 6 | ~40 |
| 3 | Builder integration | 3 | ~20 |
| 4 | IMcpRequestContext | 5 | ~35 |
| 5 | Middleware x-mcp-call | 3 | ~25 |
| 6 | Service registration | 2 | ~10 |
| 7 | E2E Tool Naming | 4 | ~80 |
| 8 | E2E MCP Context | 4 | ~60 |
| 9 | Version bump + pack | 0 | ~5 |
| **Total** | | **31** | **~290** |

---

## Ready for ACT

Type `ACT` to begin Block 1.
