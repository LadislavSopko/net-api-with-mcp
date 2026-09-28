namespace McpPoc.Api.Tests;

/// <summary>
/// Integration tests for role-based tool visibility filtering.
/// Verifies that tools/list only returns tools the user is authorized to invoke.
/// </summary>
[Collection("McpApi")]
public sealed class ToolVisibilityTests : IAsyncLifetime
{
    private readonly McpApiFixture _fixture;
    private McpClientHelper _viewerClient = null!;
    private McpClientHelper _memberClient = null!;
    private McpClientHelper _managerClient = null!;
    private McpClientHelper _adminClient = null!;

    public ToolVisibilityTests(McpApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        // Reset data to seed state for test isolation
        _fixture.ResetUserStore();

        var viewerHttp = await _fixture.GetAuthenticatedClientAsync("viewer", "viewer123");
        _viewerClient = new McpClientHelper(viewerHttp);

        var memberHttp = await _fixture.GetAuthenticatedClientAsync("alice@example.com", "alice123");
        _memberClient = new McpClientHelper(memberHttp);

        var managerHttp = await _fixture.GetAuthenticatedClientAsync("bob@example.com", "bob123");
        _managerClient = new McpClientHelper(managerHttp);

        var adminHttp = await _fixture.GetAuthenticatedClientAsync("carol@example.com", "carol123");
        _adminClient = new McpClientHelper(adminHttp);
    }

    public async ValueTask DisposeAsync()
    {
        await _viewerClient.DisposeAsync();
        await _memberClient.DisposeAsync();
        await _managerClient.DisposeAsync();
        await _adminClient.DisposeAsync();
    }

    // Base tools visible to all authenticated users (no policy = null minRole)
    private static readonly string[] BaseTools = new[]
    {
        "UserGetById", "get_all", "get_scope_id", "get_public_info", "get_mcp_context", "echo_headers"
    };

    [Fact]
    public async Task Viewer_Should_SeeOnly_BaseTools()
    {
        // Act
        var tools = await _viewerClient.ListToolsAsync();
        var toolNames = tools.Select(t => t.Name).ToArray();

        // Assert - Viewer (role 0) should only see tools with no minimum role requirement
        toolNames.Should().BeEquivalentTo(BaseTools,
            "Viewer should only see base tools without role requirements");
        toolNames.Should().NotContain("admin_reports_summary", "the inherited tool requires Admin");
    }

    [Fact]
    public async Task Member_Should_See_BaseAndCreateTools()
    {
        // Act
        var tools = await _memberClient.ListToolsAsync();
        var toolNames = tools.Select(t => t.Name).ToArray();

        // Assert - Member (role 1) should see base tools + create
        string[] expected = [.. BaseTools, "create"];
        toolNames.Should().BeEquivalentTo(expected,
            "Member should see base tools and create");
        toolNames.Should().NotContain("admin_reports_summary", "the inherited tool requires Admin");
    }

    [Fact]
    public async Task Manager_Should_See_BaseCreateUpdateTools()
    {
        // Act
        var tools = await _managerClient.ListToolsAsync();
        var toolNames = tools.Select(t => t.Name).ToArray();

        // Assert - Manager (role 2) should see base tools + create + update
        string[] expected = [.. BaseTools, "create", "update"];
        toolNames.Should().BeEquivalentTo(expected,
            "Manager should see base tools, create, and update");
        toolNames.Should().NotContain("admin_reports_summary",
            "a Manager satisfies the base [Authorize] but not the derived RequireAdmin policy");
    }

    [Fact]
    public async Task Admin_Should_See_AllTools()
    {
        // Act
        var tools = await _adminClient.ListToolsAsync();
        var toolNames = tools.Select(t => t.Name).ToArray();

        // Assert - Admin (role 3) should see all 10 tools, including admin_reports_summary, which is
        // inherited from ReportsControllerBase but governed by AdminReportsController's RequireAdmin policy
        string[] expected = [.. BaseTools, "create", "update", "promote_to_manager", "admin_reports_summary"];
        toolNames.Should().BeEquivalentTo(expected,
            "Admin should see all tools");
    }
}
