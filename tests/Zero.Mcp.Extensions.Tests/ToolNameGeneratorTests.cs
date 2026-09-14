using AwesomeAssertions;
using ModelContextProtocol.Server;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

public class ToolNameGeneratorTests
{
    [Fact]
    public void GenerateName_WithMethodOnly_ReturnsMethodNameOnly()
    {
        // Arrange
        var method = typeof(TestProductsController).GetMethod(nameof(TestProductsController.GetById))!;
        var options = new ZeroMcpOptions { NamingConvention = ToolNamingConvention.MethodOnly };

        // Act
        var result = ToolNameGenerator.GenerateName(method, typeof(TestProductsController), options);

        // Assert
        result.Should().Be("get_by_id");
    }

    [Fact]
    public void GenerateName_WithControllerPrefix_ReturnsControllerPrefixedName()
    {
        // Arrange
        var method = typeof(TestProductsController).GetMethod(nameof(TestProductsController.GetById))!;
        var options = new ZeroMcpOptions { NamingConvention = ToolNamingConvention.ControllerPrefix };

        // Act
        var result = ToolNameGenerator.GenerateName(method, typeof(TestProductsController), options);

        // Assert
        result.Should().Be("test_products_get_by_id");
    }

    [Fact]
    public void GenerateName_WithExplicitAttribute_IgnoresConvention()
    {
        // Arrange
        var method = typeof(TestProductsController).GetMethod(nameof(TestProductsController.GetByIdWithExplicitName))!;
        var options = new ZeroMcpOptions { NamingConvention = ToolNamingConvention.ControllerPrefix };

        // Act
        var result = ToolNameGenerator.GenerateName(method, typeof(TestProductsController), options);

        // Assert
        result.Should().Be("explicit_tool_name");
    }

    [Fact]
    public void GetControllerPrefix_RemovesControllerSuffix()
    {
        // Act
        var result = ToolNameGenerator.GetControllerPrefix(typeof(TestProductsController));

        // Assert
        result.Should().Be("test_products");
    }

    [Fact]
    public void GetControllerPrefix_ConvertsToSnakeCase()
    {
        // Act
        var result = ToolNameGenerator.GetControllerPrefix(typeof(MyTestApiController));

        // Assert
        result.Should().Be("my_test_api");
    }

    [Fact]
    public void ToSnakeCase_ConvertsCorrectly()
    {
        // Assert various conversions
        ToolNameGenerator.ToSnakeCase("GetById").Should().Be("get_by_id");
        ToolNameGenerator.ToSnakeCase("GetAll").Should().Be("get_all");
        ToolNameGenerator.ToSnakeCase("CreateUser").Should().Be("create_user");
        ToolNameGenerator.ToSnakeCase("TestProducts").Should().Be("test_products");
        ToolNameGenerator.ToSnakeCase("HTTPSConnection").Should().Be("https_connection");
    }

    [Fact]
    public void GenerateName_WithCustomSeparator_UsesCustomSeparator()
    {
        // Arrange
        var method = typeof(TestProductsController).GetMethod(nameof(TestProductsController.GetById))!;
        var options = new ZeroMcpOptions
        {
            NamingConvention = ToolNamingConvention.ControllerPrefix,
            ToolNameSeparator = "-"
        };

        // Act
        var result = ToolNameGenerator.GenerateName(method, typeof(TestProductsController), options);

        // Assert
        result.Should().Be("test_products-get_by_id");
    }

    // Test controllers for reflection
    [McpServerToolType]
    private class TestProductsController
    {
        [McpServerTool]
        public string GetById(int id) => $"Product {id}";

        [McpServerTool(Name = "explicit_tool_name")]
        public string GetByIdWithExplicitName(int id) => $"Product {id}";
    }

    [McpServerToolType]
    private class MyTestApiController
    {
        [McpServerTool]
        public string GetAll() => "All items";
    }

    [Fact]
    public void Should_UseExplicitName_WhenSdkAttributeNameIsSet()
    {
        var method = typeof(SdkUsersController).GetMethod(nameof(SdkUsersController.GetById))!;
        var options = new ZeroMcpOptions { NamingConvention = ToolNamingConvention.ControllerPrefix };

        var result = ToolNameGenerator.GenerateName(method, typeof(SdkUsersController), options);

        result.Should().Be("UserGetById", "an explicit Name on the SDK attribute always wins over the convention");
    }

    [Theory]
    [InlineData(ToolNamingConvention.MethodOnly, "get_all")]
    [InlineData(ToolNamingConvention.ControllerPrefix, "sdk_users_get_all")]
    public void Should_UseConvention_WhenSdkAttributeNameIsNull(ToolNamingConvention convention, string expected)
    {
        var method = typeof(SdkUsersController).GetMethod(nameof(SdkUsersController.GetAll))!;
        var options = new ZeroMcpOptions { NamingConvention = convention };

        var result = ToolNameGenerator.GenerateName(method, typeof(SdkUsersController), options);

        result.Should().Be(expected);
    }

    // Fixture decorated with the official SDK attributes (ModelContextProtocol.Server).
    [McpServerToolType]
    private class SdkUsersController
    {
        [McpServerTool(Name = "UserGetById")]
        public string GetById(int id) => $"User {id}";

        [McpServerTool]
        public string GetAll() => "All users";
    }
}
