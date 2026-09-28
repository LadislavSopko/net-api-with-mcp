using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Zero.Mcp.Extensions;

/// <summary>
/// Derives <see cref="McpServerToolCreateOptions"/> from the SDK <see cref="McpServerToolAttribute"/> and
/// <see cref="DescriptionAttribute"/> on a controller action. The <c>AIFunction</c> overload of
/// <c>McpServerTool.Create</c> does not read attributes, so this mirrors the SDK's internal option derivation
/// for the reflection path, and attaches the metadata produced by <see cref="ToolMetadataBuilder"/>.
/// </summary>
internal static class ToolCreateOptionsFactory
{
    // SDK attribute defaults: hints are only forwarded when they differ from these, matching the SDK's
    // "was it explicitly set" semantics (the attribute exposes them as non-nullable bools).
    private const bool DestructiveDefault = true;
    private const bool IdempotentDefault = false;
    private const bool OpenWorldDefault = true;
    private const bool ReadOnlyDefault = false;

    /// <summary>
    /// Builds the tool creation options for one controller action.
    /// </summary>
    /// <param name="method">The controller action exposed as an MCP tool.</param>
    /// <param name="toolType">
    /// The controller type being scanned. Passed to <see cref="ToolMetadataBuilder.Build"/> so that a tool
    /// method inherited from a base controller carries the class-level attributes of the controller that is
    /// actually registered, not only those of the type that declares the method.
    /// </param>
    /// <param name="services">The service provider handed to the SDK for tool activation.</param>
    /// <param name="serializerOptions">Serializer options used for arguments, results and the output schema.</param>
    /// <param name="includeAuthorization">
    /// When <see langword="false"/>, authorization metadata is stripped so the SDK authorization filters
    /// see no policy on the tool.
    /// </param>
    public static McpServerToolCreateOptions Create(
        MethodInfo method,
        Type toolType,
        IServiceProvider services,
        JsonSerializerOptions serializerOptions,
        bool includeAuthorization)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(toolType);

        var options = new McpServerToolCreateOptions
        {
            Services = services,
            SerializerOptions = serializerOptions,
            Metadata = ToolMetadataBuilder.Build(method, toolType, includeAuthorization),
            Description = method.GetCustomAttribute<DescriptionAttribute>()?.Description,
        };

        var attribute = method.GetCustomAttribute<McpServerToolAttribute>();
        if (attribute is null)
        {
            return options;
        }

        options.Title = attribute.Title;
        options.UseStructuredContent = attribute.UseStructuredContent;

        if (attribute.Destructive != DestructiveDefault)
        {
            options.Destructive = attribute.Destructive;
        }

        if (attribute.Idempotent != IdempotentDefault)
        {
            options.Idempotent = attribute.Idempotent;
        }

        if (attribute.OpenWorld != OpenWorldDefault)
        {
            options.OpenWorld = attribute.OpenWorld;
        }

        if (attribute.ReadOnly != ReadOnlyDefault)
        {
            options.ReadOnly = attribute.ReadOnly;
        }

        if (attribute.OutputSchemaType is { } schemaType)
        {
            options.OutputSchema = AIJsonUtilities.CreateJsonSchema(schemaType, serializerOptions: serializerOptions);
        }

        if (!string.IsNullOrEmpty(attribute.IconSource))
        {
            options.Icons = [new Icon { Source = attribute.IconSource }];
        }

        return options;
    }
}
