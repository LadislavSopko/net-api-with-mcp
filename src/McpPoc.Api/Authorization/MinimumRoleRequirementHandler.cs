using McpPoc.Api.Infrastructure;
using McpPoc.Api.Services;
using Microsoft.AspNetCore.Authorization;

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
        Log.HandleRequirementCalled(_logger, requirement.MinimumRole);

        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            Log.UserNotAuthenticated(_logger);
            return;
        }

        Log.UserAuthenticated(_logger);

        // For POC: Get user by username from claims (username = email in our setup)
        var usernameClaim = context.User.FindFirst("preferred_username")?.Value;
        if (string.IsNullOrEmpty(usernameClaim))
        {
            Log.NoPreferredUsernameClaim(_logger);
            return;
        }

        Log.FoundPreferredUsernameClaim(_logger, usernameClaim);

        // Get user from service (username matches email in test setup)
        var users = await _userService.GetAllAsync().ConfigureAwait(false);
        Log.GetAllAsyncReturned(_logger, users.Count);

        foreach (var u in users)
        {
            Log.UserInList(_logger, u.Id, u.Email, u.Role);
        }

        var user = users.FirstOrDefault(u => u.Email == usernameClaim);

        if (user == null)
        {
            Log.UserNotFoundForUsername(_logger, usernameClaim);
            return;
        }

        Log.FoundUser(_logger, user.Id, user.Name, user.Email, user.Role);

        if (user.Role >= requirement.MinimumRole)
        {
            Log.UserMeetsMinimumRole(_logger, user.Email, user.Role, requirement.MinimumRole);
            context.Succeed(requirement);
        }
        else
        {
            Log.UserDoesNotMeetMinimumRole(_logger, usernameClaim, user.Role, requirement.MinimumRole);
        }
    }
}
