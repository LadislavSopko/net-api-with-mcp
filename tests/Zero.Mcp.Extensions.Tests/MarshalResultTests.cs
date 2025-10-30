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

    [Fact]
    public async Task MarshalResult_Should_UnwrapValueTask()
    {
        // Arrange
        var valueTask = new ValueTask();

        // Act
        var result = await MarshalResult.UnwrapAsync(valueTask);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task MarshalResult_Should_UnwrapTaskOfT()
    {
        // Arrange
        var user = new TestUser { Id = Guid.NewGuid(), Name = "Test" };
        var task = Task.FromResult(user);

        // Act
        var result = await MarshalResult.UnwrapAsync(task);

        // Assert
        result.Should().Be(user);
    }

    [Fact]
    public async Task MarshalResult_Should_UnwrapNonGenericTask()
    {
        // Arrange
        var task = Task.CompletedTask;

        // Act
        var result = await MarshalResult.UnwrapAsync(task);

        // Assert
        result.Should().NotBeNull("Task.CompletedTask returns VoidTaskResult");
    }

    [Fact]
    public async Task MarshalResult_Should_ThrowException_ForBadRequestResult()
    {
        // Arrange
        var actionResult = new ActionResult<TestUser>(new BadRequestResult());

        // Act & Assert
        var act = async () => await MarshalResult.UnwrapAsync(actionResult);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Controller returned error result: BadRequestResult*");
    }

    [Fact]
    public async Task MarshalResult_Should_ThrowException_ForUnauthorizedResult()
    {
        // Arrange
        var actionResult = new ActionResult<TestUser>(new UnauthorizedResult());

        // Act & Assert
        var act = async () => await MarshalResult.UnwrapAsync(actionResult);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Controller returned error result: UnauthorizedResult*");
    }

    [Fact]
    public async Task MarshalResult_Should_ReturnNull_ForNullInput()
    {
        // Act
        var result = await MarshalResult.UnwrapAsync(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task MarshalResult_Should_ExtractValue_FromCreatedAtActionResult()
    {
        // Arrange
        var user = new TestUser { Id = Guid.NewGuid(), Name = "Test" };
        var createdResult = new CreatedAtActionResult("GetById", "Users", new { id = 1 }, user);
        var actionResult = new ActionResult<TestUser>(createdResult);

        // Act
        var result = await MarshalResult.UnwrapAsync(actionResult);

        // Assert
        result.Should().Be(user);
    }

    [Fact]
    public async Task MarshalResult_Should_ExtractValue_FromActionResultWithDirectValue()
    {
        // Arrange - ActionResult<T> with direct value (no Result property)
        var user = new TestUser { Id = Guid.NewGuid(), Name = "Test" };
        var actionResult = new ActionResult<TestUser>(user);

        // Act
        var result = await MarshalResult.UnwrapAsync(actionResult);

        // Assert
        result.Should().Be(user);
    }

    [Fact]
    public async Task MarshalResult_Should_ThrowException_ForStatusCodeActionResult()
    {
        // Arrange - StatusCodeResult (not ObjectResult)
        var actionResult = new ActionResult<TestUser>(new StatusCodeResult(500));

        // Act & Assert
        var act = async () => await MarshalResult.UnwrapAsync(actionResult);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Controller returned error result: StatusCodeResult*");
    }

    [Fact]
    public async Task MarshalResult_Should_ExtractValue_FromAcceptedResult()
    {
        // Arrange
        var user = new TestUser { Id = Guid.NewGuid(), Name = "Test" };
        var acceptedResult = new AcceptedResult("location", user);
        var actionResult = new ActionResult<TestUser>(acceptedResult);

        // Act
        var result = await MarshalResult.UnwrapAsync(actionResult);

        // Assert
        result.Should().Be(user);
    }

    [Fact]
    public async Task MarshalResult_Should_ReturnOriginalValue_WhenNotActionResult()
    {
        // Arrange
        var plainString = "plain value";

        // Act
        var result = await MarshalResult.UnwrapAsync(plainString);

        // Assert
        result.Should().Be(plainString);
    }

    private record TestUser
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}
