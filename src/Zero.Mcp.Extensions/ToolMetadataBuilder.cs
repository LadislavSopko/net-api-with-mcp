using System.Reflection;
using Microsoft.AspNetCore.Authorization;

namespace Zero.Mcp.Extensions;

/// <summary>
/// Builds the metadata list attached to an <c>McpServerTool</c> so the MCP SDK authorization filters
/// can evaluate <c>[Authorize]</c> / <c>[AllowAnonymous]</c>. Layout mirrors the SDK's own
/// reflection path: <c>[MethodInfo, ...declaring-class attributes, ...method attributes]</c>.
/// </summary>
internal static class ToolMetadataBuilder
{
    /// <summary>
    /// Builds the metadata for <paramref name="method"/>.
    /// </summary>
    /// <param name="method">The controller action exposed as an MCP tool.</param>
    /// <param name="includeAuthorization">
    /// When <see langword="false"/>, authorization-related entries (<see cref="IAuthorizeData"/>,
    /// <see cref="IAllowAnonymous"/>, <see cref="AuthorizationPolicy"/>, <see cref="IAuthorizationRequirementData"/>)
    /// are stripped so the SDK never sees authorization metadata on the tool.
    /// </param>
    public static IReadOnlyList<object> Build(MethodInfo method, bool includeAuthorization)
    {
        ArgumentNullException.ThrowIfNull(method);

        List<object> metadata = [method];
        if (method.DeclaringType is not null)
        {
            metadata.AddRange(method.DeclaringType.GetCustomAttributes(inherit: true));
        }

        metadata.AddRange(method.GetCustomAttributes(inherit: true));

        if (!includeAuthorization)
        {
            metadata.RemoveAll(static m => m is IAuthorizeData or IAllowAnonymous or AuthorizationPolicy or IAuthorizationRequirementData);
        }

        return metadata;
    }
}
