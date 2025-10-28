using System.Text.Json;
using FluentAssertions;
using ModelContextProtocol.Protocol;

namespace McpPoc.Api.Tests;

[Collection("McpApi")]
public class PolicyAuthorizationTests : IAsyncLifetime
{
    private readonly McpApiFixture _fixture;
    private McpClientHelper _memberClient = null!;
    private McpClientHelper _managerClient = null!;
    private McpClientHelper _adminClient = null!;

    public PolicyAuthorizationTests(McpApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Create authenticated clients for each role
        var memberHttp = await _fixture.GetAuthenticatedClientAsync("alice@example.com", "alice123");
        _memberClient = new McpClientHelper(memberHttp);

        var managerHttp = await _fixture.GetAuthenticatedClientAsync("bob@example.com", "bob123");
        _managerClient = new McpClientHelper(managerHttp);

        var adminHttp = await _fixture.GetAuthenticatedClientAsync("carol@example.com", "carol123");
        _adminClient = new McpClientHelper(adminHttp);
    }

    public async Task DisposeAsync()
    {
        await _memberClient.DisposeAsync();
        await _managerClient.DisposeAsync();
        await _adminClient.DisposeAsync();
    }

    [Fact]
    public async Task Should_AllowCreate_WhenUserIsMember()
    {
        // Arrange - Member user calling Member-protected tool
        // MCP SDK expects nested structure: { "request": { "name": "...", "email": "..." } }
        var args = new Dictionary<string, object?>
        {
            ["request"] = new Dictionary<string, object?>
            {
                ["name"] = "Test User",
                ["email"] = "test@example.com"
            }
        };

        // Act
        var result = await _memberClient.CallToolAsync("create", args);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().NotBe(true, "Member should be able to create users");
        result.Content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_AllowUpdate_WhenUserIsManager()
    {
        // Arrange - Manager user calling Manager-protected tool
        // MCP SDK expects: { "id": 1, "request": { "name": "...", "email": "..." } }
        var args = new Dictionary<string, object?>
        {
            ["id"] = 1,
            ["request"] = new Dictionary<string, object?>
            {
                ["name"] = "Updated Name",
                ["email"] = "updated@example.com"
            }
        };

        // Act
        var result = await _managerClient.CallToolAsync("update", args);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().NotBe(true, "Manager should be able to update users");
        result.Content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_BlockUpdate_WhenUserIsMember()
    {
        // Arrange - Member user trying to call Manager-protected tool
        // MCP SDK expects: { "id": 1, "request": { "name": "...", "email": "..." } }
        var args = new Dictionary<string, object?>
        {
            ["id"] = 1,
            ["request"] = new Dictionary<string, object?>
            {
                ["name"] = "Updated Name",
                ["email"] = "updated@example.com"
            }
        };

        // Act
        var result = await _memberClient.CallToolAsync("update", args);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().Be(true, "Member should NOT be able to update users - authorization should block this");
        result.Content.Should().NotBeEmpty("error response should contain error details");
    }

    [Fact]
    public async Task Should_AllowPromote_WhenUserIsAdmin()
    {
        // Arrange - Admin user calling Admin-protected tool
        var args = new Dictionary<string, object?>
        {
            ["id"] = 1
        };

        // Act
        var result = await _adminClient.CallToolAsync("promote_to_manager", args);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().NotBe(true, "Admin should be able to promote users");
        result.Content.Should().NotBeEmpty();

        var textBlock = result.Content.First().Should().BeOfType<TextContentBlock>().Subject;
        var json = JsonSerializer.Deserialize<JsonElement>(textBlock.Text);
        json.GetProperty("role").GetInt32().Should().Be(2, "user should be promoted to Manager (role 2)");
    }

    [Fact]
    public async Task Should_BlockPromote_WhenUserIsManager()
    {
        // Arrange - Manager user trying to call Admin-protected tool
        var args = new Dictionary<string, object?>
        {
            ["id"] = 1
        };

        // Act
        var result = await _managerClient.CallToolAsync("promote_to_manager", args);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().Be(true, "Manager should NOT be able to promote users - authorization should block this");
        result.Content.Should().NotBeEmpty("error response should contain error details");
    }

    [Fact]
    public async Task Should_BlockPromote_WhenUserIsMember()
    {
        // Arrange - Member user trying to call Admin-protected tool
        var args = new Dictionary<string, object?>
        {
            ["id"] = 1
        };

        // Act
        var result = await _memberClient.CallToolAsync("promote_to_manager", args);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().Be(true, "Member should NOT be able to promote users - authorization should block this");
        result.Content.Should().NotBeEmpty("error response should contain error details");
    }

    [Fact]
    public async Task Should_AllowCreate_WhenUserIsManager()
    {
        // Arrange - Manager user calling Member-protected tool (role hierarchy test)
        // MCP SDK expects nested structure: { "request": { "name": "...", "email": "..." } }
        var args = new Dictionary<string, object?>
        {
            ["request"] = new Dictionary<string, object?>
            {
                ["name"] = "Test User By Manager",
                ["email"] = "manager-created@example.com"
            }
        };

        // Act
        var result = await _managerClient.CallToolAsync("create", args);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().NotBe(true, "Manager should inherit Member permissions and be able to create users");
        result.Content.Should().NotBeEmpty();
    }
}
