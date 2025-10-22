using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace McpPoc.Api.Tests;

/// <summary>
/// Helper for making MCP protocol requests using the official SDK
/// </summary>
public class McpClientHelper : IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private McpClient? _client;

    public McpClientHelper(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Connect to the MCP server
    /// </summary>
    private async Task<McpClient> GetConnectedClientAsync()
    {
        if (_client != null)
        {
            return _client;
        }

        // Create HTTP transport pointing to /mcp endpoint
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(_httpClient.BaseAddress!, "mcp"),
                TransportMode = HttpTransportMode.AutoDetect
            },
            _httpClient,
            ownsHttpClient: false
        );

        // Connect to the server
        _client = await McpClient.CreateAsync(transport);
        return _client;
    }

    /// <summary>
    /// List all available MCP tools
    /// </summary>
    public async Task<IList<McpClientTool>> ListToolsAsync()
    {
        var client = await GetConnectedClientAsync();
        return await client.ListToolsAsync();
    }

    /// <summary>
    /// Call an MCP tool
    /// </summary>
    public async Task<CallToolResult> CallToolAsync(string toolName, IReadOnlyDictionary<string, object?>? arguments = null)
    {
        var client = await GetConnectedClientAsync();
        return await client.CallToolAsync(toolName, arguments);
    }

    public async ValueTask DisposeAsync()
    {
        if (_client != null)
        {
            await _client.DisposeAsync();
        }
    }
}
