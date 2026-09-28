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
    /// <param name="toolType">
    /// The controller type being scanned. Class-level attributes are read from this type and NOT from the
    /// type that declares the method: a tool method inherited from a base controller must carry the
    /// authorization attributes of the controller actually being registered, exactly as ASP.NET MVC
    /// applies them to the REST endpoint.
    /// </param>
    /// <param name="includeAuthorization">
    /// When <see langword="false"/>, authorization-related entries (<see cref="IAuthorizeData"/>,
    /// <see cref="IAllowAnonymous"/>, <see cref="AuthorizationPolicy"/>, <see cref="IAuthorizationRequirementData"/>)
    /// are stripped so the SDK never sees authorization metadata on the tool.
    /// </param>
    public static IReadOnlyList<object> Build(MethodInfo method, Type toolType, bool includeAuthorization)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(toolType);

        List<object> metadata = [method];

        // inherit: true still walks UP to the base class, so an inherited tool method ends up with the
        // derived policy AND the base policy, which the SDK then combines like MVC does.
        metadata.AddRange(toolType.GetCustomAttributes(inherit: true));

        metadata.AddRange(method.GetCustomAttributes(inherit: true));

        if (!includeAuthorization)
        {
            metadata.RemoveAll(static m => m is IAuthorizeData or IAllowAnonymous or AuthorizationPolicy or IAuthorizationRequirementData);
        }

        return metadata;
    }
}
