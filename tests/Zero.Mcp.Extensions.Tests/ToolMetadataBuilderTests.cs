using System.ComponentModel;
using System.Reflection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

/// <summary>
/// ToolMetadataBuilder produces the metadata list the MCP SDK's AddAuthorizationFilters() inspects:
/// [MethodInfo, ...scanned-type attributes, ...method attributes]. Class-level attributes come from the
/// SCANNED tool type, not from method.DeclaringType, so an inherited tool method carries the derived
/// controller's policy as well as the base one (GitHub issue #1).
/// </summary>
public class ToolMetadataBuilderTests
{
    private static readonly MethodInfo CreateMethod = typeof(SampleController).GetMethod(nameof(SampleController.Create))!;
    private static readonly MethodInfo InfoMethod = typeof(SampleController).GetMethod(nameof(SampleController.Info))!;
    private static readonly MethodInfo PlainMethod = typeof(Bare).GetMethod(nameof(Bare.Plain))!;

    // Inherited, NOT overridden: DeclaringType is BaseFixture — exactly what the assembly scanner yields.
    private static readonly MethodInfo InheritedQuery = typeof(DerivedFixture).GetMethod(nameof(BaseFixture.Query))!;
    private static readonly MethodInfo OverriddenQuery = typeof(OverridingFixture).GetMethod(nameof(BaseFixture.Query))!;

    private static List<string?> Policies(IReadOnlyList<object> metadata) =>
        metadata.OfType<AuthorizeAttribute>().Select(a => a.Policy).ToList();

    [Fact]
    public void Should_PutMethodInfoFirst_WhenBuilt()
    {
        var result = ToolMetadataBuilder.Build(CreateMethod, typeof(SampleController), includeAuthorization: true);

        result[0].Should().BeSameAs(CreateMethod);
    }

    [Fact]
    public void Should_IncludeClassAttributesBeforeMethodAttributes_WhenBothPresent()
    {
        var result = ToolMetadataBuilder.Build(CreateMethod, typeof(SampleController), includeAuthorization: true);

        Policies(result).Should().Equal(new string?[] { null, "RequireMember" });
    }

    [Fact]
    public void Should_IncludeAllowAnonymous_WhenMethodHasIt()
    {
        var result = ToolMetadataBuilder.Build(InfoMethod, typeof(SampleController), includeAuthorization: true);

        result.Should().ContainSingle(m => m is AllowAnonymousAttribute);
    }

    [Fact]
    public void Should_IncludeNonAuthorizationAttributes_WhenPresent()
    {
        var result = ToolMetadataBuilder.Build(CreateMethod, typeof(SampleController), includeAuthorization: true);

        result.Should().Contain(m => m is DescriptionAttribute);
        result.Should().Contain(m => m is HttpGetAttribute);
    }

    [Fact]
    public void Should_StripAuthorizationMetadata_WhenIncludeAuthorizationIsFalse()
    {
        var result = ToolMetadataBuilder.Build(CreateMethod, typeof(SampleController), includeAuthorization: false);

        result.Should().NotContain(m => m is IAuthorizeData);
        result.Should().NotContain(m => m is IAllowAnonymous);
        result[0].Should().BeSameAs(CreateMethod);
        result.Should().Contain(m => m is DescriptionAttribute);
    }

    [Fact]
    public void Should_ReturnOnlyMethodInfo_WhenNoAttributes()
    {
        var result = ToolMetadataBuilder.Build(PlainMethod, typeof(Bare), includeAuthorization: true);

        result.Should().ContainSingle().Which.Should().BeSameAs(PlainMethod);
    }

    [Fact]
    public void Should_ThrowArgumentNull_WhenMethodIsNull()
    {
        var act = () => ToolMetadataBuilder.Build(null!, typeof(SampleController), includeAuthorization: true);

        act.Should().Throw<ArgumentNullException>().WithParameterName("method");
    }

    // --- Inherited tool methods (GitHub issue #1) ------------------------------------------------

    [Fact]
    public void Should_IncludeDerivedClassPolicy_WhenMethodIsInherited()
    {
        var result = ToolMetadataBuilder.Build(InheritedQuery, typeof(DerivedFixture), includeAuthorization: true);

        Policies(result).Should().Contain("AdminOnly",
            "the policy declared on the scanned controller must reach the tool metadata even when the method is inherited");
    }

    [Fact]
    public void Should_StillIncludeBaseClassPolicy_WhenMethodIsInherited()
    {
        var result = ToolMetadataBuilder.Build(InheritedQuery, typeof(DerivedFixture), includeAuthorization: true);

        Policies(result).Should().Contain("ReaderOrAbove",
            "inherit: true keeps the base policy, so the SDK combines both exactly as MVC does");
    }

    [Fact]
    public void Should_PutMethodInfoFirst_WhenMethodIsInherited()
    {
        var result = ToolMetadataBuilder.Build(InheritedQuery, typeof(DerivedFixture), includeAuthorization: true);

        result[0].Should().BeSameAs(InheritedQuery);
    }

    [Fact]
    public void Should_KeepBehaviour_WhenMethodIsOverriddenInDerivedType()
    {
        var result = ToolMetadataBuilder.Build(OverriddenQuery, typeof(OverridingFixture), includeAuthorization: true);

        Policies(result).Should().Contain(["AdminOnly", "ReaderOrAbove"]);
    }

    [Fact]
    public void Should_ApplyOnlyBasePolicy_WhenDerivedHasNoClassAttribute()
    {
        var result = ToolMetadataBuilder.Build(InheritedQuery, typeof(PlainDerivedFixture), includeAuthorization: true);

        Policies(result).Should().ContainSingle().Which.Should().Be("ReaderOrAbove");
    }

    [Fact]
    public void Should_IncludeAllowAnonymousOfDerivedType_WhenDeclaredThere()
    {
        var result = ToolMetadataBuilder.Build(InheritedQuery, typeof(AnonDerivedFixture), includeAuthorization: true);

        result.Should().Contain(m => m is AllowAnonymousAttribute,
            "[AllowAnonymous] on the scanned controller must override the inherited [Authorize]");
    }

    [Fact]
    public void Should_GiveEachDerivedTypeItsOwnPolicy_WhenSharingABaseMethod()
    {
        var admin = ToolMetadataBuilder.Build(InheritedQuery, typeof(DerivedFixture), includeAuthorization: true);
        var manager = ToolMetadataBuilder.Build(InheritedQuery, typeof(OtherDerivedFixture), includeAuthorization: true);

        Policies(admin).Should().Contain("AdminOnly").And.NotContain("ManagerOnly");
        Policies(manager).Should().Contain("ManagerOnly").And.NotContain("AdminOnly");
    }

    [Fact]
    public void Should_StripDerivedAuthorizationMetadata_WhenIncludeAuthorizationIsFalse()
    {
        var result = ToolMetadataBuilder.Build(InheritedQuery, typeof(DerivedFixture), includeAuthorization: false);

        result.Should().NotContain(m => m is IAuthorizeData);
        result.Should().NotContain(m => m is IAllowAnonymous);
        result[0].Should().BeSameAs(InheritedQuery);
        result.Should().Contain(m => m is DescriptionAttribute);
    }

    [Fact]
    public void Should_ThrowArgumentNull_WhenToolTypeIsNull()
    {
        var act = () => ToolMetadataBuilder.Build(CreateMethod, null!, includeAuthorization: true);

        act.Should().Throw<ArgumentNullException>().WithParameterName("toolType");
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

    // --- Inheritance fixtures --------------------------------------------------------------------

    [Authorize(Policy = "ReaderOrAbove")]
    private abstract class BaseFixture
    {
        [Description("q")]
        public virtual void Query() { }
    }

    [Authorize(Policy = "AdminOnly")]
    private sealed class DerivedFixture : BaseFixture
    {
        // Query is inherited and deliberately NOT overridden.
    }

    [Authorize(Policy = "AdminOnly")]
    private sealed class OverridingFixture : BaseFixture
    {
        public override void Query() { }
    }

    private sealed class PlainDerivedFixture : BaseFixture
    {
        // No class-level attribute of its own: only the inherited base policy applies.
    }

    [AllowAnonymous]
    private sealed class AnonDerivedFixture : BaseFixture
    {
    }

    [Authorize(Policy = "ManagerOnly")]
    private sealed class OtherDerivedFixture : BaseFixture
    {
    }
}
