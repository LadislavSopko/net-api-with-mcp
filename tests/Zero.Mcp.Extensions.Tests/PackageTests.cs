using AwesomeAssertions;
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
        version!.Major.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Package_Should_HavePublicTypes()
    {
        // Arrange
        var assembly = typeof(IAuthForMcpSupplier).Assembly;
        var publicTypes = assembly.GetTypes()
            .Where(t => t.IsPublic && !t.IsNested)
            .ToList();

        // Assert - Core interfaces and attributes
        publicTypes.Should().Contain(t => t.Name == "IAuthForMcpSupplier");
        publicTypes.Should().Contain(t => t.Name == "McpServerBuilderExtensions");
        // Attributes come from the SDK (ModelContextProtocol.Server) since 3.0.0 — no own copies shipped
        publicTypes.Should().NotContain(t => t.Name == "McpServerToolTypeAttribute");
        publicTypes.Should().NotContain(t => t.Name == "McpServerToolAttribute");

        // Assert - Configuration and endpoint mapping
        publicTypes.Should().Contain(t => t.Name == "ZeroMcpOptions");
        publicTypes.Should().Contain(t => t.Name == "McpEndpointExtensions");
    }

    [Fact]
    public void ZeroMcpOptions_Should_HaveDefaultValues()
    {
        // Arrange & Act
        var options = new ZeroMcpOptions();

        // Assert
        options.RequireAuthentication.Should().BeTrue();
        options.UseAuthorization.Should().BeTrue();
        options.McpEndpointPath.Should().Be("/mcp");
        options.ToolAssembly.Should().BeNull();
        options.SerializerOptions.Should().BeNull();
    }
}
