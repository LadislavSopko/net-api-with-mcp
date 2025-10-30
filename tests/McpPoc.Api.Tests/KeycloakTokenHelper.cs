using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace McpPoc.Api.Tests;

/// <summary>
/// Helper to obtain access tokens from Keycloak for testing
/// </summary>
public class KeycloakTokenHelper
{
    private readonly string _keycloakUrl;
    private readonly string _realm;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public KeycloakTokenHelper(
        string keycloakUrl = "http://127.0.0.1:8080",
        string realm = "mcppoc-realm",
        string clientId = "mcppoc-api",
        string clientSecret = "mcppoc-api-secret")
    {
        _keycloakUrl = keycloakUrl;
        _realm = realm;
        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    /// <summary>
    /// Get access token using password grant (for user login)
    /// </summary>
    public async Task<string> GetPasswordTokenAsync(string username, string password)
    {
        using var httpClient = new HttpClient();
        var tokenEndpoint = $"{_keycloakUrl}/realms/{_realm}/protocol/openid-connect/token";

        var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _clientId,
            ["username"] = username,
            ["password"] = password
            // Note: client_secret not needed for public client
        });

        var response = await httpClient.PostAsync(tokenEndpoint, requestContent);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return tokenResponse?.AccessToken ?? throw new Exception("Failed to get access token");
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }
}
