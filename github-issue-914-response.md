Hi @stephentoub and @pederpedersen,

After extensive testing, we've identified that the issue involves **both ValueTask/Task unwrapping AND ActionResult<T> unwrapping** - both are needed for controllers to work as MCP tools.

## The Real Problem

When returning `Task<ActionResult<User>>` from a controller:

1. **First problem**: The `Task<T>` must be properly awaited and unwrapped
2. **Second problem**: The `ActionResult<T>` wrapper must be unwrapped to extract the actual `User` value
3. Without both steps, the MCP serializer sees wrapper objects and returns `{}`

## Our Solution

We've built a custom `MarshalResult` that handles the complete unwrapping chain:

```csharp
MarshalResult = async (result, resultType, ct) => await MarshalResult.UnwrapAsync(result)
```

### The Complete Unwrapping Process

```csharp
// 1. Unwrap Task<T> / ValueTask<T>
if (result is Task task)
{
    await task;
    if (taskType.IsGenericType)
    {
        var resultProperty = taskType.GetProperty("Result");
        result = resultProperty?.GetValue(task);
    }
}

// 2. Unwrap ActionResult<T>
if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(ActionResult<>))
{
    var resultProperty = resultType.GetProperty("Result");
    var valueProperty = resultType.GetProperty("Value");

    var actionResult = resultProperty?.GetValue(result);
    if (actionResult is not null)
    {
        result = actionResult;
    }
    else
    {
        return valueProperty?.GetValue(result);
    }
}

// 3. Extract from ObjectResult
if (result is ObjectResult objectResult)
{
    return objectResult.Value; // Can be null for nullable types
}
```

### Full Feature Set

- `Task<T>` and `ValueTask<T>` unwrapping
- `ActionResult<T>` extraction (gets `.Result` or `.Value`)
- `ObjectResult` value extraction
- Null value support for nullable types (`Ok(null)` is valid)
- Error result handling (throws exception for `NotFound`, `BadRequest`, etc.)

### Usage Example

```csharp
[ApiController]
[McpServerToolType]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    [McpServerTool]
    public async Task<ActionResult<User>> GetById(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        return user == null ? NotFound() : Ok(user);
    }
}
```

With our custom marshalling, the MCP client receives the actual `User` object after **both** Task and ActionResult unwrapping.

## Test Coverage

We have 55 passing tests covering:

- Task/ValueTask unwrapping
- ActionResult<T> unwrapping (including null values)
- Complete unwrapping chain for `Task<ActionResult<T>>`
- Authorization pre-filtering
- HTTP + MCP integration

## Public Repository

The full implementation is available at: https://github.com/LadislavSopko/net-api-with-mcp

The library (`src/Zero.Mcp.Extensions/`) includes:

- Complete `MarshalResult` unwrapping logic (Task + ActionResult)
- Configuration system (`ZeroMcpOptions`)
- Authorization pre-filter support
- Comprehensive test suite

## Current Status

We're currently refining tool metadata and descriptions to ensure optimal usage with LLM clients. This includes:

- Fine-tuning parameter descriptions for better AI understanding
- Optimizing tool documentation for different AI models
- Testing with various LLM providers to ensure compatibility

The core unwrapping logic is production-ready and fully tested.

## Contribution to SDK?

This complete unwrapping logic could be added to the MCP SDK as:

- Built-in marshaller for ASP.NET Core controllers (opt-in)
- Reference implementation in docs/samples
- Or keep as external library

**We'd be happy to contribute this via PR if you think it belongs in the SDK!**

The original issue report was correct - the default marshalling doesn't properly handle the `Task<ActionResult<T>>` chain that controllers return.

Thanks for your feedback!
