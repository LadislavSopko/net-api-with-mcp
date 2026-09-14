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
        // Arrange - Get unauthenticated client
        var unauthClient = _fixture.GetUnauthenticatedClient();

        // Act
        var response = await unauthClient.GetAsync("/api/users", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "endpoints with [Authorize] should return 401 without token");
    }

    [Fact]
    public async Task Should_Return401_WhenAccessingUserByIdWithoutAuthentication()
    {
        // Arrange - Get unauthenticated client
        var unauthClient = _fixture.GetUnauthenticatedClient();

        // Act
        var response = await unauthClient.GetAsync("/api/users/1", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "endpoints with [Authorize] should return 401 without token");
    }

    [Fact]
    public async Task Should_Return401_WhenCreatingUserWithoutAuthentication()
    {
        // Arrange - Get unauthenticated client
        var unauthClient = _fixture.GetUnauthenticatedClient();
        var content = new StringContent(
            """{"name":"Test User","email":"test@example.com"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        // Act
        var response = await unauthClient.PostAsync("/api/users", content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "endpoints with [Authorize] should return 401 without token");
    }

    [Fact]
    public async Task Should_Return401_WhenDeletingUserWithoutAuthentication()
    {
        // Arrange - Get unauthenticated client
        var unauthClient = _fixture.GetUnauthenticatedClient();

        // Act
        var response = await unauthClient.DeleteAsync("/api/users/1", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "endpoints with [Authorize] should return 401 without token");
    }
}
