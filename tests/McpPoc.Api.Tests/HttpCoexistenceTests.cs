namespace McpPoc.Api.Tests;

[Collection("McpApi")]
public class HttpCoexistenceTests
{
    private readonly McpApiFixture _fixture;

    public HttpCoexistenceTests(McpApiFixture fixture)
    {
        _fixture = fixture;
        // Reset data to seed state for test isolation
        _fixture.ResetUserStore();
    }

    [Fact]
    public async Task Should_AccessUsersViaHttpApi_WhenMcpIsAlsoEnabled()
    {
        // Arrange
        var client = await _fixture.GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "HTTP API should still work");
        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        users.Should().NotBeNull();
        users!.Should().HaveCountGreaterOrEqualTo(3);
    }

    [Fact]
    public async Task Should_GetUserByIdViaHttp_WhenMcpIsAlsoEnabled()
    {
        // Arrange
        var client = await _fixture.GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/users/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Name.Should().Be("Alice Smith");
    }

    [Fact]
    public async Task Should_DeleteViaHttp_EvenThoughNotExposedAsMcpTool()
    {
        // Arrange
        var client = await _fixture.GetAuthenticatedClientAsync();

        // Act
        var response = await client.DeleteAsync("/api/users/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, "Delete HTTP endpoint should work");
    }
}

// DTO for deserialization
public record UserDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
