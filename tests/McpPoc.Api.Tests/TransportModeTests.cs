using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

namespace McpPoc.Api.Tests;

/// <summary>
/// Locks in the Streamable HTTP session behaviour under SDK 2.2.0: stateless by default (no Mcp-Session-Id),
/// stateful on demand through ZeroMcpOptions.SessionMode (demo config key Mcp:SessionMode).
/// </summary>
[Collection("McpApi")]
public sealed class TransportModeTests(McpApiFixture fixture)
{
    private const string ProtocolVersionHeader = "MCP-Protocol-Version";

    [Fact]
    public async Task Should_AnswerToolsCall_WithoutSessionHeader_WhenStateless()
    {
        // Arrange - the SDK client speaks the 2026-07-28 flow (no initialize handshake, no session);
        // a recording handler captures every HTTP exchange so the absence of Mcp-Session-Id can be asserted.
        var token = (await fixture.GetAuthenticatedClientAsync()).DefaultRequestHeaders.Authorization;
        var recorder = new RecordingHandler();
        using var http = fixture.CreateDefaultClient(new Uri("http://127.0.0.1"), recorder);
        http.DefaultRequestHeaders.Authorization = token;
        await using var mcp = new McpClientHelper(http);

        // Act
        var result = await mcp.CallToolAsync("get_public_info");

        // Assert
        result.IsError.Should().NotBe(true);
        recorder.Exchanges.Should().NotBeEmpty();
        recorder.Exchanges.Should().OnlyContain(e => !e.RequestHadSessionId, "stateless clients never send Mcp-Session-Id");
        recorder.Exchanges.Should().OnlyContain(e => !e.ResponseHadSessionId, "stateless servers never issue Mcp-Session-Id");
    }

    [Fact]
    public async Task Should_ReturnSessionId_WhenSessionModeIsStateful()
    {
        // Arrange - same app, SessionMode switched to Stateful via configuration; legacy-protocol client initializes
        var token = (await fixture.GetAuthenticatedClientAsync()).DefaultRequestHeaders.Authorization;
        using var statefulFactory = fixture.WithWebHostBuilder(b => b.UseSetting("Mcp:SessionMode", "Stateful"));
        using var http = statefulFactory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://127.0.0.1") });
        http.DefaultRequestHeaders.Authorization = token;
        using var request = NewJsonRpc("2025-11-25", """
            {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-11-25",
             "capabilities":{},"clientInfo":{"name":"transport-mode-test","version":"1.0"}}}
            """);

        // Act
        using var response = await http.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue(body);
        response.Headers.TryGetValues("Mcp-Session-Id", out var values).Should().BeTrue("stateful mode issues a session id on initialize");
        values!.Single().Should().NotBeNullOrWhiteSpace();
    }

    private sealed record Exchange(bool RequestHadSessionId, bool ResponseHadSessionId);

    private sealed class RecordingHandler : DelegatingHandler
    {
        public List<Exchange> Exchanges { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            Exchanges.Add(new Exchange(request.Headers.Contains("Mcp-Session-Id"), response.Headers.Contains("Mcp-Session-Id")));
            return response;
        }
    }

    private static HttpRequestMessage NewJsonRpc(string protocolVersion, string json)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        request.Headers.Add(ProtocolVersionHeader, protocolVersion);
        return request;
    }
}
