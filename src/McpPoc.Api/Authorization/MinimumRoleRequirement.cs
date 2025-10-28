using Microsoft.AspNetCore.Authorization;
using McpPoc.Api.Models;

namespace McpPoc.Api.Authorization;

public class MinimumRoleRequirement : IAuthorizationRequirement
{
    public UserRole MinimumRole { get; }

    public MinimumRoleRequirement(UserRole minimumRole)
    {
        MinimumRole = minimumRole;
    }
}
