using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Protocol;
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

        // Register IHttpContextAccessor if not already registered
        services.AddHttpContextAccessor();

        // Register IMcpRequestContext as Scoped
        services.AddScoped<IMcpRequestContext, McpRequestContext>();

        var mcpBuilder = services
            .AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssemblyUnwrappingActionResult(options);

        // Authorization is delegated to the SDK: [Authorize]/[AllowAnonymous] found in the tool metadata are
        // evaluated through the host's IAuthorizationService for both tools/list and tools/call.
        if (options.UseAuthorization)
        {
            mcpBuilder.AddAuthorizationFilters();
        }

        return mcpBuilder;
    }

    /// <summary>
    /// Scans the assembly for controllers with the SDK [McpServerToolType] attribute and registers methods
    /// with the SDK [McpServerTool] attribute (ModelContextProtocol.Server) as MCP tools, unwrapping ActionResult&lt;T&gt; responses.
    /// Authorization metadata is attached to each tool for the SDK authorization filters.
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
                var toolName = ToolNameGenerator.GenerateName(method, toolType, options);

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
                        return McpServerTool.Create(aiFunction,
                            ToolCreateOptionsFactory.Create(method, services, serializerOptions, options.UseAuthorization));
                    });
                }
                else
                {
                    // Instance method - controller resolved through DI per invocation
                    var methodCopy = method; // Capture in closure
                    var toolNameCopy = toolName; // Capture in closure

                    builder.Services.AddSingleton<McpServerTool>(services =>
                    {
                        var aiFunction = AIFunctionFactory.Create(
                            methodCopy,
                            args => ActivatorUtilities.CreateInstance(args.Services!, toolType),
                            new AIFunctionFactoryOptions
                            {
                                Name = toolNameCopy,
                                MarshalResult = async (result, resultType, ct) => await MarshalResult.UnwrapAsync(result),
                                SerializerOptions = serializerOptions
                            });

                        return McpServerTool.Create(aiFunction,
                            ToolCreateOptionsFactory.Create(methodCopy, services, serializerOptions, options.UseAuthorization));
                    });
                }
            }
        }

        return builder;
    }
}

/// <summary>
/// Extension methods for mapping MCP endpoints.
/// </summary>
public static class McpEndpointExtensions
{
    /// <summary>
    /// Maps the MCP endpoint using the configuration from ZeroMcpOptions.
    /// Use with UseZeroMcpMarking() middleware for IMcpRequestContext support.
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

    /// <summary>
    /// Adds middleware that marks MCP requests with HttpContext.Items marker and x-mcp-call header.
    /// Call this BEFORE MapZeroMcp() in the middleware pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="mcpPath">The MCP endpoint path. Default is "/mcp".</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseZeroMcpMarking(this IApplicationBuilder app, string mcpPath = "/mcp")
    {
        return app.Use(async (context, next) =>
        {
            // Check if this request is going to the MCP endpoint
            if (context.Request.Path.StartsWithSegments(mcpPath, StringComparison.OrdinalIgnoreCase))
            {
                // Mark this as an MCP call in Items
                context.Items[McpRequestContext.McpCallMarkerKey] = true;

                // Add x-mcp-call header
                context.Request.Headers[McpRequestContext.McpCallHeaderName] = "true";
            }

            await next();
        });
    }

    /// <summary>
    /// Adds MCP call marker to an endpoint (for testing purposes).
    /// For production, use UseZeroMcpMarking() middleware instead.
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <returns>The endpoint convention builder for chaining.</returns>
    public static IEndpointConventionBuilder AddMcpCallMarker(this IEndpointConventionBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;

            // Mark this as an MCP call in Items
            httpContext.Items[McpRequestContext.McpCallMarkerKey] = true;

            // Add x-mcp-call header (modifying request headers)
            httpContext.Request.Headers[McpRequestContext.McpCallHeaderName] = "true";

            return await next(context);
        });
    }
}
