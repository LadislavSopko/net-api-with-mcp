using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

public class MarshalResultTests
{
    [Fact]
    public async Task MarshalResult_Should_ExtractValue_FromActionResultOfT()
    {
        // Arrange
        var user = new TestUser { Id = Guid.NewGuid(), Name = "Test" };
        var actionResult = new ActionResult<TestUser>(user);

        // Act
        var result = await MarshalResult.UnwrapAsync(actionResult);

        // Assert
        result.Should().Be(user);
    }

    [Fact]
    public async Task MarshalResult_Should_ExtractValue_FromOkObjectResult()
    {
        // Arrange
        var user = new TestUser { Id = Guid.NewGuid(), Name = "Test" };
        var actionResult = new ActionResult<TestUser>(new OkObjectResult(user));

        // Act
        var result = await MarshalResult.UnwrapAsync(actionResult);

        // Assert
        result.Should().Be(user);
    }

    [Fact]
    public async Task MarshalResult_Should_ReturnOriginal_ForNonActionResult()
    {
        // Arrange
        var user = new TestUser { Id = Guid.NewGuid(), Name = "Test" };

        // Act
        var result = await MarshalResult.UnwrapAsync(user);

        // Assert
        result.Should().Be(user);
    }

    [Fact]
    public async Task MarshalResult_Should_ThrowException_ForErrorResult()
    {
        // Arrange
        var actionResult = new ActionResult<TestUser>(new NotFoundResult());

        // Act & Assert
        var act = async () => await MarshalResult.UnwrapAsync(actionResult);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Controller returned error result: NotFoundResult*");
    }

    [Fact]
    public async Task MarshalResult_Should_ReturnNull_ForOkWithNullValue()
    {
        // Arrange - Ok(null) is valid for nullable return types
        var actionResult = new ActionResult<TestUser?>(new OkObjectResult(null));

        // Act
        var result = await MarshalResult.UnwrapAsync(actionResult);

        // Assert
        result.Should().BeNull();
    }

    private record TestUser
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}
