using FluentAssertions;
using Zero.Mcp.Extensions;
using McpPoc.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace McpPoc.Api.Tests;

public class KeycloakAuthSupplierTests
{
    [Fact]
    public async Task CheckAuthenticatedAsync_Should_ReturnTrue_When_UserIsAuthenticated()
    {
        // Arrange
        var httpContextAccessor = CreateHttpContextAccessor(isAuthenticated: true);
        var supplier = new KeycloakAuthSupplier(
            httpContextAccessor,
            Mock.Of<IAuthorizationService>(),
            Mock.Of<ILogger<KeycloakAuthSupplier>>());

        // Act
        var result = await supplier.CheckAuthenticatedAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAuthenticatedAsync_Should_ReturnFalse_When_UserIsNotAuthenticated()
    {
        // Arrange
        var httpContextAccessor = CreateHttpContextAccessor(isAuthenticated: false);
        var supplier = new KeycloakAuthSupplier(
            httpContextAccessor,
            Mock.Of<IAuthorizationService>(),
            Mock.Of<ILogger<KeycloakAuthSupplier>>());

        // Act
        var result = await supplier.CheckAuthenticatedAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CheckPolicyAsync_Should_ReturnTrue_When_PolicySatisfied()
    {
        // Arrange
        var httpContextAccessor = CreateHttpContextAccessor(isAuthenticated: true);
        var mockAuthService = new Mock<IAuthorizationService>();
        mockAuthService
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), null, "RequireAdmin"))
            .ReturnsAsync(AuthorizationResult.Success());

        var supplier = new KeycloakAuthSupplier(
            httpContextAccessor,
            mockAuthService.Object,
            Mock.Of<ILogger<KeycloakAuthSupplier>>());

        var attribute = new AuthorizeAttribute { Policy = "RequireAdmin" };

        // Act
        var result = await supplier.CheckPolicyAsync(attribute);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckPolicyAsync_Should_ReturnFalse_When_PolicyNotSatisfied()
    {
        // Arrange
        var httpContextAccessor = CreateHttpContextAccessor(isAuthenticated: true);
        var mockAuthService = new Mock<IAuthorizationService>();
        mockAuthService
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), null, "RequireAdmin"))
            .ReturnsAsync(AuthorizationResult.Failed());

        var supplier = new KeycloakAuthSupplier(
            httpContextAccessor,
            mockAuthService.Object,
            Mock.Of<ILogger<KeycloakAuthSupplier>>());

        var attribute = new AuthorizeAttribute { Policy = "RequireAdmin" };

        // Act
        var result = await supplier.CheckPolicyAsync(attribute);

        // Assert
        result.Should().BeFalse();
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(bool isAuthenticated)
    {
        var identity = new ClaimsIdentity(
            isAuthenticated ? new[] { new Claim(ClaimTypes.Name, "test") } : Array.Empty<Claim>(),
            isAuthenticated ? "TestAuth" : null);

        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(httpContext);
        return accessor.Object;
    }
}
