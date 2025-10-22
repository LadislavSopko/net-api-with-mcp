using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpPoc.Api.Tests;

/// <summary>
/// Helper for making MCP protocol requests
/// </summary>
public class McpClientHelper
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public McpClientHelper(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// List all available MCP tools
    /// </summary>
    public async Task<McpResponse<ToolsListResponse>> ListToolsAsync()
    {
        var request = new McpRequest
        {
            Jsonrpc = "2.0",
            Id = Guid.NewGuid().ToString(),
            Method = "tools/list"
        };

        var response = await SendMcpRequestAsync<ToolsListResponse>(request);
        return response;
    }

    /// <summary>
    /// Call an MCP tool
    /// </summary>
    public async Task<McpResponse<ToolCallResponse>> CallToolAsync(string toolName, Dictionary<string, object>? arguments = null)
    {
        var request = new McpRequest
        {
            Jsonrpc = "2.0",
            Id = Guid.NewGuid().ToString(),
            Method = "tools/call",
            Params = new ToolCallParams
            {
                Name = toolName,
                Arguments = arguments ?? new Dictionary<string, object>()
            }
        };

        var response = await SendMcpRequestAsync<ToolCallResponse>(request);
        return response;
    }

    private async Task<McpResponse<T>> SendMcpRequestAsync<T>(McpRequest request)
    {
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpResponse = await _httpClient.PostAsync("/mcp", content);
        httpResponse.EnsureSuccessStatusCode();

        var responseJson = await httpResponse.Content.ReadAsStringAsync();
        var mcpResponse = JsonSerializer.Deserialize<McpResponse<T>>(responseJson, JsonOptions);

        return mcpResponse ?? throw new InvalidOperationException("Failed to deserialize MCP response");
    }
}

// MCP Protocol DTOs
public record McpRequest
{
    public string Jsonrpc { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
    public object? Params { get; init; }
}

public record McpResponse<T>
{
    public string Jsonrpc { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;
    public T? Result { get; init; }
    public McpError? Error { get; init; }
}

public record McpError
{
    public int Code { get; init; }
    public string Message { get; init; } = string.Empty;
    public object? Data { get; init; }
}

public record ToolsListResponse
{
    public List<ToolInfo> Tools { get; init; } = new();
}

public record ToolInfo
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public JsonElement? InputSchema { get; init; }
}

public record ToolCallParams
{
    public string Name { get; init; } = string.Empty;
    public Dictionary<string, object> Arguments { get; init; } = new();
}

public record ToolCallResponse
{
    public List<ToolContent> Content { get; init; } = new();
    public bool? IsError { get; init; }
}

public record ToolContent
{
    public string Type { get; init; } = string.Empty;
    public string? Text { get; init; }
}
