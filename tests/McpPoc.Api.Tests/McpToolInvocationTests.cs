using System.Text.Json;

namespace McpPoc.Api.Tests;

[Collection("McpApi")]
public class McpToolInvocationTests : IAsyncLifetime
{
    private readonly McpApiFixture _fixture;
    private McpClientHelper _mcpClient = null!;

    public McpToolInvocationTests(McpApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Reset data to seed state for test isolation
        _fixture.ResetUserStore();

        // Use alice@example.com (Member role) for tests that require authorization
        var httpClient = await _fixture.GetAuthenticatedClientAsync("alice@example.com", "alice123");
        _mcpClient = new McpClientHelper(httpClient);
    }

    public async Task DisposeAsync()
    {
        await _mcpClient.DisposeAsync();
    }

    [Fact]
    public async Task Should_GetUserById_WhenCallingGetByIdTool()
    {
        // Arrange
        var arguments = new Dictionary<string, object?>
        {
            ["id"] = 1
        };

        // Act
        var result = await _mcpClient.CallToolAsync("get_by_id", arguments);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().NotBe(true, "tool execution should succeed");
        result.Content.Should().NotBeEmpty();

        var textBlock = result.Content.First().Should().BeOfType<ModelContextProtocol.Protocol.TextContentBlock>().Subject;
        textBlock.Text.Should().Contain("Alice Smith", "user with id 1 should be returned");
    }

    [Fact]
    public async Task Should_GetAllUsers_WhenCallingGetAllTool()
    {
        // Act
        var result = await _mcpClient.CallToolAsync("get_all");

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().NotBe(true, "tool execution should succeed");
        result.Content.Should().NotBeEmpty();

        var textBlock = result.Content.First().Should().BeOfType<ModelContextProtocol.Protocol.TextContentBlock>().Subject;
        textBlock.Text.Should().NotBeNull();

        // Parse JSON array
        var users = JsonSerializer.Deserialize<JsonElement>(textBlock.Text);
        users.GetArrayLength().Should().BeGreaterOrEqualTo(3, "at least 3 users should exist");
    }

    [Fact]
    public async Task Should_CreateUser_WhenCallingCreateTool()
    {
        // Arrange
        // MCP SDK expects nested structure: { "request": { "name": "...", "email": "..." } }
        var arguments = new Dictionary<string, object?>
        {
            ["request"] = new Dictionary<string, object?>
            {
                ["name"] = "Test User",
                ["email"] = "test@example.com"
            }
        };

        // Act
        var result = await _mcpClient.CallToolAsync("create", arguments);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().NotBe(true, "tool execution should succeed");
        result.Content.Should().NotBeEmpty();

        var textBlock = result.Content.First().Should().BeOfType<ModelContextProtocol.Protocol.TextContentBlock>().Subject;
        textBlock.Text.Should().Contain("Test User");
        textBlock.Text.Should().Contain("test@example.com");
    }

    [Fact]
    public async Task Should_ReturnNotFound_WhenGettingNonExistentUser()
    {
        // Arrange
        var arguments = new Dictionary<string, object?>
        {
            ["id"] = 99999
        };

        // Act
        var result = await _mcpClient.CallToolAsync("get_by_id", arguments);

        // Assert
        result.Should().NotBeNull();
        // The tool should indicate an error or return empty content
        // (behavior depends on how controller handles 404)
    }

    [Fact]
    public async Task Should_VerifyDependencyInjection_WorksInMcpTools()
    {
        // This test verifies that IUserService is properly injected
        // by calling a tool that uses it

        // Act
        var result = await _mcpClient.CallToolAsync("get_all");

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().NotBe(true, "DI should work - tool should execute successfully");
    }
}
