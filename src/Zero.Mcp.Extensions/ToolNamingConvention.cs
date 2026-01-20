namespace Zero.Mcp.Extensions;

/// <summary>
/// Defines how MCP tool names are generated from controller methods.
/// </summary>
public enum ToolNamingConvention
{
    /// <summary>
    /// Use method name only (e.g., "get_by_id").
    /// Default for backward compatibility.
    /// Note: May cause duplicates if multiple controllers have same method names.
    /// </summary>
    MethodOnly,

    /// <summary>
    /// Prefix with controller name (e.g., "products_get_by_id").
    /// Recommended for generic controllers to avoid name collisions.
    /// </summary>
    ControllerPrefix
}
