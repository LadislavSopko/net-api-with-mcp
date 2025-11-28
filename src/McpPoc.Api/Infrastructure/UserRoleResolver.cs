using System.Security.Claims;
using McpPoc.Api.Services;
using Zero.Mcp.Extensions;

namespace McpPoc.Api.Infrastructure;

/// <summary>
/// Resolves user role from IUserService based on preferred_username claim.
/// Follows the same pattern as MinimumRoleRequirementHandler.
/// </summary>
public class UserRoleResolver : IUserRoleResolver
{
    private readonly IUserService _userService;
    private readonly ILogger<UserRoleResolver> _logger;

    public UserRoleResolver(IUserService userService, ILogger<UserRoleResolver> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<int?> GetUserRoleAsync(ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            _logger.LogTrace("User is not authenticated");
            return null;
        }

        // Get username from preferred_username claim (Keycloak)
        var username = user.FindFirst("preferred_username")?.Value;
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("No preferred_username claim found in token");
            return null;
        }

        _logger.LogTrace("Looking up role for user: {Username}", username);

        // Look up user by email (matches Keycloak's preferred_username)
        var users = await _userService.GetAllAsync();
        var appUser = users.FirstOrDefault(u => u.Email == username);

        if (appUser == null)
        {
            _logger.LogWarning("User not found in UserService: {Username}", username);
            return null;
        }

        var roleValue = (int)appUser.Role;
        _logger.LogTrace("Resolved role {Role} ({RoleValue}) for user {Username}",
            appUser.Role, roleValue, username);

        return roleValue;
    }
}
