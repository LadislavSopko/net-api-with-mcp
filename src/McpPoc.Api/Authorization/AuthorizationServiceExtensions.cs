using Microsoft.AspNetCore.Authorization;
using McpPoc.Api.Models;

namespace McpPoc.Api.Authorization;

public static class AuthorizationServiceExtensions
{
    public static IServiceCollection AddMcpPocAuthorization(this IServiceCollection services)
    {
        // Scoped because it depends on IUserService which is Scoped
        services.AddScoped<IAuthorizationHandler, MinimumRoleRequirementHandler>();

        services.AddAuthorizationCore(options =>
        {
            options.AddPolicy(PolicyNames.RequireMember, policy =>
                policy.Requirements.Add(new MinimumRoleRequirement(UserRole.Member)));

            options.AddPolicy(PolicyNames.RequireManager, policy =>
                policy.Requirements.Add(new MinimumRoleRequirement(UserRole.Manager)));

            options.AddPolicy(PolicyNames.RequireAdmin, policy =>
                policy.Requirements.Add(new MinimumRoleRequirement(UserRole.Admin)));
        });

        return services;
    }
}
