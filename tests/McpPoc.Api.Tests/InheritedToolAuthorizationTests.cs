using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace McpPoc.Api.Tests;

/// <summary>
/// Regression tests for GitHub issue #1: a tool method INHERITED from a base controller and not
/// overridden must be governed by the class-level policy of the DERIVED controller that is scanned.
/// AdminReportsController carries [Authorize(RequireAdmin)] while the base only carries [Authorize],
/// so everyone below Admin must neither see nor be able to call admin_reports_summary.
/// </summary>
[Collection("McpApi")]
public sealed class InheritedToolAuthorizationTests : IAsyncLifetime
{
    private const string InheritedAdminTool = "admin_reports_summary";

    private readonly McpApiFixture _fixture;
    private McpClientHelper _viewerClient = null!;
    private McpClientHelper _managerClient = null!;
    private McpClientHelper _adminClient = null!;

    public InheritedToolAuthorizationTests(McpApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        var viewerHttp = await _fixture.GetAuthenticatedClientAsync("viewer", "viewer123");
        _viewerClient = new McpClientHelper(viewerHttp);

        var managerHttp = await _fixture.GetAuthenticatedClientAsync("bob@example.com", "bob123");
        _managerClient = new McpClientHelper(managerHttp);

        var adminHttp = await _fixture.GetAuthenticatedClientAsync("carol@example.com", "carol123");
        _adminClient = new McpClientHelper(adminHttp);
    }

    public async ValueTask DisposeAsync()
    {
        await _viewerClient.DisposeAsync();
        await _managerClient.DisposeAsync();
        await _adminClient.DisposeAsync();
    }

    [Fact]
    public async Task Should_HideInheritedAdminTool_WhenUserIsViewer()
    {
        var tools = await _viewerClient.ListToolsAsync();

        tools.Select(t => t.Name).Should().NotContain(InheritedAdminTool,
            "the derived controller requires Admin even though the method is declared on the base");
    }

    [Fact]
    public async Task Should_HideInheritedAdminTool_WhenUserIsManager()
    {
        var tools = await _managerClient.ListToolsAsync();

        tools.Select(t => t.Name).Should().NotContain(InheritedAdminTool,
            "a Manager satisfies the base [Authorize] but not the derived RequireAdmin policy");
    }

    [Fact]
    public async Task Should_DenyInheritedAdminTool_WhenManagerCallsIt()
    {
        Func<Task> act = () => _managerClient.CallToolAsync(InheritedAdminTool);

        await act.Should().ThrowAsync<McpProtocolException>("the SDK filter rejects the call before the tool runs")
            .WithMessage("*Access forbidden*");
    }

    [Fact]
    public async Task Should_ExposeAndRunInheritedAdminTool_WhenUserIsAdmin()
    {
        var tools = await _adminClient.ListToolsAsync();
        tools.Select(t => t.Name).Should().Contain(InheritedAdminTool);

        var result = await _adminClient.CallToolAsync(InheritedAdminTool);

        result.IsError.Should().NotBe(true);
        var text = result.Content.First().Should().BeOfType<TextContentBlock>().Subject.Text!;
        text.Should().Contain("status");
    }
}
