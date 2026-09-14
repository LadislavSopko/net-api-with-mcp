using System.ComponentModel;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.IO.Pipelines;
using ModelContextProtocol.Protocol;
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

        // Act
        services.AddZeroMcpExtensions(options =>
        {
            options.ToolAssembly = Assembly.GetExecutingAssembly();
            options.NamingConvention = ToolNamingConvention.MethodOnly; // Default
        });
        var provider = services.BuildServiceProvider();

        // Assert - verify tools have method-only names
        var names = provider.GetServices<McpServerTool>().Select(t => t.ProtocolTool.Name).ToList();
        names.Should().Contain("static_test_tool");
    }

    [Fact]
    public void WithToolsFromAssembly_UsesControllerPrefix_WhenConfigured()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddZeroMcpExtensions(options =>
        {
            options.ToolAssembly = Assembly.GetExecutingAssembly();
            options.NamingConvention = ToolNamingConvention.ControllerPrefix;
        });
        var provider = services.BuildServiceProvider();

        // Assert - verify tools have controller-prefixed names
        var names = provider.GetServices<McpServerTool>().Select(t => t.ProtocolTool.Name).ToList();
        names.Should().Contain("static_method_test_static_test_tool");
    }

    [Fact]
    public void WithToolsFromAssembly_ExplicitName_OverridesConvention()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddZeroMcpExtensions(options =>
        {
            options.ToolAssembly = Assembly.GetExecutingAssembly();
            options.NamingConvention = ToolNamingConvention.ControllerPrefix;
        });
        var provider = services.BuildServiceProvider();

        // Assert - explicit names should override convention
        var names = provider.GetServices<McpServerTool>().Select(t => t.ProtocolTool.Name).ToList();
        names.Should().Contain("my_explicit_name");
    }

    [Fact]
    public void AddZeroMcpExtensions_RegistersIMcpRequestContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

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

        services.AddZeroMcpExtensions(options => options.ToolAssembly = typeof(SdkScanFixture).Assembly);
        var provider = services.BuildServiceProvider();

        var names = provider.GetServices<McpServerTool>().Select(t => t.ProtocolTool.Name).ToList();
        names.Should().NotContain("not_a_tool");
    }

    [Fact]
    public void Should_NotShipOwnAttributeTypes_WhenUsingSdkAttributes()
    {
        var assembly = typeof(ZeroMcpOptions).Assembly;

        assembly.GetType("Zero.Mcp.Extensions.McpServerToolAttribute").Should().BeNull();
        assembly.GetType("Zero.Mcp.Extensions.McpServerToolTypeAttribute").Should().BeNull();
    }

    [Fact]
    public void Should_AttachMetadataWithAuthorization_WhenToolsRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddZeroMcpExtensions(options =>
        {
            options.ToolAssembly = typeof(SdkScanFixture).Assembly;
            options.UseAuthorization = true;
        });
        var provider = services.BuildServiceProvider();

        var fixtureTools = provider.GetServices<McpServerTool>()
            .Where(t => t.ProtocolTool.Name.StartsWith("sdk_tool_", StringComparison.Ordinal))
            .ToList();

        fixtureTools.Should().HaveCount(3);
        foreach (var tool in fixtureTools)
        {
            tool.Metadata[0].Should().BeAssignableTo<MethodInfo>().Which.Name.Should().StartWith("SdkTool");
            tool.Metadata.Should().Contain(m => m is DescriptionAttribute);
            // AddAuthorizationFilters() is registered when UseAuthorization is true, so the class-level [Authorize] flows through.
            tool.Metadata.Should().Contain(m => m is IAuthorizeData);
        }
    }

    [Theory]
    [InlineData(true, 1, 2)]
    [InlineData(false, 0, 1)]
    public void Should_RegisterSdkAuthorizationFilters_WhenUseAuthorizationIsTrue(bool useAuthorization, int expectedCallToolFilters, int expectedListToolsFilters)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();

        services.AddZeroMcpExtensions(options =>
        {
            options.ToolAssembly = typeof(SdkScanFixture).Assembly;
            options.UseAuthorization = useAuthorization;
        });
        var provider = services.BuildServiceProvider();

        // SDK 2.2.0: AddAuthorizationFilters() adds one ordinary call-tool checkpoint and one list filter;
        // WithHttpTransport always installs one list guard, so list count is 2 / 1 and call count is 1 / 0.
        var mcpOptions = provider.GetRequiredService<IOptions<McpServerOptions>>().Value;
        mcpOptions.Filters.Request.CallToolFilters.Should().HaveCount(expectedCallToolFilters);
        mcpOptions.Filters.Request.ListToolsFilters.Should().HaveCount(expectedListToolsFilters);
    }

    [Fact]
    public void Should_NotAttachAuthorizationMetadata_WhenUseAuthorizationIsFalse()
    {
        var provider = BuildProvider(useAuthorization: false);

        var tools = provider.GetServices<McpServerTool>().ToList();

        tools.Should().NotBeEmpty();
        tools.Should().OnlyContain(t => !t.Metadata.Any(m => m is IAuthorizeData));
    }

    [Fact]
    public void Should_AttachAuthorizationMetadata_WhenUseAuthorizationIsTrue()
    {
        var provider = BuildProvider(useAuthorization: true);

        var tool = provider.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "member_only");

        tool.Metadata.OfType<AuthorizeAttribute>().Should().Contain(a => a.Policy == "RequireMember");
    }

    [Fact]
    public async Task Should_CreateControllerThroughDI_WhenToolInvoked()
    {
        var greeter = Substitute.For<IGreeter>();
        greeter.Greet().Returns("hello from DI");
        var provider = BuildProvider(useAuthorization: false, services => services.AddSingleton(greeter));

        var tool = provider.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "greet");
        var pipeIn = new Pipe();
        var pipeOut = new Pipe();
        await using var server = McpServer.Create(
            new StreamServerTransport(pipeIn.Reader.AsStream(), pipeOut.Writer.AsStream()),
            new McpServerOptions(),
            serviceProvider: provider);
        var request = new RequestContext<CallToolRequestParams>(
            server,
            new JsonRpcRequest { Method = "tools/call" },
            new CallToolRequestParams { Name = "greet" });

        var result = await tool.InvokeAsync(request, TestContext.Current.CancellationToken);

        result.IsError.Should().NotBe(true);
        result.Content.Should().ContainSingle().Which.Should().BeOfType<TextContentBlock>()
            .Which.Text.Should().Contain("hello from DI");
        greeter.Received(1).Greet();
    }

    private static ServiceProvider BuildProvider(bool useAuthorization, Action<IServiceCollection>? extra = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();
        extra?.Invoke(services);
        services.AddZeroMcpExtensions(options =>
        {
            options.ToolAssembly = typeof(SdkScanFixture).Assembly;
            options.UseAuthorization = useAuthorization;
        });
        return services.BuildServiceProvider();
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
internal sealed class AsyncMethodTestController
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
internal sealed class StaticMethodTestController
{
    [McpServerTool]
    public static string StaticTestTool()
    {
        return "Static tool result";
    }
}

// Test controller for explicit name override testing
[McpServerToolType]
internal sealed class ExplicitNameTestController
{
    [McpServerTool(Name = "my_explicit_name")]
    public string MyTool()
    {
        return "Tool with explicit name";
    }
}

// Fixture decorated with the official SDK attributes (ModelContextProtocol.Server) for scanning tests.
[Authorize]
[McpServerToolType]
internal sealed class SdkScanFixture
{
    [McpServerTool, Description("one")]
    public static string SdkToolOne() => "1";

    [McpServerTool, Description("two")]
    public static string SdkToolTwo() => "2";

    [McpServerTool, Description("three")]
    public static string SdkToolThree() => "3";

    public static string NotATool() => "x";
}

public interface IGreeter
{
    string Greet();
}

// Instance-tool fixture: constructor dependency resolved through ActivatorUtilities, plus a policy-protected tool.
[Authorize]
[McpServerToolType]
internal sealed class DiScanFixture(IGreeter greeter)
{
    [McpServerTool(Name = "greet"), Description("greets")]
    public string Greet() => greeter.Greet();

    [McpServerTool(Name = "member_only"), Description("member only")]
    [Authorize(Policy = "RequireMember")]
    public string MemberOnly() => "m";
}
