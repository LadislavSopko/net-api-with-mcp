using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace McpPoc.Api.Tests;

/// <summary>
/// E2E tests for IMcpRequestContext.
///
/// Under MCP SDK 2.2.0 stateless Streamable HTTP every tools/call runs inside its own HTTP request with the
/// request's execution context and RequestServices, so IHttpContextAccessor sees the marker set by
/// UseZeroMcpMarking and IsMcpCall is true inside tools. (Earlier versions of this file documented the
/// opposite; that was a test-side snake_case deserialization mistake, not an SDK limitation.)
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

        // Assert - should return a valid response
        result.Should().NotBeNull();
        result.Content.Should().NotBeEmpty();

        var content = result.Content.First() as TextContentBlock;
        content.Should().NotBeNull();

        var contextInfo = JsonSerializer.Deserialize<McpContextInfo>(
            content!.Text!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        contextInfo.Should().NotBeNull();
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

    [Fact]
    public async Task Should_ReportIsMcpCallTrue_WhenInvokedOverStatelessTransport()
    {
        // SDK 2.2.0 stateless Streamable HTTP: every tools/call runs inside its own HTTP request, so
        // IHttpContextAccessor sees the marker set by UseZeroMcpMarking and the x-mcp-call header.
        var result = await _mcpClient.CallToolAsync("get_mcp_context");

        result.IsError.Should().NotBe(true);
        var content = result.Content.First().Should().BeOfType<TextContentBlock>().Subject;
        // The demo serializes tool payloads in snake_case (is_mcp_call); case-insensitivity alone does not match it.
        var contextInfo = JsonSerializer.Deserialize<McpContextInfo>(
            content.Text!,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

        contextInfo.Should().NotBeNull();
        contextInfo!.IsMcpCall.Should().BeTrue("the tool runs in the HTTP request marked by UseZeroMcpMarking");
        contextInfo.XMcpCallHeader.Should().Be("true");
    }
}
