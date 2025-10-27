# Filter Pipeline & DI Scoping - Issues to Check Tomorrow

## Session Summary (2025-10-22)

Authentication with Keycloak is working after fixing DNS/host mismatch issue. Now need to implement per-tool authorization checks and verify DI scoping works correctly.

---

## CRITICAL ISSUES TO VERIFY

### 1. DI Scoping for EF Core ⚠️

**Problem**: Controllers must have proper DI scoping per MCP tool invocation (like normal API calls), otherwise EF Core DbContext and other scoped services will NOT work correctly.

**Current Understanding**:
- HTTP request → ASP.NET Core creates scope → `HttpContext.RequestServices` (scoped)
- MCP uses `context.RequestServices` → passed to `McpServer`
- Our controller factory gets `args.Services` (from `RequestServiceProvider` wrapper)
- **Should work**, but needs verification

**Concerns**:
- `StreamableHttpHandler.cs:228` sets `ScopeRequests = false` in stateless mode
- `McpServerImpl.cs:667` conditionally creates scopes based on `ScopeRequests` flag
- If `ScopeRequests = false`: uses Services directly (no per-request scope!)
- If `ScopeRequests = true`: creates `AsyncScope` per request

**Files to Check**:
- `/mnt/c/Projekty/AI_Works/net-api-with-mcp/3rdp/csharp-sdk/src/ModelContextProtocol.AspNetCore/StreamableHttpHandler.cs:220-228`
- `/mnt/c/Projekty/AI_Works/net-api-with-mcp/3rdp/csharp-sdk/src/ModelContextProtocol.Core/Server/McpServerImpl.cs:662-695`

**What to Test**:
```csharp
// Create a scoped service with a unique ID
public class ScopedRequestTracker
{
    public Guid RequestId { get; } = Guid.NewGuid();
}

// Register as scoped
builder.Services.AddScoped<ScopedRequestTracker>();

// In controller, inject and return ID
[McpServerTool]
public ActionResult<string> GetScopeId([FromServices] ScopedRequestTracker tracker)
{
    return Ok(tracker.RequestId.ToString());
}

// Call twice - should return DIFFERENT IDs (proving new scope per call)
```

**Expected Behavior**:
- ✅ Each MCP tool call creates new scope
- ✅ Scoped services are new instances per call
- ✅ EF Core DbContext would be unique per tool invocation

**If Broken**:
- ❌ Same scope shared across multiple calls
- ❌ Scoped services reused incorrectly
- ❌ EF Core DbContext tracking issues

---

### 2. launchSettings.json Still Uses localhost ⚠️

**Problem**: After switching to 127.0.0.1 everywhere for DNS performance, `launchSettings.json` still has `"applicationUrl": "http://localhost:5001"`

**Impact**:
- When running API with `dotnet run`, it binds to localhost:5001
- But Keycloak expects 127.0.0.1:5001 (in realm config)
- Causes JWT issuer mismatch → 401 errors

**File**: `/mnt/c/Projekty/AI_Works/net-api-with-mcp/src/McpPoc.Api/Properties/launchSettings.json:9`

**Fix**:
```json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://127.0.0.1:5001"  // Change from localhost
    }
  }
}
```

---

## DESIGN TASKS

### 3. Filter Pipeline Architecture 🎯

**Goal**: Implement pre-filters and post-filters for MCP tool invocations:
- **Pre-filters**: Authorization, validation, logging (BEFORE method execution)
- **Post-filters**: Result transformation, error handling, logging (AFTER method execution)

**Constraint**: Must be hidden inside AIFunction creation - no complex wrappers/proxies

**Current Interception Points**:
```csharp
// In McpServerBuilderExtensions.cs

// LINE 75: Target Factory = PRE-FILTER opportunity
args => CreateControllerInstance(args.Services!, toolType)
// Can add: authorization check, validation, logging BEFORE creating controller

// LINE 109-121: MarshalResult = POST-FILTER opportunity (already exists!)
UnwrapActionResult(result, resultType, cancellationToken)
// Already does: unwrap ActionResult<T>
// Can add: logging, error handling, additional transformations
```

**Simple Design Proposal**:

```csharp
// PRE-FILTER: Enhance target factory
private static object CreateControllerWithPreFilters(
    AIFunctionContext args,
    Type controllerType,
    MethodInfo method)
{
    var services = args.Services;

    // 1. Get HttpContext via IHttpContextAccessor
    var httpContextAccessor = services.GetService<IHttpContextAccessor>();
    var httpContext = httpContextAccessor?.HttpContext;

    // 2. Check [Authorize] attribute on method
    var authorizeAttr = method.GetCustomAttribute<AuthorizeAttribute>();
    if (authorizeAttr != null)
    {
        // Verify authenticated
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Authentication required");
        }

        // Check roles if specified
        if (!string.IsNullOrEmpty(authorizeAttr.Roles))
        {
            var roles = authorizeAttr.Roles.Split(',').Select(r => r.Trim());
            if (!roles.Any(role => httpContext.User.IsInRole(role)))
            {
                throw new UnauthorizedAccessException(
                    $"User lacks required role: {authorizeAttr.Roles}");
            }
        }

        // Check policy if specified
        if (!string.IsNullOrEmpty(authorizeAttr.Policy))
        {
            // TODO: Use IAuthorizationService to evaluate policy
        }
    }

    // 3. Log pre-execution (optional)
    var logger = services.GetService<ILogger<McpServerBuilderExtensions>>();
    logger?.LogInformation("Executing tool: {Method}", method.Name);

    // 4. Create controller instance (already authorized)
    return ActivatorUtilities.CreateInstance(services, controllerType);
}

// POST-FILTER: Enhance MarshalResult
private static ValueTask<object?> ProcessResultWithPostFilters(
    object? result,
    Type? resultType,
    CancellationToken cancellationToken)
{
    try
    {
        // 1. Unwrap ActionResult (existing functionality)
        var unwrapped = result == null ? null : UnwrapIfActionResult(result);

        // 2. Log result (optional)
        // logger?.LogInformation("Tool result: {Result}", unwrapped);

        // 3. Additional transformations if needed

        return ValueTask.FromResult(unwrapped);
    }
    catch (Exception ex)
    {
        // 4. Error handling/logging
        // logger?.LogError(ex, "Error processing result");
        throw;
    }
}
```

**Challenge**: How to pass `MethodInfo` to the target factory?
- Current: `args => CreateControllerInstance(args.Services!, toolType)`
- Need: `args => CreateControllerWithPreFilters(args, controllerType, method)`

**Solution**: Capture `method` in closure when creating the factory:
```csharp
foreach (var method in toolMethods)
{
    builder.Services.AddSingleton<McpServerTool>(services =>
    {
        var methodCopy = method; // Capture in closure

        var aiFunction = AIFunctionFactory.Create(
            methodCopy,
            args => CreateControllerWithPreFilters(args, toolType, methodCopy), // Pass method
            new AIFunctionFactoryOptions
            {
                Name = ConvertToSnakeCase(methodCopy.Name),
                MarshalResult = ProcessResultWithPostFilters, // Enhanced
                SerializerOptions = serializerOptions
            });

        return McpServerTool.Create(aiFunction, new McpServerToolCreateOptions
        {
            Services = services
        });
    });
}
```

---

## QUESTIONS TO ANSWER

1. **Does current DI scoping work correctly?**
   - Test with scoped service (ScopedRequestTracker)
   - Test with EF Core DbContext if available
   - Verify each tool call gets new scope

2. **Is stateful or stateless mode being used?**
   - Check `HttpServerTransportOptions.Stateless` value at runtime
   - If stateless: `ScopeRequests = false` (potential issue!)
   - If stateful: `ScopeRequests = true` (should be fine)

3. **Should we explicitly configure scoping?**
   - Can we force `ScopeRequests = true` even in stateless mode?
   - Is there a configuration option to control this?

4. **How to access MethodInfo in target factory?**
   - Use closure to capture method reference
   - Pass as parameter somehow
   - Store in thread-local/async-local context

5. **Should IHttpContextAccessor be registered?**
   - Need it for accessing HttpContext in pre-filters
   - Check if already registered by ASP.NET Core
   - Add `builder.Services.AddHttpContextAccessor()` if needed

---

## FILES TO MODIFY

### `/src/McpPoc.Api/Extensions/McpServerBuilderExtensions.cs`
**Changes needed**:
- Line 75: Enhance target factory with pre-filters (authorization)
- Line 109: Enhance MarshalResult with post-filters (logging)
- Pass MethodInfo to both filters via closure

### `/src/McpPoc.Api/Properties/launchSettings.json`
**Changes needed**:
- Line 9: Change `"applicationUrl": "http://localhost:5001"` → `"http://127.0.0.1:5001"`

### `/src/McpPoc.Api/Program.cs`
**Changes needed**:
- Add `builder.Services.AddHttpContextAccessor()` if not present
- Consider adding scoped test service for verification

---

## TEST PLAN

### Test 1: Verify DI Scoping
```csharp
// Add to tests/McpPoc.Api.Tests/
public class DIScopingTests
{
    [Fact]
    public async Task Should_CreateNewScope_PerToolInvocation()
    {
        // Call MCP tool twice
        // Verify different scope IDs returned
        // Proves new scope per call
    }
}
```

### Test 2: Verify Authorization Filter
```csharp
public class AuthorizationFilterTests
{
    [Fact]
    public async Task Should_Block_UnauthorizedToolCall()
    {
        // Call tool with [Authorize] without token
        // Expect error (not 401, but MCP error response)
    }

    [Fact]
    public async Task Should_Block_WrongRole()
    {
        // Call tool with [Authorize(Roles = "admin")] as regular user
        // Expect error about missing role
    }
}
```

### Test 3: Verify Post-Filter
```csharp
public class PostFilterTests
{
    [Fact]
    public async Task Should_UnwrapActionResult()
    {
        // Call tool returning ActionResult<User>
        // Verify unwrapped User object in response
        // (Already tested, but verify still works after changes)
    }
}
```

---

## NEXT SESSION CHECKLIST

- [ ] Fix `launchSettings.json` to use 127.0.0.1:5001
- [ ] Verify DI scoping with test service
- [ ] Check if `IHttpContextAccessor` is registered
- [ ] Implement pre-filter authorization check
- [ ] Enhance post-filter with logging
- [ ] Test authorization filtering works
- [ ] Test role-based authorization
- [ ] Document final filter pipeline architecture
- [ ] Update Memory Bank with implementation

---

## KEY INSIGHTS

1. **Scoping is probably OK** because `HttpContext.RequestServices` is already scoped, but needs verification
2. **Filter pipeline should be simple** - just enhance existing interception points, no complex wrappers
3. **Authorization can be per-tool** by checking `[Authorize]` attribute in pre-filter
4. **MethodInfo can be captured** in closure when creating AIFunction factory
5. **launchSettings.json mismatch** is causing 401 issues when running with `dotnet run`

---

## REFERENCES

**SDK Files Analyzed**:
- `ModelContextProtocol.AspNetCore/StreamableHttpHandler.cs:220-228` (stateless mode sets ScopeRequests=false)
- `ModelContextProtocol.Core/Server/McpServerImpl.cs:662-695` (conditional scoping logic)
- `ModelContextProtocol.Core/Server/RequestServiceProvider.cs` (wraps service provider with request context)
- `ModelContextProtocol.Core/Server/AIFunctionMcpServerTool.cs:226-244` (where AIFunction is invoked)

**Project Files**:
- `src/McpPoc.Api/Extensions/McpServerBuilderExtensions.cs` (our custom registration)
- `src/McpPoc.Api/Properties/launchSettings.json` (needs fix)
- `src/McpPoc.Api/Program.cs` (MCP registration)

**Memory Bank**:
- `mem-bank-mbel5/activeContext.md` (updated with current session)
