using AwesomeAssertions;
using Xunit;

namespace Zero.Mcp.Extensions.Tests;

/// <summary>
/// Guards the test toolchain itself: xunit.v3 runner and AwesomeAssertions (Apache 2.0, namespace AwesomeAssertions since 9.x).
/// </summary>
public class TestStackSmokeTests
{
    [Fact]
    public void Should_RunUnderXunitV3_WhenExecuted()
    {
        typeof(FactAttribute).Assembly.GetName().Name.Should().Be("xunit.v3.core");
    }

    [Fact]
    public void Should_UseAwesomeAssertions_WhenAsserting()
    {
        typeof(AwesomeAssertions.AssertionExtensions).Assembly.GetName().Name.Should().Be("AwesomeAssertions");
    }
}
