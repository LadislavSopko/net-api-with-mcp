using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace McpPoc.Api.Extensions;

/// <summary>
/// MCP Server builder extensions with ActionResult unwrapping support.
/// </summary>
public static class McpServerBuilderExtensions
{
    /// <summary>
    /// Registers MCP tools from assembly with ActionResult unwrapping support.
    /// This is like WithToolsFromAssembly but with custom MarshalResult that unwraps ActionResult&lt;T&gt;.
    /// </summary>
    /// <param name="builder">The MCP server builder</param>
    /// <param name="toolAssembly">Assembly to scan for tools (defaults to calling assembly)</param>
    /// <returns>The builder for chaining</returns>
    public static IMcpServerBuilder WithToolsFromAssemblyUnwrappingActionResult(
        this IMcpServerBuilder builder,
        Assembly? toolAssembly = null)
    {
        toolAssembly ??= Assembly.GetCallingAssembly();

        // Find all types with [McpServerToolType]
        var toolTypes = toolAssembly.GetTypes()
            .Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() is not null);

        foreach (var toolType in toolTypes)
        {
            // Find all methods with [McpServerTool]
            var toolMethods = toolType.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Static | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() is not null);

            foreach (var method in toolMethods)
            {
                var serializerOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
                };

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
                                Name = ConvertToSnakeCase(method.Name),
                                MarshalResult = UnwrapActionResult,
                                SerializerOptions = serializerOptions
                            });
                        return McpServerTool.Create(aiFunction, new McpServerToolCreateOptions { Services = services });
                    });
                }
                else
                {
                    // Instance method with custom marshaller
                    builder.Services.AddSingleton<McpServerTool>(services =>
                    {
                        var aiFunction = AIFunctionFactory.Create(
                            method,
                            args => CreateControllerInstance(args.Services!, toolType),
                            new AIFunctionFactoryOptions
                            {
                                Name = ConvertToSnakeCase(method.Name),
                                MarshalResult = UnwrapActionResult,
                                SerializerOptions = serializerOptions
                            });

                        // SDK automatically collects metadata including [Authorize] attributes
                        // See: AIFunctionMcpServerTool.CreateMetadata in SDK
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

    /// <summary>
    /// Creates a controller instance using DI.
    /// </summary>
    private static object CreateControllerInstance(IServiceProvider services, Type controllerType)
    {
        return ActivatorUtilities.CreateInstance(services, controllerType);
    }

    /// <summary>
    /// Custom MarshalResult that unwraps ActionResult&lt;T&gt; before serialization.
    /// This is the FIX for the POC - it extracts the actual value from ActionResult wrappers.
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
    /// Unwraps ActionResult&lt;T&gt; to get the actual value.
    /// </summary>
    private static object? UnwrapIfActionResult(object result)
    {
        var resultType = result.GetType();

        // Handle ActionResult<T>
        if (resultType.IsGenericType &&
            resultType.GetGenericTypeDefinition() == typeof(ActionResult<>))
        {
            // Get Result property (the IActionResult inside)
            var resultProperty = resultType.GetProperty("Result");
            var actionResult = resultProperty?.GetValue(result);

            if (actionResult != null)
            {
                return UnwrapIActionResult(actionResult);
            }
        }

        // Handle IActionResult directly (OkObjectResult, CreatedAtActionResult, etc.)
        if (result is IActionResult)
        {
            return UnwrapIActionResult(result);
        }

        return result;
    }

    /// <summary>
    /// Extracts the Value property from IActionResult implementations.
    /// </summary>
    private static object? UnwrapIActionResult(object actionResult)
    {
        // OkObjectResult, CreatedAtActionResult, etc. have Value property
        var valueProperty = actionResult.GetType().GetProperty("Value");
        return valueProperty?.GetValue(actionResult) ?? actionResult;
    }

    /// <summary>
    /// Converts method name to snake_case and removes Async suffix.
    /// </summary>
    private static string ConvertToSnakeCase(string methodName)
    {
        // Remove "Async" suffix if present
        if (methodName.EndsWith("Async") && methodName.Length > 5)
        {
            methodName = methodName.Substring(0, methodName.Length - 5);
        }

        // Convert to snake_case using JsonNamingPolicy
        return JsonNamingPolicy.SnakeCaseLower.ConvertName(methodName) ?? methodName;
    }
}
