namespace McpPoc.Api.Tests;

[Collection("McpApi")]
public sealed class McpToolDiscoveryTests : IAsyncLifetime
{
    private readonly McpApiFixture _fixture;
    private McpClientHelper _mcpClient = null!;

    public McpToolDiscoveryTests(McpApiFixture fixture)
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
    public async Task Should_DiscoverToolsFilteredByRole_WhenListingTools()
    {
        // Act - default user is alice@example.com (Member role)
        var tools = await _mcpClient.ListToolsAsync();

        // Assert - Member sees base tools + create (6 total)
        // Base tools: UserGetById, get_all, get_scope_id, get_public_info, get_mcp_context, echo_headers
        // Role-protected: create (Member+)
        // Not visible to Member: update (Manager+), promote_to_manager (Admin+)
        tools.Should().NotBeNull();
        tools.Should().HaveCount(7, "Member should see 6 base tools + create");

        // Verify expected tool names (SDK converts to snake_case)
        var toolNames = tools.Select(t => t.Name).ToList();
        toolNames.Should().Contain("UserGetById");
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
            .Should().ContainSingle(t => t.Name == "UserGetById")
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

    [Fact]
    public async Task Should_ReturnTtlAndPrivateScope_WhenConfigured()
    {
        // Arrange - demo Program.cs configures ToolsListTimeToLive = 5 minutes; authorization is on => private scope.
        // Raw JSON-RPC so the wire-level hint names (ttlMs / cacheScope) are asserted, not the SDK client's view.
        var http = await _fixture.GetAuthenticatedClientAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("""{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}""", System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        request.Headers.Add("MCP-Protocol-Version", "2025-11-25");

        // Act
        using var response = await http.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue(body);
        body.Should().Contain("\"ttlMs\":300000");
        body.Should().Contain("\"cacheScope\":\"private\"");
    }

    [Fact]
    public async Task Should_ExposeOutputSchemaOfUser_WhenOutputSchemaTypeIsSet()
    {
        // Arrange - UsersController.GetById carries [McpServerTool(Name = "UserGetById", OutputSchemaType = typeof(User))]
        var tools = await _mcpClient.ListToolsAsync();
        var tool = tools.Should().ContainSingle(t => t.Name == "UserGetById").Subject;

        // Assert - the SDK derives outputSchema from OutputSchemaType (snake_case serializer => "id", "name")
        tool.ProtocolTool.OutputSchema.Should().NotBeNull();
        var properties = tool.ProtocolTool.OutputSchema!.Value.GetProperty("properties");
        properties.TryGetProperty("id", out _).Should().BeTrue();
        properties.TryGetProperty("name", out _).Should().BeTrue();
    }
}
