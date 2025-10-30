using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Zero.Mcp.Extensions;

/// <summary>
/// Marshals ActionResult&lt;T&gt; responses to extract the actual value for MCP serialization.
/// </summary>
internal static class MarshalResult
{
    /// <summary>
    /// Unwraps an ActionResult&lt;T&gt; or IActionResult to extract the actual value.
    /// Returns null for Ok(null) (valid for nullable types).
    /// Throws InvalidOperationException if controller returns an error result.
    /// </summary>
    /// <param name="result">The result to unwrap.</param>
    /// <returns>The unwrapped value (can be null), or the original result if not an ActionResult.</returns>
    /// <exception cref="InvalidOperationException">When an error result (NotFound, BadRequest, etc.) is returned.</exception>
    public static async ValueTask<object?> UnwrapAsync(object? result)
    {
        if (result is null)
            return null;

        // Handle ValueTask wrapping
        if (result is ValueTask valueTask)
        {
            await valueTask;
            return null;
        }

        var resultType = result.GetType();

        // Handle ValueTask<T>
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            dynamic vt = result;
            result = await vt;
        }

        // Handle Task<T>
        if (result is Task task)
        {
            await task;
            var taskType = task.GetType();
            if (taskType.IsGenericType)
            {
                var resultProperty = taskType.GetProperty("Result");
                result = resultProperty?.GetValue(task);
            }
            else
            {
                return null;
            }
        }

        if (result is null)
            return null;

        resultType = result.GetType();

        // Handle ActionResult<T>
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(ActionResult<>))
        {
            var resultProperty = resultType.GetProperty("Result");
            var valueProperty = resultType.GetProperty("Value");

            var actionResult = resultProperty?.GetValue(result);
            if (actionResult is not null)
            {
                result = actionResult;
                resultType = result.GetType();
            }
            else
            {
                return valueProperty?.GetValue(result);
            }
        }

        // Handle IActionResult with value
        if (result is IActionResult actionResultInterface)
        {
            if (actionResultInterface is IStatusCodeActionResult statusCodeResult)
            {
                // ObjectResult is valid even if Value is null (for nullable types)
                if (statusCodeResult is ObjectResult objectResult)
                {
                    return objectResult.Value; // Can be null for ActionResult<T?>
                }
            }

            // Error results like NotFoundResult, BadRequestResult should throw
            throw new InvalidOperationException(
                $"Controller returned error result: {actionResultInterface.GetType().Name}. " +
                "MCP tools should return domain error objects wrapped in ActionResult<T> instead of IActionResult error types. " +
                "Example: return new ActionResult<User>(new ObjectResult(new { error = \"Not found\" }) { StatusCode = 404 });");
        }

        return result;
    }
}
