using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

public class McpMiddlewareTests
{
    [Fact]
    public async Task McpEndpoint_SetsItemsMarker()
    {
        // Arrange
        bool markerWasSet = false;

        using var host = await CreateTestHost(async context =>
        {
            markerWasSet = context.Items.ContainsKey(McpRequestContext.McpCallMarkerKey);
            await context.Response.WriteAsync("OK");
        }, useMcpMarking: true);

        var client = host.GetTestClient();

        // Act
        var response = await client.PostAsync("/mcp", null);

        // Assert
        markerWasSet.Should().BeTrue("MCP endpoint should set the marker in Items");
    }

    [Fact]
    public async Task McpEndpoint_AddsXMcpCallHeader()
    {
        // Arrange
        string? headerValue = null;

        using var host = await CreateTestHost(async context =>
        {
            headerValue = context.Request.Headers[McpRequestContext.McpCallHeaderName].FirstOrDefault();
            await context.Response.WriteAsync("OK");
        }, useMcpMarking: true);

        var client = host.GetTestClient();

        // Act
        var response = await client.PostAsync("/mcp", null);

        // Assert
        headerValue.Should().Be("true", "MCP endpoint should add x-mcp-call header");
    }

    [Fact]
    public async Task NonMcpEndpoint_NoMarkerOrHeader()
    {
        // Arrange
        bool markerWasSet = false;
        string? headerValue = null;

        using var host = await CreateTestHost(async context =>
        {
            markerWasSet = context.Items.ContainsKey(McpRequestContext.McpCallMarkerKey);
            headerValue = context.Request.Headers[McpRequestContext.McpCallHeaderName].FirstOrDefault();
            await context.Response.WriteAsync("OK");
        }, useMcpMarking: true);

        var client = host.GetTestClient();

        // Act - call a different endpoint
        var response = await client.GetAsync("/other");

        // Assert
        markerWasSet.Should().BeFalse("non-MCP endpoints should not have the marker");
        headerValue.Should().BeNull("non-MCP endpoints should not have x-mcp-call header");
    }

    private static async Task<IHost> CreateTestHost(RequestDelegate handler, bool useMcpMarking)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddSingleton(Substitute.For<IAuthForMcpSupplier>());
                        services.AddLogging();
                        services.AddSingleton(new ZeroMcpOptions { RequireAuthentication = false });
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            if (useMcpMarking)
                            {
                                // Map MCP endpoint with middleware that marks MCP calls
                                endpoints.MapPost("/mcp", handler).AddMcpCallMarker();
                            }
                            else
                            {
                                endpoints.MapPost("/mcp", handler);
                            }
                            endpoints.MapGet("/other", handler);
                        });
                    });
            })
            .Build();

        await host.StartAsync();
        return host;
    }
}
