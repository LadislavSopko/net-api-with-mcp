using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

/// <summary>
/// ToolCreateOptionsFactory derives McpServerToolCreateOptions from the SDK [McpServerTool] attribute
/// (the AIFunction overload of McpServerTool.Create does not read attributes itself).
/// </summary>
public class ToolCreateOptionsFactoryTests
{
    private static readonly JsonSerializerOptions SnakeCase = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    private static readonly IServiceProvider Services = new ServiceCollection().BuildServiceProvider();

    private static MethodInfo Method(string name) => typeof(SampleController).GetMethod(name)!;

    private static McpServerToolCreateOptions Create(string method, bool includeAuthorization = true) =>
        ToolCreateOptionsFactory.Create(Method(method), Services, SnakeCase, includeAuthorization);

    [Fact]
    public void Should_SetTitle_WhenAttributeHasTitle()
    {
        Create(nameof(SampleController.Titled)).Title.Should().Be("Nice title");
    }

    [Fact]
    public void Should_LeaveHintsNull_WhenAttributeDoesNotSetThem()
    {
        var options = Create(nameof(SampleController.Plain));

        options.Destructive.Should().BeNull();
        options.Idempotent.Should().BeNull();
        options.OpenWorld.Should().BeNull();
        options.ReadOnly.Should().BeNull();
    }

    [Fact]
    public void Should_SetReadOnlyTrue_WhenAttributeReadOnlyIsTrue()
    {
        Create(nameof(SampleController.ReadOnlyTool)).ReadOnly.Should().BeTrue();
    }

    [Fact]
    public void Should_SetDestructiveFalse_WhenAttributeDestructiveIsFalse()
    {
        Create(nameof(SampleController.NonDestructive)).Destructive.Should().BeFalse();
    }

    [Fact]
    public void Should_SetUseStructuredContent_WhenAttributeSetsIt()
    {
        Create(nameof(SampleController.Structured)).UseStructuredContent.Should().BeTrue();
    }

    [Fact]
    public void Should_CreateOutputSchemaFromType_WhenOutputSchemaTypeIsSet()
    {
        var options = Create(nameof(SampleController.GetUser));

        options.OutputSchema.Should().NotBeNull();
        var properties = options.OutputSchema!.Value.GetProperty("properties");
        properties.TryGetProperty("id", out _).Should().BeTrue();
        properties.TryGetProperty("name", out _).Should().BeTrue();
    }

    [Fact]
    public void Should_LeaveOutputSchemaNull_WhenOutputSchemaTypeIsNull()
    {
        Create(nameof(SampleController.Plain)).OutputSchema.Should().BeNull();
    }

    [Fact]
    public void Should_SetIcons_WhenIconSourceIsSet()
    {
        var options = Create(nameof(SampleController.WithIcon));

        options.Icons.Should().ContainSingle().Which.Source.Should().Be("https://x/icon.png");
    }

    [Fact]
    public void Should_SetDescription_WhenDescriptionAttributePresent()
    {
        Create(nameof(SampleController.Plain)).Description.Should().Be("plain description");
    }

    [Fact]
    public void Should_UseSdkMetadataLayout_WhenBuilt()
    {
        var options = Create(nameof(SampleController.Plain));

        options.Metadata.Should().NotBeNull();
        options.Metadata![0].Should().BeSameAs(Method(nameof(SampleController.Plain)));
        options.Metadata.Should().Contain(m => m is AuthorizeAttribute, "the class-level [Authorize] is part of the metadata");
    }

    [Fact]
    public void Should_StripAuthorizationMetadata_WhenIncludeAuthorizationIsFalse()
    {
        var options = Create(nameof(SampleController.Plain), includeAuthorization: false);

        options.Metadata.Should().NotBeNull();
        options.Metadata.Should().NotContain(m => m is IAuthorizeData);
        options.Metadata![0].Should().BeSameAs(Method(nameof(SampleController.Plain)));
    }

    [Fact]
    public void Should_SetServicesAndSerializerOptions_WhenProvided()
    {
        var options = Create(nameof(SampleController.Plain));

        options.Services.Should().BeSameAs(Services);
        options.SerializerOptions.Should().BeSameAs(SnakeCase);
    }

    public sealed record User(int Id, string Name);

    [Authorize]
    [McpServerToolType]
    private sealed class SampleController
    {
        [McpServerTool, Description("plain description")]
        public ActionResult<string> Plain() => "x";

        [McpServerTool(Title = "Nice title")]
        public ActionResult<string> Titled() => "x";

        [McpServerTool(ReadOnly = true)]
        public ActionResult<string> ReadOnlyTool() => "x";

        [McpServerTool(Destructive = false)]
        public ActionResult<string> NonDestructive() => "x";

        [McpServerTool(UseStructuredContent = true)]
        public ActionResult<string> Structured() => "x";

        [McpServerTool(OutputSchemaType = typeof(User))]
        public ActionResult<User> GetUser() => new User(1, "n");

        [McpServerTool(IconSource = "https://x/icon.png")]
        public ActionResult<string> WithIcon() => "x";
    }
}
