using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

public class ToolAuthorizationMetadataTests
{
    [Fact]
    public void Should_ExtractMinimumRole_FromRequireMemberPolicy()
    {
        // Arrange
        var method = typeof(TestController).GetMethod(nameof(TestController.MemberOnly))!;

        // Act
        var metadata = ToolAuthorizationMetadata.FromMethod(method, "member_only");

        // Assert
        metadata.MinimumRole.Should().Be(1);
    }

    [Fact]
    public void Should_ExtractMinimumRole_FromRequireManagerPolicy()
    {
        var method = typeof(TestController).GetMethod(nameof(TestController.ManagerOnly))!;
        var metadata = ToolAuthorizationMetadata.FromMethod(method, "manager_only");
        metadata.MinimumRole.Should().Be(2);
    }

    [Fact]
    public void Should_ExtractMinimumRole_FromRequireAdminPolicy()
    {
        var method = typeof(TestController).GetMethod(nameof(TestController.AdminOnly))!;
        var metadata = ToolAuthorizationMetadata.FromMethod(method, "admin_only");
        metadata.MinimumRole.Should().Be(3);
    }

    [Fact]
    public void Should_ReturnNullMinimumRole_ForAuthenticatedOnly()
    {
        var method = typeof(TestController).GetMethod(nameof(TestController.AuthenticatedOnly))!;
        var metadata = ToolAuthorizationMetadata.FromMethod(method, "authenticated_only");
        metadata.MinimumRole.Should().BeNull();
    }

    [Fact]
    public void Should_StoreAndRetrieve_Metadata()
    {
        // Arrange
        var store = new ToolAuthorizationStore();

        // Act
        store.Register("create", new ToolAuthorizationMetadata("create", 1));
        store.Register("update", new ToolAuthorizationMetadata("update", 2));

        // Assert
        store.GetMinimumRole("create").Should().Be(1);
        store.GetMinimumRole("update").Should().Be(2);
        store.GetMinimumRole("unknown").Should().BeNull();
    }
}

// Test fixtures
internal class TestController
{
    [Authorize(Policy = "RequireMember")]
    public void MemberOnly() { }

    [Authorize(Policy = "RequireManager")]
    public void ManagerOnly() { }

    [Authorize(Policy = "RequireAdmin")]
    public void AdminOnly() { }

    [Authorize]
    public void AuthenticatedOnly() { }
}
