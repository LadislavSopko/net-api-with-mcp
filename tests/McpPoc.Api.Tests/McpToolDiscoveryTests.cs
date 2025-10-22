namespace McpPoc.Api.Tests;

[Collection("McpApi")]
public class McpToolDiscoveryTests
{
    private readonly McpApiFixture _fixture;
    private readonly McpClientHelper _mcpClient;

    public McpToolDiscoveryTests(McpApiFixture fixture)
    {
        _fixture = fixture;
        _mcpClient = new McpClientHelper(fixture.HttpClient);
    }

    [Fact]
    public async Task Should_DiscoverThreeMcpTools_WhenListingTools()
    {
        // Act
        var response = await _mcpClient.ListToolsAsync();

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull("MCP request should succeed");
        response.Result.Should().NotBeNull();
        response.Result!.Tools.Should().HaveCount(3, "only GetById, GetAll, and Create should be exposed");
    }

    [Fact]
    public async Task Should_DiscoverGetByIdTool_WithCorrectSchema()
    {
        // Act
        var response = await _mcpClient.ListToolsAsync();

        // Assert
        var getByIdTool = response.Result!.Tools
            .Should().ContainSingle(t => t.Name.Contains("GetById", StringComparison.OrdinalIgnoreCase))
            .Subject;

        getByIdTool.Description.Should().Contain("Gets a user by their ID");
    }

    [Fact]
    public async Task Should_DiscoverGetAllTool_WithCorrectSchema()
    {
        // Act
        var response = await _mcpClient.ListToolsAsync();

        // Assert
        var getAllTool = response.Result!.Tools
            .Should().ContainSingle(t => t.Name.Contains("GetAll", StringComparison.OrdinalIgnoreCase))
            .Subject;

        getAllTool.Description.Should().Contain("Gets all users");
    }

    [Fact]
    public async Task Should_DiscoverCreateTool_WithParameterDescriptions()
    {
        // Act
        var response = await _mcpClient.ListToolsAsync();

        // Assert
        var createTool = response.Result!.Tools
            .Should().ContainSingle(t => t.Name.Contains("Create", StringComparison.OrdinalIgnoreCase))
            .Subject;

        createTool.Description.Should().Contain("Creates a new user");
        createTool.InputSchema.Should().NotBeNull("Create tool should have input schema for name and email");
    }

    [Fact]
    public async Task Should_NotExposeDeleteEndpoint_AsAnMcpTool()
    {
        // Act
        var response = await _mcpClient.ListToolsAsync();

        // Assert
        response.Result!.Tools
            .Should().NotContain(t => t.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase),
                "Delete endpoint should NOT have [McpServerTool] attribute");
    }
}
