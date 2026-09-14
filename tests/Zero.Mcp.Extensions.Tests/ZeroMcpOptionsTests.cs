using AwesomeAssertions;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

public class ZeroMcpOptionsTests
{
    [Fact]
    public void NamingConvention_DefaultsTo_MethodOnly()
    {
        // Arrange & Act
        var options = new ZeroMcpOptions();

        // Assert
        options.NamingConvention.Should().Be(ToolNamingConvention.MethodOnly);
    }

    [Fact]
    public void ToolNameSeparator_DefaultsTo_Underscore()
    {
        // Arrange & Act
        var options = new ZeroMcpOptions();

        // Assert
        options.ToolNameSeparator.Should().Be("_");
    }

    [Fact]
    public void NamingConvention_CanBeSet_ToControllerPrefix()
    {
        // Arrange
        var options = new ZeroMcpOptions();

        // Act
        options.NamingConvention = ToolNamingConvention.ControllerPrefix;

        // Assert
        options.NamingConvention.Should().Be(ToolNamingConvention.ControllerPrefix);
    }

    [Fact]
    public void ToolNameSeparator_CanBeCustomized()
    {
        // Arrange
        var options = new ZeroMcpOptions();

        // Act
        options.ToolNameSeparator = "-";

        // Assert
        options.ToolNameSeparator.Should().Be("-");
    }

    [Fact]
    public void Should_NotExposeFilterToolsByPermissions_WhenAuthorizationIsSdkDriven()
    {
        // Removed in 3.0.0: the SDK authorization filters always filter tools/list when UseAuthorization is true.
        typeof(ZeroMcpOptions).GetProperty("FilterToolsByPermissions").Should().BeNull();
    }
}
