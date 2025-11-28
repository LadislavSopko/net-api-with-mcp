using FluentAssertions;
using System.Security.Claims;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

public class ToolFilteringTests
{
    [Theory]
    [InlineData(0, new[] { "get_by_id", "get_all" })]                                    // Viewer
    [InlineData(1, new[] { "get_by_id", "get_all", "create" })]                          // Member
    [InlineData(2, new[] { "get_by_id", "get_all", "create", "update" })]                // Manager
    [InlineData(3, new[] { "get_by_id", "get_all", "create", "update", "promote" })]     // Admin
    public void Should_FilterTools_ByUserRole(int userRole, string[] expectedTools)
    {
        // Arrange
        var store = CreateTestStore();
        var allTools = new[] { "get_by_id", "get_all", "create", "update", "promote" };

        // Act
        var filtered = ToolListFilter.FilterByRole(allTools, userRole, store);

        // Assert
        filtered.Should().BeEquivalentTo(expectedTools);
    }

    [Fact]
    public void Should_ExtractRole_FromClaimsPrincipal()
    {
        // Arrange
        var claims = new[] { new Claim("role", "Manager") };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var role = ToolListFilter.GetUserRole(principal);

        // Assert
        role.Should().Be(2);
    }

    [Fact]
    public void Should_ReturnNull_WhenNoRoleClaim()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var role = ToolListFilter.GetUserRole(principal);
        role.Should().BeNull();
    }

    private static ToolAuthorizationStore CreateTestStore()
    {
        var store = new ToolAuthorizationStore();
        store.Register("get_by_id", new ToolAuthorizationMetadata("get_by_id", null));
        store.Register("get_all", new ToolAuthorizationMetadata("get_all", null));
        store.Register("create", new ToolAuthorizationMetadata("create", 1));
        store.Register("update", new ToolAuthorizationMetadata("update", 2));
        store.Register("promote", new ToolAuthorizationMetadata("promote", 3));
        return store;
    }
}
