namespace McpPoc.Api.Tests;

/// <summary>
/// E2E tests for tool naming conventions.
/// Verifies that tools are named correctly based on convention settings.
/// </summary>
[Collection("McpApi")]
public class ToolNamingTests : IAsyncLifetime
{
    private readonly McpApiFixture _fixture;
    private McpClientHelper _mcpClient = null!;

    public ToolNamingTests(McpApiFixture fixture)
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
    public async Task MethodOnly_Convention_ToolsHaveSnakeCaseMethodNames()
    {
        // Act - API uses MethodOnly convention by default
        var tools = await _mcpClient.ListToolsAsync();

        // Assert - tools should have snake_case method names
        var toolNames = tools.Select(t => t.Name).ToList();

        // Verify method names are converted to snake_case (not prefixed with controller)
        toolNames.Should().Contain("UserGetById", "GetById carries an explicit Name that always wins over the convention");
        toolNames.Should().Contain("get_all", "GetAll should become get_all");
        toolNames.Should().Contain("create", "Create should become create");

        // Verify tools are NOT prefixed with controller name
        toolNames.Should().NotContain(n => n.StartsWith("users_"),
            "MethodOnly convention should not prefix with controller name");
    }

    [Fact]
    public async Task MethodOnly_Convention_AsyncSuffixIsStripped()
    {
        // Act
        var tools = await _mcpClient.ListToolsAsync();

        // Assert - Async suffix should be removed
        var toolNames = tools.Select(t => t.Name).ToList();

        // No tool names should end with "_async"
        toolNames.Should().NotContain(n => n.EndsWith("_async"),
            "Async suffix should be stripped from method names");
    }

    [Fact]
    public async Task Tools_HaveDescriptionsFromAttributes()
    {
        // Act
        var tools = await _mcpClient.ListToolsAsync();

        // Assert - tools should have descriptions from [Description] attribute
        var getByIdTool = tools.FirstOrDefault(t => t.Name == "UserGetById");
        getByIdTool.Should().NotBeNull();
        getByIdTool!.Description.Should().NotBeNullOrEmpty("Tools should have descriptions");
        getByIdTool.Description.Should().Contain("user", "Description should be meaningful");
    }

    [Fact]
    public async Task Tools_HaveValidJsonSchemas()
    {
        // Act
        var tools = await _mcpClient.ListToolsAsync();

        // Assert - tools with parameters should have valid JSON schemas
        var createTool = tools.FirstOrDefault(t => t.Name == "create");
        createTool.Should().NotBeNull();
        createTool!.JsonSchema.ValueKind.Should().NotBe(System.Text.Json.JsonValueKind.Undefined,
            "Tools with parameters should have JSON schemas");
    }
}
