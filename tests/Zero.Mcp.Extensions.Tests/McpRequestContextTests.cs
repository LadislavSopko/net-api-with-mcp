using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

public class McpRequestContextTests
{
    [Fact]
    public void IsMcpCall_ReturnsFalse_WhenNotMcpRequest()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        var context = new McpRequestContext(accessor.Object);

        // Act & Assert
        context.IsMcpCall.Should().BeFalse();
    }

    [Fact]
    public void IsMcpCall_ReturnsTrue_WhenMcpMarkerPresent()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items[McpRequestContext.McpCallMarkerKey] = true;

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        var context = new McpRequestContext(accessor.Object);

        // Act & Assert
        context.IsMcpCall.Should().BeTrue();
    }

    [Fact]
    public void GetHeader_ReturnsNull_WhenNotMcpCall()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Custom-Header"] = "custom-value";
        // Note: No MCP marker set

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        var context = new McpRequestContext(accessor.Object);

        // Act
        var value = context.GetHeader("X-Custom-Header");

        // Assert
        value.Should().BeNull("headers should not be accessible when not an MCP call");
    }

    [Fact]
    public void GetHeader_ReturnsValue_WhenMcpCall()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items[McpRequestContext.McpCallMarkerKey] = true;
        httpContext.Request.Headers["X-Custom-Header"] = "custom-value";

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        var context = new McpRequestContext(accessor.Object);

        // Act
        var value = context.GetHeader("X-Custom-Header");

        // Assert
        value.Should().Be("custom-value");
    }

    [Fact]
    public void Headers_ReturnsNull_WhenNotMcpCall()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Custom-Header"] = "custom-value";
        // Note: No MCP marker set

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        var context = new McpRequestContext(accessor.Object);

        // Act
        var headers = context.Headers;

        // Assert
        headers.Should().BeNull("headers dictionary should not be accessible when not an MCP call");
    }

    [Fact]
    public void Headers_ReturnsHeaders_WhenMcpCall()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items[McpRequestContext.McpCallMarkerKey] = true;
        httpContext.Request.Headers["X-Custom-Header"] = "custom-value";

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        var context = new McpRequestContext(accessor.Object);

        // Act
        var headers = context.Headers;

        // Assert
        headers.Should().NotBeNull();
        headers!["X-Custom-Header"].ToString().Should().Be("custom-value");
    }

    [Fact]
    public void IsMcpCall_ReturnsFalse_WhenHttpContextIsNull()
    {
        // Arrange
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        var context = new McpRequestContext(accessor.Object);

        // Act & Assert
        context.IsMcpCall.Should().BeFalse();
    }
}
