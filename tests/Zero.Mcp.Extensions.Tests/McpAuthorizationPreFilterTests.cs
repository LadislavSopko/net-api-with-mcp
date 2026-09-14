using AwesomeAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

public class McpAuthorizationPreFilterTests
{
    [Fact]
    public async Task ShouldAllowExecution_When_NoAuthorizeAttribute()
    {
        // Arrange
        var mockSupplier = Substitute.For<IAuthForMcpSupplier>();
        var filter = new McpAuthorizationPreFilter(mockSupplier, Substitute.For<ILogger>());
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.PublicMethod))!;

        // Act
        var allowed = await filter.CheckAuthorizationAsync(methodInfo);

        // Assert
        allowed.Should().BeTrue();
        await mockSupplier.DidNotReceive().CheckAuthenticatedAsync();
    }

    [Fact]
    public async Task ShouldCheckAuthentication_When_AuthorizeAttribute_WithoutPolicy()
    {
        // Arrange
        var mockSupplier = Substitute.For<IAuthForMcpSupplier>();
        mockSupplier.CheckAuthenticatedAsync().Returns(true);
        var filter = new McpAuthorizationPreFilter(mockSupplier, Substitute.For<ILogger>());
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.AuthenticatedMethod))!;

        // Act
        var allowed = await filter.CheckAuthorizationAsync(methodInfo);

        // Assert
        allowed.Should().BeTrue();
        await mockSupplier.Received(1).CheckAuthenticatedAsync();
        await mockSupplier.DidNotReceive().CheckPolicyAsync(Arg.Any<AuthorizeAttribute>());
    }

    [Fact]
    public async Task ShouldCheckPolicy_When_AuthorizeAttribute_WithPolicy()
    {
        // Arrange
        var mockSupplier = Substitute.For<IAuthForMcpSupplier>();
        mockSupplier.CheckAuthenticatedAsync().Returns(true);
        mockSupplier.CheckPolicyAsync(Arg.Any<AuthorizeAttribute>()).Returns(true);
        var filter = new McpAuthorizationPreFilter(mockSupplier, Substitute.For<ILogger>());
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.PolicyMethod))!;

        // Act
        var allowed = await filter.CheckAuthorizationAsync(methodInfo);

        // Assert
        allowed.Should().BeTrue();
        await mockSupplier.Received(1).CheckAuthenticatedAsync();
        await mockSupplier.Received(1).CheckPolicyAsync(Arg.Is<AuthorizeAttribute>(a => a.Policy == "RequireAdmin"));
    }

    [Fact]
    public async Task ShouldDenyExecution_When_NotAuthenticated()
    {
        // Arrange
        var mockSupplier = Substitute.For<IAuthForMcpSupplier>();
        mockSupplier.CheckAuthenticatedAsync().Returns(false);
        var filter = new McpAuthorizationPreFilter(mockSupplier, Substitute.For<ILogger>());
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.AuthenticatedMethod))!;

        // Act
        var allowed = await filter.CheckAuthorizationAsync(methodInfo);

        // Assert
        allowed.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldAllowExecution_When_AllowAnonymous_OverridesClassAuthorize()
    {
        // Arrange
        var mockSupplier = Substitute.For<IAuthForMcpSupplier>();
        var filter = new McpAuthorizationPreFilter(mockSupplier, Substitute.For<ILogger>());
        var methodInfo = typeof(AuthorizedController).GetMethod(nameof(AuthorizedController.PublicMethod))!;

        // Act
        var allowed = await filter.CheckAuthorizationAsync(methodInfo);

        // Assert
        allowed.Should().BeTrue();
        await mockSupplier.DidNotReceive().CheckAuthenticatedAsync();
    }

    [Fact]
    public async Task ShouldCheckAllPolicies_When_MultipleAuthorizeAttributes()
    {
        // Arrange - SECURITY FIX: Test for multiple [Authorize] attributes
        var mockSupplier = Substitute.For<IAuthForMcpSupplier>();
        mockSupplier.CheckAuthenticatedAsync().Returns(true);
        mockSupplier.CheckPolicyAsync(Arg.Is<AuthorizeAttribute>(a => a.Policy == "PolicyA")).Returns(true);
        mockSupplier.CheckPolicyAsync(Arg.Is<AuthorizeAttribute>(a => a.Policy == "PolicyB")).Returns(true);
        var filter = new McpAuthorizationPreFilter(mockSupplier, Substitute.For<ILogger>());
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.MultiPolicyMethod))!;

        // Act
        var allowed = await filter.CheckAuthorizationAsync(methodInfo);

        // Assert
        allowed.Should().BeTrue();
        await mockSupplier.Received(1).CheckAuthenticatedAsync();
        await mockSupplier.Received(1).CheckPolicyAsync(Arg.Is<AuthorizeAttribute>(a => a.Policy == "PolicyA"));
        await mockSupplier.Received(1).CheckPolicyAsync(Arg.Is<AuthorizeAttribute>(a => a.Policy == "PolicyB"));
    }

    [Fact]
    public async Task ShouldDenyExecution_When_OneOfMultiplePoliciesFails()
    {
        // Arrange - ALL policies must pass
        var mockSupplier = Substitute.For<IAuthForMcpSupplier>();
        mockSupplier.CheckAuthenticatedAsync().Returns(true);
        mockSupplier.CheckPolicyAsync(Arg.Is<AuthorizeAttribute>(a => a.Policy == "PolicyA")).Returns(true);
        mockSupplier.CheckPolicyAsync(Arg.Is<AuthorizeAttribute>(a => a.Policy == "PolicyB")).Returns(false);
        var filter = new McpAuthorizationPreFilter(mockSupplier, Substitute.For<ILogger>());
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.MultiPolicyMethod))!;

        // Act
        var allowed = await filter.CheckAuthorizationAsync(methodInfo);

        // Assert
        allowed.Should().BeFalse();
    }

    private class TestController
    {
        public void PublicMethod() { }

        [Authorize]
        public void AuthenticatedMethod() { }

        [Authorize(Policy = "RequireAdmin")]
        public void PolicyMethod() { }

        [Authorize(Policy = "PolicyA")]
        [Authorize(Policy = "PolicyB")]
        public void MultiPolicyMethod() { }
    }

    [Authorize]
    private class AuthorizedController
    {
        [AllowAnonymous]
        public void PublicMethod() { }
    }
}
