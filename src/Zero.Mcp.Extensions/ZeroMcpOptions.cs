using System.Reflection;
using System.Text.Json;
using ModelContextProtocol.AspNetCore;

namespace Zero.Mcp.Extensions;

/// <summary>
/// Configuration options for Zero.Mcp.Extensions.
/// </summary>
public class ZeroMcpOptions
{
    /// <summary>
    /// Whether to require authentication for MCP endpoints. Default is true.
    /// </summary>
    public bool RequireAuthentication { get; set; } = true;

    /// <summary>
    /// When true, [Authorize]/[AllowAnonymous] on controllers are enforced by the MCP SDK authorization filters
    /// (tools/list is filtered per user and unauthorized tools/call is rejected); requires the host to call
    /// AddAuthorization(). When false, no authorization metadata is attached to tools and the filters are not
    /// registered, so every tool is listed and callable. Default is true.
    /// </summary>
    public bool UseAuthorization { get; set; } = true;

    /// <summary>
    /// The path where the MCP endpoint will be mapped. Default is "/mcp".
    /// </summary>
    public string McpEndpointPath { get; set; } = "/mcp";

    /// <summary>
    /// The assembly to scan for MCP tools. If null, uses the calling assembly.
    /// </summary>
    public Assembly? ToolAssembly { get; set; }

    /// <summary>
    /// JSON serializer options for MCP tool parameters and results.
    /// If null, uses default snake_case_lower naming policy.
    /// </summary>
    public JsonSerializerOptions? SerializerOptions { get; set; }

    /// <summary>
    /// Streamable HTTP session mode passed to the MCP SDK transport. Default is Stateless (the SDK 2.x default
    /// for the 2026-07-28 protocol revision): no Mcp-Session-Id, every request self-contained, load-balancer friendly.
    /// </summary>
    public HttpServerSessionMode SessionMode { get; set; } = HttpServerSessionMode.Stateless;

    /// <summary>
    /// When set, tools/list responses carry the MCP cache hints: ttlMs = this value and cacheScope = private when
    /// UseAuthorization is true (the list varies per user) or public otherwise. Default is null (no hints).
    /// </summary>
    public TimeSpan? ToolsListTimeToLive { get; set; }

    /// <summary>
    /// Naming convention for generated MCP tool names.
    /// Default is MethodOnly for backward compatibility.
    /// </summary>
    public ToolNamingConvention NamingConvention { get; set; } = ToolNamingConvention.MethodOnly;

    /// <summary>
    /// Separator used between controller prefix and method name when using ControllerPrefix convention.
    /// Default is "_" (underscore).
    /// </summary>
    public string ToolNameSeparator { get; set; } = "_";

    /// <summary>
    /// Gets the effective serializer options (returns provided options or default).
    /// </summary>
    internal JsonSerializerOptions GetEffectiveSerializerOptions()
    {
        return SerializerOptions ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        };
    }
}
