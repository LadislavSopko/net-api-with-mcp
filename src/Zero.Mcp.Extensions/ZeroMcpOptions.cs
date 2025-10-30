using System.Reflection;
using System.Text.Json;

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
    /// Whether to use authorization policies (requires IAuthForMcpSupplier). Default is true.
    /// When false, [Authorize] attributes are ignored.
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
