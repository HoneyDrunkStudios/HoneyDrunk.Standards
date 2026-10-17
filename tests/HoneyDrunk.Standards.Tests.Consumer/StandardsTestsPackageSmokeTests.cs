using AwesomeAssertions;
using NSubstitute;
using Xunit;

namespace HoneyDrunk.Standards.Tests.Consumer;

public sealed class StandardsTestsPackageSmokeTests
{
    public interface ISmokeClock
    {
        DateTimeOffset UtcNow { get; }
    }

    [Fact]
    public void StandardsTestsPackage_Provides_TestStack_Dependencies()
    {
        var clock = Substitute.For<ISmokeClock>();
        clock.UtcNow.Returns(DateTimeOffset.UnixEpoch);

        clock.UtcNow.Should().Be(DateTimeOffset.UnixEpoch);
        _ = clock.Received(1).UtcNow;
    }
}
