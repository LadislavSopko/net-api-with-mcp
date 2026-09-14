using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
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

        return services
            .AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssemblyUnwrappingActionResult(options);
    }

    /// <summary>
    /// Scans the assembly for controllers with the SDK [McpServerToolType] attribute and registers methods
    /// with the SDK [McpServerTool] attribute (ModelContextProtocol.Server) as MCP tools, unwrapping ActionResult&lt;T&gt; responses and
    /// performing pre-filter authorization checks.
    /// </summary>
    private static IMcpServerBuilder WithToolsFromAssemblyUnwrappingActionResult(
        this IMcpServerBuilder builder,
        ZeroMcpOptions options)
    {
        var toolAssembly = options.ToolAssembly!;
        var serializerOptions = options.GetEffectiveSerializerOptions();

        // Create authorization metadata store
        var authStore = new ToolAuthorizationStore();
        builder.Services.AddSingleton<IToolAuthorizationStore>(authStore);

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

                // Capture authorization metadata for this tool
                var metadata = ToolAuthorizationMetadata.FromMethod(method, toolName);
                authStore.Register(toolName, metadata);

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
                    var toolNameCopy = toolName; // Capture in closure

                    builder.Services.AddSingleton<McpServerTool>(services =>
                    {
                        var aiFunction = AIFunctionFactory.Create(
                            methodCopy,
                            args => CreateControllerWithPreFilter(args.Services!, toolType, methodCopy, options),
                            new AIFunctionFactoryOptions
                            {
                                Name = toolNameCopy,
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

        // Add tools/list filter if enabled (SDK 2.x: filters are registered through WithRequestFilters)
        if (options.FilterToolsByPermissions)
        {
            builder.WithRequestFilters(filters => filters.AddListToolsFilter(next => async (context, cancellationToken) =>
            {
                var result = await next(context, cancellationToken).ConfigureAwait(false);

                // store must be registered if filtering is enabled
                var store = context.Services?.GetRequiredService<IToolAuthorizationStore>();
                // IUserRoleResolver is optional but if not present it will not filter based on claims
                var roleResolver = context.Services?.GetService<IUserRoleResolver>();

                // Try IUserRoleResolver first (application-provided), fall back to claim-based
                int? userRole = roleResolver is not null && context.User is not null
                    ? await roleResolver.GetUserRoleAsync(context.User).ConfigureAwait(false)
                    : ToolListFilter.GetUserRole(context.User);

                var authorizedToolNames = ToolListFilter.FilterByRole(
                    result.Tools.Select(t => t.Name),
                    userRole,
                    store).ToHashSet(StringComparer.Ordinal);

                result.Tools = result.Tools.Where(t => authorizedToolNames.Contains(t.Name)).ToList();
                return result;
            }));
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
