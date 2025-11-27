using System.Security.Claims;

namespace Zero.Mcp.Extensions;

/// <summary>
/// Interface for resolving user's role from claims.
/// Applications implement this to provide their role resolution logic.
/// </summary>
public interface IUserRoleResolver
{
    /// <summary>
    /// Gets the numeric role level for a user.
    /// Returns null if role cannot be determined.
    /// </summary>
    Task<int?> GetUserRoleAsync(ClaimsPrincipal user);
}

/// <summary>
/// Filters tool lists based on user authorization.
/// </summary>
public static class ToolListFilter
{
    /// <summary>
    /// Filters tools to only those the user is authorized to use.
    /// </summary>
    public static IEnumerable<string> FilterByRole(
        IEnumerable<string> allTools,
        int? userRole,
        IToolAuthorizationStore? store)
    {
        if (userRole == null || store == null)
            return Enumerable.Empty<string>();

        return allTools.Where(tool =>
        {
            var minRole = store.GetMinimumRole(tool);
            // null minRole = any authenticated user can access
            return minRole == null || userRole >= minRole;
        });
    }

    /// <summary>
    /// Extracts role value from ClaimsPrincipal using common claim types.
    /// Checks: "role", realm_access roles, resource_access roles.
    /// </summary>
    public static int? GetUserRole(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        // Try direct "role" claim first
        var roleClaim = user.FindFirst("role")?.Value;
        if (!string.IsNullOrEmpty(roleClaim))
        {
            return ParseRoleName(roleClaim);
        }

        // Try realm_roles claim (Keycloak-style)
        var realmRoles = user.FindFirst("realm_roles")?.Value;
        if (!string.IsNullOrEmpty(realmRoles))
        {
            return ParseRoleName(realmRoles);
        }

        return null;
    }

    /// <summary>
    /// Parses role name to numeric value.
    /// </summary>
    public static int? ParseRoleName(string? roleName)
    {
        if (string.IsNullOrEmpty(roleName))
            return null;

        return roleName switch
        {
            "Viewer" => 0,
            "Member" => 1,
            "Manager" => 2,
            "Admin" => 3,
            _ => null
        };
    }
}
