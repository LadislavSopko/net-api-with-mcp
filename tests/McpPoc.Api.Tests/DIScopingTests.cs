using System.Text.Json;
using FluentAssertions;

namespace McpPoc.Api.Tests;

[Collection("McpApi")]
public class DIScopingTests : IAsyncLifetime
{
    private readonly McpApiFixture _fixture;
    private McpClientHelper _mcpClient = null!;

    public DIScopingTests(McpApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var httpClient = await _fixture.GetAuthenticatedClientAsync();
        _mcpClient = new McpClientHelper(httpClient);
    }

    public async Task DisposeAsync()
    {
        await _mcpClient.DisposeAsync();
    }

    [Fact]
    public async Task Should_CreateNewScope_PerToolInvocation()
    {
        // This is the CRITICAL test for DI scoping behavior
        // If scoping works correctly: each call gets different RequestId
        // If scoping is broken: same RequestId returned (shared scope)

        // Act - Call the scope test tool twice
        var result1 = await _mcpClient.CallToolAsync("get_scope_id");
        var result2 = await _mcpClient.CallToolAsync("get_scope_id");

        // Assert - Both calls should succeed
        result1.Should().NotBeNull();
        result1.IsError.Should().NotBe(true, "first tool call should succeed");
        result1.Content.Should().NotBeEmpty();

        result2.Should().NotBeNull();
        result2.IsError.Should().NotBe(true, "second tool call should succeed");
        result2.Content.Should().NotBeEmpty();

        // Extract RequestIds from responses
        var textBlock1 = result1.Content.First().Should().BeOfType<ModelContextProtocol.Protocol.TextContentBlock>().Subject;
        var textBlock2 = result2.Content.First().Should().BeOfType<ModelContextProtocol.Protocol.TextContentBlock>().Subject;

        var json1 = JsonSerializer.Deserialize<JsonElement>(textBlock1.Text);
        var json2 = JsonSerializer.Deserialize<JsonElement>(textBlock2.Text);

        var requestId1 = json1.GetProperty("request_id").GetString();
        var requestId2 = json2.GetProperty("request_id").GetString();

        // CRITICAL ASSERTION: RequestIds must be DIFFERENT
        requestId1.Should().NotBeNullOrEmpty("first call should return a request ID");
        requestId2.Should().NotBeNullOrEmpty("second call should return a request ID");
        requestId1.Should().NotBe(requestId2,
            "each MCP tool invocation should create a NEW scope with DIFFERENT RequestId. " +
            "If this fails, DI scoping is broken and EF Core DbContext will have tracking issues!");
    }

    [Fact]
    public async Task Should_VerifyScopedService_IsInjected()
    {
        // Verify the scoped service can be injected at all

        // Act
        var result = await _mcpClient.CallToolAsync("get_scope_id");

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().NotBe(true, "tool should execute successfully");
        result.Content.Should().NotBeEmpty();

        var textBlock = result.Content.First().Should().BeOfType<ModelContextProtocol.Protocol.TextContentBlock>().Subject;
        var json = JsonSerializer.Deserialize<JsonElement>(textBlock.Text);

        json.TryGetProperty("request_id", out var requestIdProperty).Should().BeTrue("response should contain request_id");
        requestIdProperty.GetString().Should().NotBeNullOrEmpty("request_id should have a value");
    }

    [Fact]
    public async Task Should_VerifyMultipleCalls_ProduceDifferentTimestamps()
    {
        // Additional verification: CreatedAt timestamps should also be different
        // This proves instances are created at different times

        // Act - Call with small delay to ensure different timestamps
        var result1 = await _mcpClient.CallToolAsync("get_scope_id");
        await Task.Delay(10); // Small delay to ensure different timestamps
        var result2 = await _mcpClient.CallToolAsync("get_scope_id");

        // Assert
        var textBlock1 = result1.Content.First().Should().BeOfType<ModelContextProtocol.Protocol.TextContentBlock>().Subject;
        var textBlock2 = result2.Content.First().Should().BeOfType<ModelContextProtocol.Protocol.TextContentBlock>().Subject;

        var json1 = JsonSerializer.Deserialize<JsonElement>(textBlock1.Text);
        var json2 = JsonSerializer.Deserialize<JsonElement>(textBlock2.Text);

        var createdAt1 = DateTime.Parse(json1.GetProperty("created_at").GetString()!);
        var createdAt2 = DateTime.Parse(json2.GetProperty("created_at").GetString()!);

        createdAt2.Should().BeAfter(createdAt1,
            "second instance should be created after the first, proving they are separate instances");
    }
}
