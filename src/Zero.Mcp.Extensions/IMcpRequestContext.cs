using Microsoft.AspNetCore.Http;

namespace Zero.Mcp.Extensions;

/// <summary>
/// Provides access to MCP request context information.
/// Allows controllers to detect if the request came via MCP and access headers.
/// </summary>
public interface IMcpRequestContext
{
    /// <summary>
    /// Gets whether the current request is an MCP call.
    /// </summary>
    bool IsMcpCall { get; }

    /// <summary>
    /// Gets a header value from the MCP request.
    /// Returns null if not an MCP call or header doesn't exist.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <returns>The header value, or null.</returns>
    string? GetHeader(string name);

    /// <summary>
    /// Gets all headers from the MCP request.
    /// Returns null if not an MCP call.
    /// </summary>
    IHeaderDictionary? Headers { get; }
}
