using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpPoc.Api.Filters;

/// <summary>
/// MCP filter that unwraps ActionResult wrappers from tool invocation responses.
/// This allows controllers to return ActionResult&lt;T&gt; while MCP clients receive clean data.
/// </summary>
public class ActionResultUnwrapperFilter
{
    private readonly ILogger<ActionResultUnwrapperFilter> _logger;

    public ActionResultUnwrapperFilter(ILogger<ActionResultUnwrapperFilter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a filter that unwraps ActionResult objects from MCP tool responses.
    /// </summary>
    public McpRequestHandler<CallToolRequestParams, CallToolResult> CreateFilter(
        McpRequestHandler<CallToolRequestParams, CallToolResult> next)
    {
        return async (context, cancellationToken) =>
        {
            // Call the underlying tool
            var result = await next(context, cancellationToken);

            _logger.LogInformation("[ActionResultUnwrapperFilter] Processing tool call");
            _logger.LogInformation("[ActionResultUnwrapperFilter] Content count: {Count}", result.Content.Count);

            // Only process if we have text content
            if (result.Content.Count == 0 || result.Content[0] is not TextContentBlock textBlock)
            {
                _logger.LogInformation("[ActionResultUnwrapperFilter] No text content, returning original");
                return result;
            }

            _logger.LogInformation("[ActionResultUnwrapperFilter] Original JSON: {Json}", textBlock.Text);

            // Try to parse as JSON and unwrap ActionResult
            var unwrappedText = TryUnwrapActionResult(textBlock.Text);

            _logger.LogInformation("[ActionResultUnwrapperFilter] Unwrapped JSON: {Json}", unwrappedText);
            _logger.LogInformation("[ActionResultUnwrapperFilter] Changed: {Changed}", unwrappedText != textBlock.Text);

            // If unwrapping occurred, return modified result
            if (unwrappedText != textBlock.Text)
            {
                return new CallToolResult
                {
                    Content = [new TextContentBlock { Text = unwrappedText }],
                    IsError = result.IsError,
                    StructuredContent = result.StructuredContent
                };
            }

            return result;
        };
    }

    private string TryUnwrapActionResult(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            _logger.LogInformation("[ActionResultUnwrapperFilter] Root ValueKind: {Kind}", root.ValueKind);

            // Check if this looks like an ActionResult wrapper
            // ActionResult serialization includes properties like "Value", "StatusCode", "Result", etc.
            if (root.ValueKind == JsonValueKind.Object)
            {
                // Log all properties
                foreach (var prop in root.EnumerateObject())
                {
                    _logger.LogInformation("[ActionResultUnwrapperFilter] Property: {Name} = {Value}", prop.Name, prop.Value);
                }

                // MCP SDK wraps with "result" property
                if (root.TryGetProperty("result", out var resultElement))
                {
                    _logger.LogInformation("[ActionResultUnwrapperFilter] Found 'result' property, type: {Type}", resultElement.ValueKind);

                    // If result is an object with properties, it might be the ActionResult
                    if (resultElement.ValueKind == JsonValueKind.Object)
                    {
                        // Check for ActionResult patterns inside result
                        if (resultElement.TryGetProperty("value", out var valueElement))
                        {
                            _logger.LogInformation("[ActionResultUnwrapperFilter] Found 'result.value', returning it");
                            return JsonSerializer.Serialize(valueElement);
                        }

                        if (resultElement.TryGetProperty("Value", out var capitalValueElement))
                        {
                            _logger.LogInformation("[ActionResultUnwrapperFilter] Found 'result.Value', returning it");
                            return JsonSerializer.Serialize(capitalValueElement);
                        }
                    }

                    // Return the result content directly
                    return JsonSerializer.Serialize(resultElement);
                }

                // OkObjectResult pattern: has "value" property
                if (root.TryGetProperty("value", out var valueElement2))
                {
                    return JsonSerializer.Serialize(valueElement2);
                }

                // CreatedAtActionResult pattern: has "Value" property
                if (root.TryGetProperty("Value", out var capitalValueElement2))
                {
                    return JsonSerializer.Serialize(capitalValueElement2);
                }

                // Check if it has typical ActionResult properties but empty/null value
                // In this case, return empty object
                if (root.TryGetProperty("statusCode", out _) ||
                    root.TryGetProperty("StatusCode", out _))
                {
                    // This is an ActionResult but with no value - return empty object
                    return "{}";
                }
            }

            // Not an ActionResult wrapper, return original
            return json;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "[ActionResultUnwrapperFilter] JSON parsing failed");
            // Not valid JSON or parsing failed, return original
            return json;
        }
    }
}
