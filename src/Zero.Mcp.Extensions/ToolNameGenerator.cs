using System.Reflection;
using System.Text.Json;

namespace Zero.Mcp.Extensions;

/// <summary>
/// Generates tool names based on method info and naming convention options.
/// </summary>
public static class ToolNameGenerator
{
    /// <summary>
    /// Generates a tool name for a method based on the specified options.
    /// </summary>
    /// <param name="method">The method to generate a name for.</param>
    /// <param name="controllerType">The controller type containing the method.</param>
    /// <param name="options">The naming options.</param>
    /// <returns>The generated tool name.</returns>
    public static string GenerateName(MethodInfo method, Type controllerType, ZeroMcpOptions options)
    {
        // Check for explicit name in attribute first - always wins
        var attribute = method.GetCustomAttribute<ModelContextProtocol.Server.McpServerToolAttribute>();
        if (!string.IsNullOrEmpty(attribute?.Name))
        {
            return attribute.Name;
        }

        // Get method name in snake_case (without Async suffix)
        var methodName = ToSnakeCase(StripAsyncSuffix(method.Name));

        // Apply naming convention
        return options.NamingConvention switch
        {
            ToolNamingConvention.ControllerPrefix =>
                $"{GetControllerPrefix(controllerType)}{options.ToolNameSeparator}{methodName}",
            _ => methodName // MethodOnly is default
        };
    }

    /// <summary>
    /// Gets the controller prefix in snake_case without "Controller" suffix.
    /// </summary>
    /// <param name="controllerType">The controller type.</param>
    /// <returns>The controller prefix in snake_case.</returns>
    public static string GetControllerPrefix(Type controllerType)
    {
        var name = controllerType.Name;

        // Remove "Controller" suffix if present
        if (name.EndsWith("Controller", StringComparison.Ordinal))
        {
            name = name.Substring(0, name.Length - "Controller".Length);
        }

        return ToSnakeCase(name);
    }

    /// <summary>
    /// Converts a PascalCase or camelCase string to snake_case.
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <returns>The name in snake_case.</returns>
    public static string ToSnakeCase(string name)
    {
        return JsonNamingPolicy.SnakeCaseLower.ConvertName(name) ?? name;
    }

    /// <summary>
    /// Removes "Async" suffix from method name if present.
    /// </summary>
    private static string StripAsyncSuffix(string methodName)
    {
        if (methodName.EndsWith("Async", StringComparison.Ordinal) && methodName.Length > 5)
        {
            return methodName.Substring(0, methodName.Length - 5);
        }
        return methodName;
    }
}
