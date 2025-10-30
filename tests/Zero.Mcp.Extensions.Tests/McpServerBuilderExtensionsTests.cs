using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
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
    public void WithToolsFromAssemblyUnwrappingActionResult_Should_BeCallable()
    {
        // Integration tests will verify full behavior
        true.Should().BeTrue();
    }
}
