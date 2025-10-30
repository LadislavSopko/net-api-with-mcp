using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;

namespace McpPoc.Api.Tests;

/// <summary>
/// Test fixture that spins up the API for integration testing
/// </summary>
public class McpApiFixture : WebApplicationFactory<Program>
{
    private readonly KeycloakTokenHelper _tokenHelper;
    private readonly Dictionary<string, string> _tokenCache;

    public McpApiFixture()
    {
        _tokenHelper = new KeycloakTokenHelper();
        _tokenCache = new Dictionary<string, string>();
    }

    /// <summary>
    /// Get HttpClient with authentication using default test user (alice@example.com).
    /// Tokens are cached for performance.
    /// </summary>
    public async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        // Use alice@example.com as the default authenticated user
        // (client_credentials flow no longer supported since client is public)
        return await GetAuthenticatedClientAsync("alice@example.com", "alice123");
    }

    /// <summary>
    /// Get HttpClient authenticated as specific user (for role-based testing).
    /// Tokens are cached per user for performance.
    /// </summary>
    public async Task<HttpClient> GetAuthenticatedClientAsync(string username, string password)
    {
        string cacheKey = $"user:{username}";

        if (!_tokenCache.TryGetValue(cacheKey, out var token))
        {
            token = await _tokenHelper.GetPasswordTokenAsync(username, password);
            _tokenCache[cacheKey] = token;
        }

        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://127.0.0.1")
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Get unauthenticated HttpClient (for testing 401 responses)
    /// </summary>
    public HttpClient GetUnauthenticatedClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://127.0.0.1")
        });
    }
}

/// <summary>
/// Collection fixture for sharing test context
/// </summary>
[CollectionDefinition("McpApi")]
public class McpApiCollection : ICollectionFixture<McpApiFixture>
{
}
