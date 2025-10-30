using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

public class McpAuthorizationPreFilterTests
{
    [Fact]
    public async Task ShouldAllowExecution_When_NoAuthorizeAttribute()
    {
        // Arrange
        var mockSupplier = new Mock<IAuthForMcpSupplier>();
        var filter = new McpAuthorizationPreFilter(mockSupplier.Object, Mock.Of<ILogger>());
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.PublicMethod))!;

        // Act
        var allowed = await filter.CheckAuthorizationAsync(methodInfo);

        // Assert
        allowed.Should().BeTrue();
        mockSupplier.Verify(x => x.CheckAuthenticatedAsync(), Times.Never);
    }

    [Fact]
    public async Task ShouldCheckAuthentication_When_AuthorizeAttribute_WithoutPolicy()
    {
        // Arrange
        var mockSupplier = new Mock<IAuthForMcpSupplier>();
        mockSupplier.Setup(x => x.CheckAuthenticatedAsync()).ReturnsAsync(true);
        var filter = new McpAuthorizationPreFilter(mockSupplier.Object, Mock.Of<ILogger>());
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.AuthenticatedMethod))!;

        // Act
        var allowed = await filter.CheckAuthorizationAsync(methodInfo);

        // Assert
        allowed.Should().BeTrue();
        mockSupplier.Verify(x => x.CheckAuthenticatedAsync(), Times.Once);
        mockSupplier.Verify(x => x.CheckPolicyAsync(It.IsAny<AuthorizeAttribute>()), Times.Never);
    }

    [Fact]
    public async Task ShouldCheckPolicy_When_AuthorizeAttribute_WithPolicy()
    {
        // Arrange
        var mockSupplier = new Mock<IAuthForMcpSupplier>();
        mockSupplier.Setup(x => x.CheckAuthenticatedAsync()).ReturnsAsync(true);
        mockSupplier.Setup(x => x.CheckPolicyAsync(It.IsAny<AuthorizeAttribute>())).ReturnsAsync(true);
        var filter = new McpAuthorizationPreFilter(mockSupplier.Object, Mock.Of<ILogger>());
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.PolicyMethod))!;

        // Act
        var allowed = await filter.CheckAuthorizationAsync(methodInfo);

        // Assert
        allowed.Should().BeTrue();
        mockSupplier.Verify(x => x.CheckAuthenticatedAsync(), Times.Once);
        mockSupplier.Verify(x => x.CheckPolicyAsync(
            It.Is<AuthorizeAttribute>(a => a.Policy == "RequireAdmin")), Times.Once);
    }

    [Fact]
    public async Task ShouldDenyExecution_When_NotAuthenticated()
    {
        // Arrange
        var mockSupplier = new Mock<IAuthForMcpSupplier>();
        mockSupplier.Setup(x => x.CheckAuthenticatedAsync()).ReturnsAsync(false);
        var filter = new McpAuthorizationPreFilter(mockSupplier.Object, Mock.Of<ILogger>());
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
        var mockSupplier = new Mock<IAuthForMcpSupplier>();
        var filter = new McpAuthorizationPreFilter(mockSupplier.Object, Mock.Of<ILogger>());
        var methodInfo = typeof(AuthorizedController).GetMethod(nameof(AuthorizedController.PublicMethod))!;

        // Act
        var allowed = await filter.CheckAuthorizationAsync(methodInfo);

        // Assert
        allowed.Should().BeTrue();
        mockSupplier.Verify(x => x.CheckAuthenticatedAsync(), Times.Never);
    }

    [Fact]
    public async Task ShouldCheckAllPolicies_When_MultipleAuthorizeAttributes()
    {
        // Arrange - SECURITY FIX: Test for multiple [Authorize] attributes
        var mockSupplier = new Mock<IAuthForMcpSupplier>();
        mockSupplier.Setup(x => x.CheckAuthenticatedAsync()).ReturnsAsync(true);
        mockSupplier.Setup(x => x.CheckPolicyAsync(It.Is<AuthorizeAttribute>(a => a.Policy == "PolicyA"))).ReturnsAsync(true);
        mockSupplier.Setup(x => x.CheckPolicyAsync(It.Is<AuthorizeAttribute>(a => a.Policy == "PolicyB"))).ReturnsAsync(true);
        var filter = new McpAuthorizationPreFilter(mockSupplier.Object, Mock.Of<ILogger>());
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.MultiPolicyMethod))!;

        // Act
        var allowed = await filter.CheckAuthorizationAsync(methodInfo);

        // Assert
        allowed.Should().BeTrue();
        mockSupplier.Verify(x => x.CheckAuthenticatedAsync(), Times.Once);
        mockSupplier.Verify(x => x.CheckPolicyAsync(It.Is<AuthorizeAttribute>(a => a.Policy == "PolicyA")), Times.Once);
        mockSupplier.Verify(x => x.CheckPolicyAsync(It.Is<AuthorizeAttribute>(a => a.Policy == "PolicyB")), Times.Once);
    }

    [Fact]
    public async Task ShouldDenyExecution_When_OneOfMultiplePoliciesFails()
    {
        // Arrange - ALL policies must pass
        var mockSupplier = new Mock<IAuthForMcpSupplier>();
        mockSupplier.Setup(x => x.CheckAuthenticatedAsync()).ReturnsAsync(true);
        mockSupplier.Setup(x => x.CheckPolicyAsync(It.Is<AuthorizeAttribute>(a => a.Policy == "PolicyA"))).ReturnsAsync(true);
        mockSupplier.Setup(x => x.CheckPolicyAsync(It.Is<AuthorizeAttribute>(a => a.Policy == "PolicyB"))).ReturnsAsync(false);
        var filter = new McpAuthorizationPreFilter(mockSupplier.Object, Mock.Of<ILogger>());
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
