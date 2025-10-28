using Microsoft.AspNetCore.Authorization;
using McpPoc.Api.Services;

namespace McpPoc.Api.Authorization;

public class MinimumRoleRequirementHandler : AuthorizationHandler<MinimumRoleRequirement>
{
    private readonly IUserService _userService;
    private readonly ILogger<MinimumRoleRequirementHandler> _logger;

    public MinimumRoleRequirementHandler(
        IUserService userService,
        ILogger<MinimumRoleRequirementHandler> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumRoleRequirement requirement)
    {
        _logger.LogTrace("HandleRequirementAsync called for requirement: {MinRole}", requirement.MinimumRole);

        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("User is not authenticated");
            return;
        }

        _logger.LogTrace("User is authenticated");

        // For POC: Get user by username from claims (username = email in our setup)
        var usernameClaim = context.User.FindFirst("preferred_username")?.Value;
        if (string.IsNullOrEmpty(usernameClaim))
        {
            _logger.LogWarning("No preferred_username claim found in token");
            return;
        }

        _logger.LogTrace("Found preferred_username claim: {Username}", usernameClaim);

        // Get user from service (username matches email in test setup)
        var users = await _userService.GetAllAsync();
        _logger.LogTrace("GetAllAsync returned {Count} users", users.Count);

        foreach (var u in users)
        {
            _logger.LogTrace("  User in list: Id={Id}, Email={Email}, Role={Role}", u.Id, u.Email, u.Role);
        }

        var user = users.FirstOrDefault(u => u.Email == usernameClaim);

        if (user == null)
        {
            _logger.LogWarning("User not found in UserService for username: {Username}", usernameClaim);
            return;
        }

        _logger.LogTrace("Found user: Id={Id}, Name={Name}, Email={Email}, Role={Role}",
            user.Id, user.Name, user.Email, user.Role);

        if (user.Role >= requirement.MinimumRole)
        {
            _logger.LogInformation(
                "User {Email} with role {Role} meets minimum role {MinRole}",
                user.Email, user.Role, requirement.MinimumRole);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "User {Username} with role {Role} does NOT meet minimum role {MinRole}",
                usernameClaim, user.Role, requirement.MinimumRole);
        }
    }
}
