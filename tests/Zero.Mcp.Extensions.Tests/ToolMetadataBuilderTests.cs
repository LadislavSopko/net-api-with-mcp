using System.ComponentModel;
using System.Reflection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

/// <summary>
/// ToolMetadataBuilder produces the metadata list the MCP SDK's AddAuthorizationFilters() inspects:
/// [MethodInfo, ...declaring-class attributes, ...method attributes].
/// </summary>
public class ToolMetadataBuilderTests
{
    private static readonly MethodInfo CreateMethod = typeof(SampleController).GetMethod(nameof(SampleController.Create))!;
    private static readonly MethodInfo InfoMethod = typeof(SampleController).GetMethod(nameof(SampleController.Info))!;
    private static readonly MethodInfo PlainMethod = typeof(Bare).GetMethod(nameof(Bare.Plain))!;

    [Fact]
    public void Should_PutMethodInfoFirst_WhenBuilt()
    {
        var result = ToolMetadataBuilder.Build(CreateMethod, includeAuthorization: true);

        result[0].Should().BeSameAs(CreateMethod);
    }

    [Fact]
    public void Should_IncludeClassAttributesBeforeMethodAttributes_WhenBothPresent()
    {
        var result = ToolMetadataBuilder.Build(CreateMethod, includeAuthorization: true);

        var policies = result.OfType<AuthorizeAttribute>().Select(a => a.Policy).ToList();

        policies.Should().Equal(new string?[] { null, "RequireMember" });
    }

    [Fact]
    public void Should_IncludeAllowAnonymous_WhenMethodHasIt()
    {
        var result = ToolMetadataBuilder.Build(InfoMethod, includeAuthorization: true);

        result.Should().ContainSingle(m => m is AllowAnonymousAttribute);
    }

    [Fact]
    public void Should_IncludeNonAuthorizationAttributes_WhenPresent()
    {
        var result = ToolMetadataBuilder.Build(CreateMethod, includeAuthorization: true);

        result.Should().Contain(m => m is DescriptionAttribute);
        result.Should().Contain(m => m is HttpGetAttribute);
    }

    [Fact]
    public void Should_StripAuthorizationMetadata_WhenIncludeAuthorizationIsFalse()
    {
        var result = ToolMetadataBuilder.Build(CreateMethod, includeAuthorization: false);

        result.Should().NotContain(m => m is IAuthorizeData);
        result.Should().NotContain(m => m is IAllowAnonymous);
        result[0].Should().BeSameAs(CreateMethod);
        result.Should().Contain(m => m is DescriptionAttribute);
    }

    [Fact]
    public void Should_ReturnOnlyMethodInfo_WhenNoAttributes()
    {
        var result = ToolMetadataBuilder.Build(PlainMethod, includeAuthorization: true);

        result.Should().ContainSingle().Which.Should().BeSameAs(PlainMethod);
    }

    [Fact]
    public void Should_ThrowArgumentNull_WhenMethodIsNull()
    {
        var act = () => ToolMetadataBuilder.Build(null!, includeAuthorization: true);

        act.Should().Throw<ArgumentNullException>().WithParameterName("method");
    }

    [Authorize]
    private sealed class SampleController
    {
        [Authorize(Policy = "RequireMember")]
        [Description("d")]
        [HttpGet]
        public void Create() { }

        [AllowAnonymous]
        public void Info() { }
    }

    private sealed class Bare
    {
        public void Plain() { }
    }
}
