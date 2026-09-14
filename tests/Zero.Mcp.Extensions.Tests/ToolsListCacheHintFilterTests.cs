using System.IO.Pipelines;
using AwesomeAssertions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

/// <summary>
/// ToolsListCacheHintFilter stamps TimeToLive + CacheScope on tools/list results when ToolsListTimeToLive is configured.
/// </summary>
public class ToolsListCacheHintFilterTests
{
    private static ListToolsResult NewResult() => new() { Tools = [new Tool { Name = "a" }] };

    [Fact]
    public void Should_LeaveResultUntouched_WhenTimeToLiveIsNull()
    {
        var result = NewResult();

        ToolsListCacheHintFilter.Stamp(result, ttl: null, useAuthorization: true);

        result.TimeToLive.Should().BeNull();
        result.CacheScope.Should().BeNull();
    }

    [Fact]
    public void Should_SetTimeToLive_WhenConfigured()
    {
        var result = NewResult();

        ToolsListCacheHintFilter.Stamp(result, TimeSpan.FromMinutes(5), useAuthorization: true);

        result.TimeToLive.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Should_SetCacheScopePrivate_WhenUseAuthorizationIsTrue()
    {
        var result = NewResult();

        ToolsListCacheHintFilter.Stamp(result, TimeSpan.FromMinutes(5), useAuthorization: true);

        result.CacheScope.Should().Be(CacheScope.Private, "the list varies per user when authorization is on");
    }

    [Fact]
    public void Should_SetCacheScopePublic_WhenUseAuthorizationIsFalse()
    {
        var result = NewResult();

        ToolsListCacheHintFilter.Stamp(result, TimeSpan.FromMinutes(5), useAuthorization: false);

        result.CacheScope.Should().Be(CacheScope.Public);
    }

    [Fact]
    public async Task Should_CallNext_ExactlyOnce()
    {
        var calls = 0;
        var inner = NewResult();
        McpRequestHandler<ListToolsRequestParams, ListToolsResult> next = (_, _) =>
        {
            calls++;
            return ValueTask.FromResult(inner);
        };
        var pipeIn = new Pipe();
        var pipeOut = new Pipe();
        await using var server = McpServer.Create(
            new StreamServerTransport(pipeIn.Reader.AsStream(), pipeOut.Writer.AsStream()),
            new McpServerOptions());
        var context = new RequestContext<ListToolsRequestParams>(server, new JsonRpcRequest { Method = "tools/list" });

        var handler = ToolsListCacheHintFilter.Apply(next, TimeSpan.FromSeconds(30), useAuthorization: false);
        var result = await handler(context, TestContext.Current.CancellationToken);

        calls.Should().Be(1);
        result.Should().BeSameAs(inner);
        result.Tools.Should().ContainSingle(t => t.Name == "a");
        result.TimeToLive.Should().Be(TimeSpan.FromSeconds(30));
        result.CacheScope.Should().Be(CacheScope.Public);
    }
}
