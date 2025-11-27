using FluentAssertions;
using Xunit;

namespace McpPoc.Api.Tests;

/// <summary>
/// Integration tests for role-based tool visibility filtering.
/// Verifies that tools/list only returns tools the user is authorized to invoke.
/// </summary>
[Collection("McpApi")]
public class ToolVisibilityTests : IAsyncLifetime
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

    public async Task InitializeAsync()
    {
        var viewerHttp = await _fixture.GetAuthenticatedClientAsync("viewer", "viewer123");
        _viewerClient = new McpClientHelper(viewerHttp);

        var memberHttp = await _fixture.GetAuthenticatedClientAsync("alice@example.com", "alice123");
        _memberClient = new McpClientHelper(memberHttp);

        var managerHttp = await _fixture.GetAuthenticatedClientAsync("bob@example.com", "bob123");
        _managerClient = new McpClientHelper(managerHttp);

        var adminHttp = await _fixture.GetAuthenticatedClientAsync("carol@example.com", "carol123");
        _adminClient = new McpClientHelper(adminHttp);
    }

    public async Task DisposeAsync()
    {
        await _viewerClient.DisposeAsync();
        await _memberClient.DisposeAsync();
        await _managerClient.DisposeAsync();
        await _adminClient.DisposeAsync();
    }

    // Base tools visible to all authenticated users (no policy = null minRole)
    private static readonly string[] BaseTools = new[]
    {
        "get_by_id", "get_all", "get_scope_id", "get_public_info"
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
    }

    [Fact]
    public async Task Member_Should_See_BaseAndCreateTools()
    {
        // Act
        var tools = await _memberClient.ListToolsAsync();
        var toolNames = tools.Select(t => t.Name).ToArray();

        // Assert - Member (role 1) should see base tools + create
        var expected = BaseTools.Concat(new[] { "create" }).ToArray();
        toolNames.Should().BeEquivalentTo(expected,
            "Member should see base tools and create");
    }

    [Fact]
    public async Task Manager_Should_See_BaseCreateUpdateTools()
    {
        // Act
        var tools = await _managerClient.ListToolsAsync();
        var toolNames = tools.Select(t => t.Name).ToArray();

        // Assert - Manager (role 2) should see base tools + create + update
        var expected = BaseTools.Concat(new[] { "create", "update" }).ToArray();
        toolNames.Should().BeEquivalentTo(expected,
            "Manager should see base tools, create, and update");
    }

    [Fact]
    public async Task Admin_Should_See_AllTools()
    {
        // Act
        var tools = await _adminClient.ListToolsAsync();
        var toolNames = tools.Select(t => t.Name).ToArray();

        // Assert - Admin (role 3) should see all 7 tools
        var expected = BaseTools.Concat(new[] { "create", "update", "promote_to_manager" }).ToArray();
        toolNames.Should().BeEquivalentTo(expected,
            "Admin should see all tools");
    }
}
