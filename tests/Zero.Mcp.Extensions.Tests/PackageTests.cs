using FluentAssertions;
using System.Reflection;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

public class PackageTests
{
    [Fact]
    public void Package_Should_HaveVersion()
    {
        // Arrange
        var assembly = typeof(IAuthForMcpSupplier).Assembly;
        var version = assembly.GetName().Version;

        // Assert
        version.Should().NotBeNull();
        version!.Major.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public void Package_Should_HavePublicTypes()
    {
        // Arrange
        var assembly = typeof(IAuthForMcpSupplier).Assembly;
        var publicTypes = assembly.GetTypes()
            .Where(t => t.IsPublic && !t.IsNested)
            .ToList();

        // Assert
        publicTypes.Should().Contain(t => t.Name == "IAuthForMcpSupplier");
        publicTypes.Should().Contain(t => t.Name == "McpServerBuilderExtensions");
        publicTypes.Should().Contain(t => t.Name == "McpServerToolTypeAttribute");
        publicTypes.Should().Contain(t => t.Name == "McpServerToolAttribute");
    }
}
