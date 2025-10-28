using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace McpPoc.Api.Tests;

[Collection("McpApi")]
public class HttpAuthorizationTests
{
    private readonly McpApiFixture _fixture;

    public HttpAuthorizationTests(McpApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_AllowCreate_WhenUserIsMember_ViaHttp()
    {
        // Arrange
        var client = await _fixture.GetAuthenticatedClientAsync("alice@example.com", "alice123");
        var request = new { name = "Test User", email = "test@example.com" };

        // Act
        var response = await client.PostAsJsonAsync("/api/users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created, "Member (Alice) should be able to create users via HTTP");
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Name.Should().Be("Test User");
    }

    [Fact]
    public async Task Should_AllowUpdate_WhenUserIsManager_ViaHttp()
    {
        // Arrange
        var client = await _fixture.GetAuthenticatedClientAsync("bob@example.com", "bob123");
        var request = new { name = "Updated Name", email = "updated@example.com" };

        // Act
        var response = await client.PutAsJsonAsync("/api/users/1", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "Manager (Bob) should be able to update users via HTTP");
    }

    [Fact]
    public async Task Should_BlockUpdate_WhenUserIsMember_ViaHttp()
    {
        // Arrange
        var client = await _fixture.GetAuthenticatedClientAsync("alice@example.com", "alice123");
        var request = new { name = "Updated Name", email = "updated@example.com" };

        // Act
        var response = await client.PutAsJsonAsync("/api/users/1", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "Member (Alice) should NOT be able to update users via HTTP");
    }
}
