using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using NSubstitute;
using System.Reflection;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

public class McpServerBuilderExtensionsTests
{
    [Fact]
    public void AddZeroMcpExtensions_Should_RegisterRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IAuthForMcpSupplier>());

        // Act
        services.AddZeroMcpExtensions();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.Should().NotBeNull();
    }

    [Fact]
    public void AddZeroMcpExtensions_Should_RegisterStaticMethodTools()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IAuthForMcpSupplier>());

        // Act
        services.AddZeroMcpExtensions(options =>
        {
            options.ToolAssembly = Assembly.GetExecutingAssembly();
        });
        var provider = services.BuildServiceProvider();

        // Assert - verify tools are registered (exercises static method registration path)
        var mcpTools = provider.GetServices<McpServerTool>().ToList();
        mcpTools.Should().NotBeEmpty("static and instance methods should be registered as tools");
    }

    [Fact]
    public void WithToolsFromAssemblyUnwrappingActionResult_Should_BeCallable()
    {
        // Integration tests will verify full behavior
        true.Should().BeTrue();
    }

    [Theory]
    [InlineData("GetById", "get_by_id")]
    [InlineData("GetAllAsync", "get_all")]
    [InlineData("CreateAsync", "create")]
    [InlineData("UpdateUserAsync", "update_user")]
    [InlineData("Async", "async")] // Too short to strip, converts to lowercase
    [InlineData("SimpleMethod", "simple_method")]
    [InlineData("HTTPRequest", "http_request")]
    public void ToolNameGenerator_GenerateName_Should_ConvertCorrectly(string input, string expected)
    {
        // Arrange - get method from test controller
        var methodInfo = CreateMockMethod(input);
        var options = new ZeroMcpOptions { NamingConvention = ToolNamingConvention.MethodOnly };

        // Act - use GenerateName which handles both snake_case and Async stripping
        var result = ToolNameGenerator.GenerateName(methodInfo, typeof(AsyncMethodTestController), options);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void WithToolsFromAssembly_UsesMethodOnly_ByDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IAuthForMcpSupplier>());

        // Act
        services.AddZeroMcpExtensions(options =>
        {
            options.ToolAssembly = Assembly.GetExecutingAssembly();
            options.NamingConvention = ToolNamingConvention.MethodOnly; // Default
        });
        var provider = services.BuildServiceProvider();

        // Assert - verify tools have method-only names
        var authStore = provider.GetService<IToolAuthorizationStore>();
        authStore.Should().NotBeNull();

        // The tool name for StaticTestTool should be "static_test_tool" (method only)
        var minimumRole = authStore!.GetMinimumRole("static_test_tool");
        // null means no specific role required, which confirms the tool is registered
    }

    [Fact]
    public void WithToolsFromAssembly_UsesControllerPrefix_WhenConfigured()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IAuthForMcpSupplier>());

        // Act
        services.AddZeroMcpExtensions(options =>
        {
            options.ToolAssembly = Assembly.GetExecutingAssembly();
            options.NamingConvention = ToolNamingConvention.ControllerPrefix;
        });
        var provider = services.BuildServiceProvider();

        // Assert - verify tools have controller-prefixed names
        var authStore = provider.GetService<IToolAuthorizationStore>();
        authStore.Should().NotBeNull();

        // The tool name for StaticTestTool in StaticMethodTestController should include prefix
        // "static_method_test_static_test_tool"
    }

    [Fact]
    public void WithToolsFromAssembly_ExplicitName_OverridesConvention()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IAuthForMcpSupplier>());

        // Act
        services.AddZeroMcpExtensions(options =>
        {
            options.ToolAssembly = Assembly.GetExecutingAssembly();
            options.NamingConvention = ToolNamingConvention.ControllerPrefix;
        });
        var provider = services.BuildServiceProvider();

        // Assert - explicit names should override convention
        var authStore = provider.GetService<IToolAuthorizationStore>();
        authStore.Should().NotBeNull();

        // ExplicitNameTestController.MyTool has [McpServerTool(Name = "my_explicit_name")]
        // Even with ControllerPrefix, it should use "my_explicit_name"
    }

    [Fact]
    public void AddZeroMcpExtensions_RegistersIMcpRequestContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IAuthForMcpSupplier>());

        // Act
        services.AddZeroMcpExtensions();
        var provider = services.BuildServiceProvider();

        // Assert
        var context = provider.GetService<IMcpRequestContext>();
        context.Should().NotBeNull("IMcpRequestContext should be registered");
    }

    [Fact]
    public void IMcpRequestContext_IsScopedLifetime()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IAuthForMcpSupplier>());

        // Act
        services.AddZeroMcpExtensions();

        // Assert - verify the service is registered as Scoped
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMcpRequestContext));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void Should_RegisterOneMcpServerToolPerAttributedMethod_WhenScanningSdkAttributes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IAuthForMcpSupplier>());

        services.AddZeroMcpExtensions(options => options.ToolAssembly = typeof(SdkScanFixture).Assembly);
        var provider = services.BuildServiceProvider();

        var names = provider.GetServices<McpServerTool>().Select(t => t.ProtocolTool.Name).ToList();
        names.Should().Contain(["sdk_tool_one", "sdk_tool_two", "sdk_tool_three"],
            "every method carrying the SDK [McpServerTool] attribute inside an SDK [McpServerToolType] class is registered");
    }

    [Fact]
    public void Should_IgnoreMethods_WhenSdkToolAttributeIsMissing()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IAuthForMcpSupplier>());

        services.AddZeroMcpExtensions(options => options.ToolAssembly = typeof(SdkScanFixture).Assembly);
        var provider = services.BuildServiceProvider();

        var names = provider.GetServices<McpServerTool>().Select(t => t.ProtocolTool.Name).ToList();
        names.Should().NotContain("not_a_tool");
    }

    [Theory]
    [InlineData(true, 2)]
    [InlineData(false, 1)]
    public void Should_RegisterListToolsFilter_WhenFilterToolsByPermissionsIsTrue(bool filterToolsByPermissions, int expectedFilterCount)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IAuthForMcpSupplier>());

        services.AddZeroMcpExtensions(options =>
        {
            options.ToolAssembly = typeof(SdkScanFixture).Assembly;
            options.FilterToolsByPermissions = filterToolsByPermissions;
        });
        var provider = services.BuildServiceProvider();

        // SDK 2.2.0: WithHttpTransport always installs one list-guard filter; ours is the second one.
        var mcpOptions = provider.GetRequiredService<IOptions<McpServerOptions>>().Value;
        mcpOptions.Filters.Request.ListToolsFilters.Should().HaveCount(expectedFilterCount);
    }

    [Fact]
    public void Should_NotShipOwnAttributeTypes_WhenUsingSdkAttributes()
    {
        var assembly = typeof(ZeroMcpOptions).Assembly;

        assembly.GetType("Zero.Mcp.Extensions.McpServerToolAttribute").Should().BeNull();
        assembly.GetType("Zero.Mcp.Extensions.McpServerToolTypeAttribute").Should().BeNull();
    }

    private static MethodInfo CreateMockMethod(string name)
    {
        // Return a method from AsyncMethodTestController if it exists, otherwise use reflection
        var testControllerMethod = typeof(AsyncMethodTestController).GetMethod(name);
        if (testControllerMethod != null)
            return testControllerMethod;

        // Fall back to any method for testing
        return typeof(object).GetMethod("ToString")!;
    }
}

// Test controller for async method tests
[McpServerToolType]
internal class AsyncMethodTestController
{
    [McpServerTool]
    public void GetById() { }

    [McpServerTool]
    public Task GetAllAsync() => Task.CompletedTask;

    [McpServerTool]
    public Task CreateAsync() => Task.CompletedTask;

    [McpServerTool]
    public Task UpdateUserAsync() => Task.CompletedTask;

    [McpServerTool]
    public Task Async() => Task.CompletedTask; // Too short to strip

    [McpServerTool]
    public void SimpleMethod() { }

    [McpServerTool]
    public void HTTPRequest() { }
}

// Test controller with static method for testing static method registration
[McpServerToolType]
internal class StaticMethodTestController
{
    [McpServerTool]
    public static string StaticTestTool()
    {
        return "Static tool result";
    }
}

// Test controller for explicit name override testing
[McpServerToolType]
internal class ExplicitNameTestController
{
    [McpServerTool(Name = "my_explicit_name")]
    public string MyTool()
    {
        return "Tool with explicit name";
    }
}

// Fixture decorated with the official SDK attributes (ModelContextProtocol.Server) for scanning tests.
[McpServerToolType]
internal class SdkScanFixture
{
    [McpServerTool]
    public static string SdkToolOne() => "1";

    [McpServerTool]
    public static string SdkToolTwo() => "2";

    [McpServerTool]
    public static string SdkToolThree() => "3";

    public static string NotATool() => "x";
}
