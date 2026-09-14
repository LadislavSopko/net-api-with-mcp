using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Zero.Mcp.Extensions;

/// <summary>
/// tools/list request filter that stamps the MCP cache hints (<c>ttlMs</c> / <c>cacheScope</c>) on the result
/// when <see cref="ZeroMcpOptions.ToolsListTimeToLive"/> is configured. Registered after the SDK authorization
/// filters so it wraps the already-filtered, per-user list.
/// </summary>
internal static class ToolsListCacheHintFilter
{
    /// <summary>
    /// Wraps <paramref name="next"/> and stamps the cache hints on its result.
    /// </summary>
    public static McpRequestHandler<ListToolsRequestParams, ListToolsResult> Apply(
        McpRequestHandler<ListToolsRequestParams, ListToolsResult> next,
        TimeSpan? ttl,
        bool useAuthorization) =>
        async (context, cancellationToken) =>
        {
            var result = await next(context, cancellationToken).ConfigureAwait(false);
            Stamp(result, ttl, useAuthorization);
            return result;
        };

    /// <summary>
    /// Pure part: sets <see cref="ListToolsResult.TimeToLive"/> and <see cref="ListToolsResult.CacheScope"/>
    /// when <paramref name="ttl"/> is set. The scope is <see cref="CacheScope.Private"/> when authorization is on
    /// (the list varies per user) and <see cref="CacheScope.Public"/> otherwise.
    /// </summary>
    internal static void Stamp(ListToolsResult result, TimeSpan? ttl, bool useAuthorization)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (ttl is { } timeToLive)
        {
            result.TimeToLive = timeToLive;
            result.CacheScope = useAuthorization ? CacheScope.Private : CacheScope.Public;
        }
    }
}
