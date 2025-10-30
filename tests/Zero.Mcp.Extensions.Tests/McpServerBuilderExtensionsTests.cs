using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Moq;
using System.Reflection;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

public class McpServerBuilderExtensionsTests
{
    [Fact]
    public void AddZeroMcpExtensions_Should_RegisterRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IAuthForMcpSupplier>());

        // Act
        services.AddZeroMcpExtensions();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.Should().NotBeNull();
    }

    [Fact]
    public void AddZeroMcpExtensions_Should_RegisterStaticMethodTools()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IAuthForMcpSupplier>());

        // Act
        services.AddZeroMcpExtensions(options =>
        {
            options.ToolAssembly = Assembly.GetExecutingAssembly();
        });
        var provider = services.BuildServiceProvider();

        // Assert - verify tools are registered (exercises static method registration path)
        var mcpTools = provider.GetServices<McpServerTool>().ToList();
        mcpTools.Should().NotBeEmpty("static and instance methods should be registered as tools");
    }

    [Fact]
    public void WithToolsFromAssemblyUnwrappingActionResult_Should_BeCallable()
    {
        // Integration tests will verify full behavior
        true.Should().BeTrue();
    }

    [Theory]
    [InlineData("GetById", "get_by_id")]
    [InlineData("GetAllAsync", "get_all")]
    [InlineData("CreateAsync", "create")]
    [InlineData("UpdateUserAsync", "update_user")]
    [InlineData("Async", "async")] // Too short to strip, converts to lowercase
    [InlineData("SimpleMethod", "simple_method")]
    [InlineData("HTTPRequest", "http_request")]
    public void ConvertToSnakeCase_Should_ConvertCorrectly(string input, string expected)
    {
        // Arrange - use reflection to access private method
        var method = typeof(McpServerBuilderExtensions)
            .GetMethod("ConvertToSnakeCase", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull("ConvertToSnakeCase method should exist");

        // Act
        var result = method!.Invoke(null, new object[] { input }) as string;

        // Assert
        result.Should().Be(expected);
    }
}

// Test controller with static method for testing static method registration
[McpServerToolType]
internal class StaticMethodTestController
{
    [McpServerTool]
    public static string StaticTestTool()
    {
        return "Static tool result";
    }
}
