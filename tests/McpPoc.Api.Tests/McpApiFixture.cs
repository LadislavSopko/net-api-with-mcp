using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;

namespace McpPoc.Api.Tests;

/// <summary>
/// Test fixture that spins up the API for integration testing
/// </summary>
public class McpApiFixture : WebApplicationFactory<Program>
{
    private readonly KeycloakTokenHelper _tokenHelper;
    private string? _cachedToken;

    public HttpClient HttpClient { get; }

    public McpApiFixture()
    {
        _tokenHelper = new KeycloakTokenHelper();

        HttpClient = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });

        // Try to get token and set it on HttpClient
        // If Keycloak is not available, tests will fail with 401
        try
        {
            _cachedToken = _tokenHelper.GetClientCredentialsTokenAsync().GetAwaiter().GetResult();
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _cachedToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not get Keycloak token: {ex.Message}");
            Console.WriteLine("Tests requiring authentication will fail with 401");
        }
    }

    /// <summary>
    /// Get authenticated HttpClient with fresh token
    /// </summary>
    public async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        var token = await _tokenHelper.GetClientCredentialsTokenAsync();
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Get authenticated HttpClient for specific user
    /// </summary>
    public async Task<HttpClient> GetAuthenticatedClientAsync(string username, string password)
    {
        var token = await _tokenHelper.GetPasswordTokenAsync(username, password);
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            HttpClient.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Collection fixture for sharing test context
/// </summary>
[CollectionDefinition("McpApi")]
public class McpApiCollection : ICollectionFixture<McpApiFixture>
{
}
