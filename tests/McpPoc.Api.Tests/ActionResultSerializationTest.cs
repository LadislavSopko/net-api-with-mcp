using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Xunit.Abstractions;

namespace McpPoc.Api.Tests;

/// <summary>
/// Quick test to understand how ActionResult serializes
/// </summary>
public class ActionResultSerializationTest
{
    private readonly ITestOutputHelper _output;

    public ActionResultSerializationTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Should_ShowWhatActionResultSerializesTo()
    {
        // Create an ActionResult<string> like controllers do
        ActionResult<string> actionResult = new OkObjectResult("Hello World");

        // Serialize it the way MCP SDK does at line 286
        var json = JsonSerializer.Serialize(actionResult, typeof(object));

        // Output to xUnit test output
        _output.WriteLine($"Serialized ActionResult: {json}");

        // Try to get the value
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        _output.WriteLine($"Root type: {root.ValueKind}");

        foreach (var prop in root.EnumerateObject())
        {
            _output.WriteLine($"Property: {prop.Name} = {prop.Value}");
        }

        // This test will show us exactly what properties exist
        json.Should().NotBeNull();
    }

    [Fact]
    public void Should_ShowWhatOkObjectResultSerializesTo()
    {
        // Create OkObjectResult directly
        var okResult = new OkObjectResult(new { Name = "Alice", Id = 1 });

        // Serialize it
        var json = JsonSerializer.Serialize(okResult, typeof(object));

        _output.WriteLine($"Serialized OkObjectResult: {json}");

        var doc = JsonDocument.Parse(json);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            _output.WriteLine($"Property: {prop.Name} = {prop.Value}");
        }

        json.Should().NotBeNull();
    }
}
