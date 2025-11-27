namespace McpPoc.Api.Tests;

[Collection("McpApi")]
public class McpToolDiscoveryTests : IAsyncLifetime
{
    private readonly McpApiFixture _fixture;
    private McpClientHelper _mcpClient = null!;

    public McpToolDiscoveryTests(McpApiFixture fixture)
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
    public async Task Should_DiscoverToolsFilteredByRole_WhenListingTools()
    {
        // Act - default user is alice@example.com (Member role)
        var tools = await _mcpClient.ListToolsAsync();

        // Assert - Member sees base tools + create (5 total)
        // Base tools: get_by_id, get_all, get_scope_id, get_public_info
        // Role-protected: create (Member+)
        // Not visible to Member: update (Manager+), promote_to_manager (Admin+)
        tools.Should().NotBeNull();
        tools.Should().HaveCount(5, "Member should see 4 base tools + create");

        // Verify expected tool names (SDK converts to snake_case)
        var toolNames = tools.Select(t => t.Name).ToList();
        toolNames.Should().Contain("get_by_id");
        toolNames.Should().Contain("get_all");
        toolNames.Should().Contain("create");
        toolNames.Should().Contain("get_scope_id");
        toolNames.Should().Contain("get_public_info");

        // Member should NOT see higher-role tools
        toolNames.Should().NotContain("update", "Member cannot see Manager-level tools");
        toolNames.Should().NotContain("promote_to_manager", "Member cannot see Admin-level tools");
    }

    [Fact]
    public async Task Should_DiscoverGetByIdTool_WithCorrectSchema()
    {
        // Act
        var tools = await _mcpClient.ListToolsAsync();

        // Assert - SDK converts method names to snake_case
        var getByIdTool = tools
            .Should().ContainSingle(t => t.Name == "get_by_id")
            .Subject;

        getByIdTool.Description.Should().Contain("Gets a user by their ID");
    }

    [Fact]
    public async Task Should_DiscoverGetAllTool_WithCorrectSchema()
    {
        // Act
        var tools = await _mcpClient.ListToolsAsync();

        // Assert - SDK converts method names to snake_case
        var getAllTool = tools
            .Should().ContainSingle(t => t.Name == "get_all")
            .Subject;

        getAllTool.Description.Should().Contain("Gets all users");
    }

    [Fact]
    public async Task Should_DiscoverCreateTool_WithParameterDescriptions()
    {
        // Act
        var tools = await _mcpClient.ListToolsAsync();

        // Assert - SDK converts method names to snake_case
        var createTool = tools
            .Should().ContainSingle(t => t.Name == "create")
            .Subject;

        createTool.Description.Should().Contain("Creates a new user");
        createTool.JsonSchema.ValueKind.Should().NotBe(System.Text.Json.JsonValueKind.Undefined, "Create tool should have input schema for name and email");
    }

    [Fact]
    public async Task Should_NotExposeDeleteEndpoint_AsAnMcpTool()
    {
        // Act
        var tools = await _mcpClient.ListToolsAsync();

        // Assert
        tools
            .Should().NotContain(t => t.Name.Contains("delete", StringComparison.OrdinalIgnoreCase),
                "Delete endpoint should NOT have [McpServerTool] attribute");
    }
}
