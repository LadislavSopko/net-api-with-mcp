using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Zero.Mcp.Extensions;

/// <summary>
/// Performs authorization checks before MCP tool execution using the host's IAuthForMcpSupplier.
/// </summary>
internal class McpAuthorizationPreFilter
{
    private readonly IAuthForMcpSupplier _authSupplier;
    private readonly ILogger _logger;

    public McpAuthorizationPreFilter(IAuthForMcpSupplier authSupplier, ILogger logger)
    {
        _authSupplier = authSupplier;
        _logger = logger;
    }

    /// <summary>
    /// Checks if the request is authorized to execute the specified method.
    /// Supports [AllowAnonymous] override, inherits attributes from base classes,
    /// and enforces ALL [Authorize] attributes (SECURITY: not just the first one).
    /// </summary>
    /// <param name="methodInfo">The controller method being invoked.</param>
    /// <returns>True if authorized, false otherwise.</returns>
    public async Task<bool> CheckAuthorizationAsync(MethodInfo methodInfo)
    {
        // Check for [AllowAnonymous] first (highest precedence)
        var allowAnonymous = methodInfo.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true)
            ?? methodInfo.DeclaringType?.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true);

        if (allowAnonymous != null)
        {
            _logger.LogTrace("Found [AllowAnonymous] on {Method}, allowing execution", methodInfo.Name);
            return true;
        }

        // SECURITY FIX: Get ALL [Authorize] attributes (not just the first one)
        // This is critical - ASP.NET Core evaluates ALL attributes
        var authorizeAttributes = methodInfo.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Concat(methodInfo.DeclaringType?.GetCustomAttributes<AuthorizeAttribute>(inherit: true) ?? Enumerable.Empty<AuthorizeAttribute>())
            .ToList();

        if (!authorizeAttributes.Any())
        {
            _logger.LogTrace("No [Authorize] attribute found on {Method}, allowing execution", methodInfo.Name);
            return true;
        }

        _logger.LogTrace("Found {Count} [Authorize] attributes on {Method}, checking authentication",
            authorizeAttributes.Count, methodInfo.Name);

        // Check authentication once (supplier manages its own context access)
        var isAuthenticated = await _authSupplier.CheckAuthenticatedAsync();
        if (!isAuthenticated)
        {
            _logger.LogWarning("Authentication failed for {Method}", methodInfo.Name);
            return false;
        }

        _logger.LogTrace("Authentication successful for {Method}", methodInfo.Name);

        // SECURITY FIX: Check EVERY policy - ALL must pass
        foreach (var authorizeAttr in authorizeAttributes)
        {
            if (!string.IsNullOrEmpty(authorizeAttr.Policy))
            {
                _logger.LogTrace("Checking policy '{Policy}' for {Method}", authorizeAttr.Policy, methodInfo.Name);

                var policyResult = await _authSupplier.CheckPolicyAsync(authorizeAttr);
                if (!policyResult)
                {
                    _logger.LogWarning("Policy '{Policy}' check failed for {Method}", authorizeAttr.Policy, methodInfo.Name);
                    return false;
                }

                _logger.LogTrace("Policy '{Policy}' check successful for {Method}", authorizeAttr.Policy, methodInfo.Name);
            }
        }

        return true;
    }
}
