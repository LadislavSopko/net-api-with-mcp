using Microsoft.AspNetCore.Authorization;
using System.Reflection;

namespace Zero.Mcp.Extensions;

/// <summary>
/// Authorization metadata for a tool.
/// </summary>
public record ToolAuthorizationMetadata(string ToolName, int? MinimumRole)
{
    /// <summary>
    /// Extracts authorization metadata from method attributes.
    /// </summary>
    public static ToolAuthorizationMetadata FromMethod(MethodInfo method, string toolName)
    {
        // Check method and class level [Authorize] attributes
        var authorizeAttrs = method.GetCustomAttributes<AuthorizeAttribute>()
            .Concat(method.DeclaringType?.GetCustomAttributes<AuthorizeAttribute>()
                    ?? Enumerable.Empty<AuthorizeAttribute>());

        int? minimumRole = null;

        foreach (var attr in authorizeAttrs)
        {
            if (string.IsNullOrEmpty(attr.Policy)) continue;

            var role = attr.Policy switch
            {
                "RequireMember" => 1,
                "RequireManager" => 2,
                "RequireAdmin" => 3,
                _ => (int?)null
            };

            if (role.HasValue)
            {
                minimumRole = minimumRole.HasValue
                    ? Math.Max(minimumRole.Value, role.Value)
                    : role;
            }
        }

        return new ToolAuthorizationMetadata(toolName, minimumRole);
    }
}

/// <summary>
/// Simple store for tool authorization metadata.
/// </summary>
public interface IToolAuthorizationStore
{
    void Register(string toolName, ToolAuthorizationMetadata metadata);
    int? GetMinimumRole(string toolName);
}

public class ToolAuthorizationStore : IToolAuthorizationStore
{
    private readonly Dictionary<string, ToolAuthorizationMetadata> _store = new();

    public void Register(string toolName, ToolAuthorizationMetadata metadata)
        => _store[toolName] = metadata;

    public int? GetMinimumRole(string toolName)
        => _store.TryGetValue(toolName, out var meta) ? meta.MinimumRole : null;
}
