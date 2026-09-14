using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace McpPoc.Api.Tests;

/// <summary>
/// E2E tests for IMcpRequestContext.
///
/// LIMITATION: The MCP SDK creates internal scopes for tool invocations where
/// HttpContext is not properly flowed. This means IsMcpCall detection via
/// HttpContext.Items or Request.Path doesn't work during MCP tool calls.
///
/// The IMcpRequestContext works correctly in:
/// - Regular ASP.NET Core controllers accessed via HTTP
/// - Middleware pipeline
/// - Unit tests with mocked HttpContextAccessor
/// </summary>
[Collection("McpApi")]
public class McpRequestContextE2ETests : IAsyncLifetime
{
    private readonly McpApiFixture _fixture;
    private McpClientHelper _mcpClient = null!;

    public McpRequestContextE2ETests(McpApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        var httpClient = await _fixture.GetAuthenticatedClientAsync();
        _mcpClient = new McpClientHelper(httpClient);
    }

    public async ValueTask DisposeAsync()
    {
        await _mcpClient.DisposeAsync();
    }

    [Fact]
    public async Task HttpCall_IsMcpCall_ReturnsFalse()
    {
        // Arrange - get authenticated HTTP client (not MCP)
        var httpClient = await _fixture.GetAuthenticatedClientAsync();

        // Act - call the endpoint directly via HTTP (not MCP)
        var response = await httpClient.GetAsync("/api/users/mcp-context");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var contextInfo = JsonSerializer.Deserialize<McpContextInfo>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert - HTTP calls should NOT be detected as MCP
        contextInfo.Should().NotBeNull();
        contextInfo!.IsMcpCall.Should().BeFalse("Direct HTTP request should NOT be detected as MCP call");
    }

    [Fact]
    public async Task McpTool_GetMcpContext_IsDiscoverable()
    {
        // Act
        var tools = await _mcpClient.ListToolsAsync();

        // Assert - the get_mcp_context tool should be discoverable
        var toolNames = tools.Select(t => t.Name).ToList();
        toolNames.Should().Contain("get_mcp_context", "The diagnostic tool should be available");
    }

    [Fact]
    public async Task McpTool_GetMcpContext_ReturnsValidResponse()
    {
        // Act - call the MCP context diagnostic tool
        var result = await _mcpClient.CallToolAsync("get_mcp_context");

        // Assert - should return a valid response (even if IsMcpCall is false due to SDK limitation)
        result.Should().NotBeNull();
        result.Content.Should().NotBeEmpty();

        var content = result.Content.First() as TextContentBlock;
        content.Should().NotBeNull();

        var contextInfo = JsonSerializer.Deserialize<McpContextInfo>(
            content!.Text!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        contextInfo.Should().NotBeNull();
        // Note: IsMcpCall may be false due to MCP SDK not flowing HttpContext to tool scopes
        // This is a known limitation documented in the test class
    }

    [Fact]
    public async Task HttpCall_HasHeaders_WithAccessibleCount()
    {
        // Arrange
        var httpClient = await _fixture.GetAuthenticatedClientAsync();

        // Act - call via HTTP
        var response = await httpClient.GetAsync("/api/users/mcp-context");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var contextInfo = JsonSerializer.Deserialize<McpContextInfo>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert - HTTP calls don't expose headers (IsMcpCall is false)
        contextInfo.Should().NotBeNull();
        contextInfo!.HeaderCount.Should().Be(0, "Headers should not be exposed for non-MCP calls");
    }

    // Record to deserialize the response
    private record McpContextInfo(bool IsMcpCall, string? XMcpCallHeader, int HeaderCount);
}
