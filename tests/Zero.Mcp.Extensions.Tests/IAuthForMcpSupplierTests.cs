using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

public class IAuthForMcpSupplierTests
{
    [Fact]
    public void IAuthForMcpSupplier_Should_HaveCheckAuthenticatedMethod_WithNoParameters()
    {
        // Arrange
        var interfaceType = typeof(IAuthForMcpSupplier);

        // Act
        var method = interfaceType.GetMethod("CheckAuthenticatedAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<bool>));
        method.GetParameters().Should().BeEmpty(); // No parameters
    }

    [Fact]
    public void IAuthForMcpSupplier_Should_HaveCheckPolicyMethod_WithAttributeParameter()
    {
        // Arrange
        var interfaceType = typeof(IAuthForMcpSupplier);

        // Act
        var method = interfaceType.GetMethod("CheckPolicyAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<bool>));
        var parameters = method.GetParameters();
        parameters.Should().HaveCount(1);
        parameters[0].ParameterType.Should().Be(typeof(AuthorizeAttribute));
    }
}
