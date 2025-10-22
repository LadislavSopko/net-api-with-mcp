using System.Net;
using FluentAssertions;
using Xunit;

namespace McpPoc.Api.Tests;

[Collection("McpApi")]
public class AuthenticationTests
{
    private readonly McpApiFixture _fixture;

    public AuthenticationTests(McpApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_Return401_WhenAccessingUsersWithoutAuthentication()
    {
        // Arrange - No authentication token

        // Act
        var response = await _fixture.HttpClient.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "endpoints with [Authorize] should return 401 without token");
    }

    [Fact]
    public async Task Should_Return401_WhenAccessingUserByIdWithoutAuthentication()
    {
        // Arrange - No authentication token

        // Act
        var response = await _fixture.HttpClient.GetAsync("/api/users/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "endpoints with [Authorize] should return 401 without token");
    }

    [Fact]
    public async Task Should_Return401_WhenCreatingUserWithoutAuthentication()
    {
        // Arrange - No authentication token
        var content = new StringContent(
            """{"name":"Test User","email":"test@example.com"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        // Act
        var response = await _fixture.HttpClient.PostAsync("/api/users", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "endpoints with [Authorize] should return 401 without token");
    }

    [Fact]
    public async Task Should_Return401_WhenDeletingUserWithoutAuthentication()
    {
        // Arrange - No authentication token

        // Act
        var response = await _fixture.HttpClient.DeleteAsync("/api/users/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "endpoints with [Authorize] should return 401 without token");
    }
}
