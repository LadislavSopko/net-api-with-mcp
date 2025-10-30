using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Zero.Mcp.Extensions;

/// <summary>
/// Extension methods for configuring MCP server with ASP.NET Core controllers.
/// </summary>
public static class McpServerBuilderExtensions
{
    /// <summary>
    /// Adds MCP server with ActionResult unwrapping and authorization support.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for MCP options.</param>
    /// <returns>The MCP server builder for further configuration.</returns>
    public static IMcpServerBuilder AddZeroMcpExtensions(
        this IServiceCollection services,
        Action<ZeroMcpOptions>? configure = null)
    {
        // Create and configure options
        var options = new ZeroMcpOptions();
        configure?.Invoke(options);

        // Capture the calling assembly NOW if not explicitly provided
        options.ToolAssembly ??= Assembly.GetCallingAssembly();

        // Register options for access in MapZeroMcp
        services.AddSingleton(options);

        return services
            .AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssemblyUnwrappingActionResult(options);
    }

    /// <summary>
    /// Scans the assembly for controllers with [McpServerToolType] and registers methods
    /// with [McpServerTool] as MCP tools, unwrapping ActionResult&lt;T&gt; responses and
    /// performing pre-filter authorization checks.
    /// </summary>
    private static IMcpServerBuilder WithToolsFromAssemblyUnwrappingActionResult(
        this IMcpServerBuilder builder,
        ZeroMcpOptions options)
    {
        var toolAssembly = options.ToolAssembly!;
        var serializerOptions = options.GetEffectiveSerializerOptions();

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
                                Name = ConvertToSnakeCase(methodCopy.Name),
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

    /// <summary>
    /// Creates a controller instance with pre-filter authorization check using IAuthForMcpSupplier.
    /// </summary>
    private static object CreateControllerWithPreFilter(
        IServiceProvider services,
        Type controllerType,
        MethodInfo method,
        ZeroMcpOptions options)
    {
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(typeof(McpAuthorizationPreFilter));

        // Skip authorization if disabled in options
        if (!options.UseAuthorization)
        {
            logger.LogTrace("Authorization disabled in options, skipping check for: {Method}", method.Name);
            return ActivatorUtilities.CreateInstance(services, controllerType);
        }

        // Get auth supplier (required when UseAuthorization is true)
        var authSupplier = services.GetService<IAuthForMcpSupplier>();
        if (authSupplier == null)
        {
            logger.LogError("UseAuthorization is true but IAuthForMcpSupplier is not registered");
            throw new InvalidOperationException(
                "UseAuthorization is true but IAuthForMcpSupplier is not registered. " +
                "Either register IAuthForMcpSupplier or set UseAuthorization to false in ZeroMcpOptions.");
        }

        // Perform pre-filter authorization check
        var preFilter = new McpAuthorizationPreFilter(authSupplier, logger);
        var isAuthorized = preFilter.CheckAuthorizationAsync(method).GetAwaiter().GetResult();

        if (!isAuthorized)
        {
            logger.LogWarning(
                "Authorization failed for MCP tool: {Method} on {Controller}",
                method.Name,
                controllerType.Name);

            throw new UnauthorizedAccessException(
                $"Authorization failed for MCP tool: {method.Name}");
        }

        logger.LogTrace("Authorization successful for MCP tool: {Method}", method.Name);

        // Authorization passed - create controller instance
        return ActivatorUtilities.CreateInstance(services, controllerType);
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

/// <summary>
/// Marks a controller class as containing MCP server tools.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class McpServerToolTypeAttribute : Attribute
{
}

/// <summary>
/// Marks a controller method as an MCP server tool.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class McpServerToolAttribute : Attribute
{
}

/// <summary>
/// Extension methods for mapping MCP endpoints.
/// </summary>
public static class McpEndpointExtensions
{
    /// <summary>
    /// Maps the MCP endpoint using the configuration from ZeroMcpOptions.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="path">Optional path override. If not provided, uses path from ZeroMcpOptions.</param>
    /// <returns>The endpoint convention builder for further configuration.</returns>
    public static IEndpointConventionBuilder MapZeroMcp(this IEndpointRouteBuilder app, string? path = null)
    {
        // Get options from DI container
        var options = app.ServiceProvider.GetService<ZeroMcpOptions>() ?? new ZeroMcpOptions();

        // Use provided path or fall back to options
        var effectivePath = path ?? options.McpEndpointPath;

        // Map MCP endpoint
        var builder = app.MapMcp(effectivePath);

        // Conditionally require authentication
        if (options.RequireAuthentication)
        {
            builder.RequireAuthorization();
        }

        return builder;
    }
}
