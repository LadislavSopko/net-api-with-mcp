using Microsoft.AspNetCore.Authorization;

namespace Zero.Mcp.Extensions;

/// <summary>
/// Provides authentication and authorization verification for MCP tool invocations.
/// Implemented by the host application to integrate with its specific auth system.
/// </summary>
/// <remarks>
/// The host implementation manages its own dependencies (e.g., IHttpContextAccessor, IAuthorizationService).
/// This keeps the library completely decoupled from HttpContext and ASP.NET Core infrastructure.
/// </remarks>
public interface IAuthForMcpSupplier
{
    /// <summary>
    /// Checks if the current request has an authenticated user.
    /// </summary>
    /// <returns>True if authenticated, false otherwise.</returns>
    Task<bool> CheckAuthenticatedAsync();

    /// <summary>
    /// Checks if the current user satisfies the specified authorization policy.
    /// </summary>
    /// <param name="attribute">The [Authorize] attribute from the controller method or class.</param>
    /// <returns>True if the policy is satisfied, false otherwise.</returns>
    Task<bool> CheckPolicyAsync(AuthorizeAttribute attribute);
}
