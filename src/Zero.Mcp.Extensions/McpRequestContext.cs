using Microsoft.AspNetCore.Http;

namespace Zero.Mcp.Extensions;

/// <summary>
/// Default implementation of IMcpRequestContext.
/// Uses HttpContext.Items to detect MCP calls.
/// </summary>
public class McpRequestContext : IMcpRequestContext
{
    /// <summary>
    /// Key used in HttpContext.Items to mark MCP calls.
    /// </summary>
    public const string McpCallMarkerKey = "__McpCall";

    /// <summary>
    /// Header name automatically added to MCP requests.
    /// </summary>
    public const string McpCallHeaderName = "x-mcp-call";

    private readonly IHttpContextAccessor _accessor;

    /// <summary>
    /// Creates a new instance of McpRequestContext.
    /// </summary>
    /// <param name="accessor">The HTTP context accessor.</param>
    public McpRequestContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    /// <inheritdoc />
    public bool IsMcpCall
    {
        get
        {
            var context = _accessor.HttpContext;
            if (context == null)
                return false;

            // Check for marker in Items (set by middleware)
            if (context.Items.ContainsKey(McpCallMarkerKey))
                return true;

            // Fallback: check if request path is MCP endpoint
            // This handles cases where middleware couldn't set the marker
            var path = context.Request.Path.Value;
            return path != null && path.StartsWith("/mcp", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <inheritdoc />
    public string? GetHeader(string name)
    {
        if (!IsMcpCall)
            return null;

        var header = _accessor.HttpContext?.Request.Headers[name].FirstOrDefault();

        // If looking for x-mcp-call and it's an MCP call, return "true" even if header wasn't set
        if (header == null && name == McpCallHeaderName && IsMcpCall)
            return "true";

        return header;
    }

    /// <inheritdoc />
    public IHeaderDictionary? Headers =>
        IsMcpCall ? _accessor.HttpContext?.Request.Headers : null;
}
