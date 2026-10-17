using AwesomeAssertions;
using Consumer.Sample;
using NSubstitute;
using Xunit;

namespace HoneyDrunk.Standards.Tests;

public sealed class StandardsTestStackSmokeTests
{
    [Fact]
    public async Task StandardsTestsProject_Uses_Canonical_TestStack()
    {
        var logger = Substitute.For<ILogger>();
        var demo = new StandardsDemo("Grid", logger);
        var user = new User(1, "grid@example.com");

        var greeting = await demo.GetGreetingAsync(CancellationToken.None);

        demo.Name.Should().Be("Grid");
        demo.IsValidName().Should().BeTrue();
        greeting.Should().Be("Hello, Grid!");
        user.Id.Should().Be(1);
        user.Email.Should().Be("grid@example.com");
        Action invalidUser = () => _ = new User(0, "grid@example.com");
        invalidUser.Should().Throw<ArgumentException>();
        logger.Received(1).LogInformation("Generating greeting for {Name}", "Grid");
    }
}
