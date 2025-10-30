using Zero.Mcp.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace McpPoc.Api.Infrastructure;

/// <summary>
/// Keycloak-specific implementation of IAuthForMcpSupplier.
/// Integrates MCP tool authorization with Keycloak OAuth2/OIDC authentication.
/// </summary>
public class KeycloakAuthSupplier : IAuthForMcpSupplier
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<KeycloakAuthSupplier> _logger;

    public KeycloakAuthSupplier(
        IHttpContextAccessor httpContextAccessor,
        IAuthorizationService authorizationService,
        ILogger<KeycloakAuthSupplier> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    public Task<bool> CheckAuthenticatedAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext is null in KeycloakAuthSupplier");
            return Task.FromResult(false);
        }

        var isAuthenticated = httpContext.User?.Identity?.IsAuthenticated ?? false;

        _logger.LogTrace(
            "Authentication check: {IsAuthenticated} for user {User}",
            isAuthenticated,
            httpContext.User?.Identity?.Name ?? "anonymous");

        return Task.FromResult(isAuthenticated);
    }

    public async Task<bool> CheckPolicyAsync(AuthorizeAttribute attribute)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext is null in KeycloakAuthSupplier.CheckPolicyAsync");
            return false;
        }

        if (string.IsNullOrEmpty(attribute.Policy))
        {
            _logger.LogWarning("Policy is null or empty in [Authorize] attribute");
            return false;
        }

        _logger.LogTrace(
            "Checking policy '{Policy}' for user {User}",
            attribute.Policy,
            httpContext.User?.Identity?.Name ?? "anonymous");

        var authResult = await _authorizationService.AuthorizeAsync(
            httpContext.User ?? new System.Security.Claims.ClaimsPrincipal(),
            null,
            attribute.Policy);

        if (authResult.Succeeded)
        {
            _logger.LogTrace("Policy '{Policy}' check succeeded", attribute.Policy);
        }
        else
        {
            _logger.LogWarning(
                "Policy '{Policy}' check failed for user {User}. Failures: {Failures}",
                attribute.Policy,
                httpContext.User?.Identity?.Name ?? "anonymous",
                string.Join(", ", authResult.Failure?.FailureReasons.Select(r => r.Message) ?? Array.Empty<string>()));
        }

        return authResult.Succeeded;
    }
}
