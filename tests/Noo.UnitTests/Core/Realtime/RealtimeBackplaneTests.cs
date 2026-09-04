using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Noo.Api.Core.DataAbstraction.Cache;
using Noo.Api.Core.System.Realtime;
using Noo.Api.Core.System.Realtime.Backplane;
using StackExchange.Redis;

namespace Noo.UnitTests.Core.Realtime;

public class RealtimeBackplaneTests
{
    private static RealtimeBackplane Create(
        string? connectionString,
        IRedisConnectionFactory? factory = null
    ) =>
        new(
            Options.Create(new RealtimeConfig { BackplaneConnectionString = connectionString }),
            factory ?? new Mock<IRedisConnectionFactory>().Object,
            NullLogger<RealtimeBackplane>.Instance
        );

    [Fact]
    public void IsNotConfigured_WhenNoConnectionStringIsSet()
    {
        Assert.False(Create(null).IsConfigured);
        Assert.False(Create("   ").IsConfigured);
    }

    [Fact]
    public void ThrowsRatherThanDegrading_WhenAskedForAnUnconfiguredConnection()
    {
        var backplane = Create(null);

        Assert.Throws<InvalidOperationException>(() => backplane.Connection);
    }

    // The cache connection deliberately falls back to memory when Redis is unreachable. The
    // backplane must not: a pod that quietly loses it keeps serving connections that no longer
    // receive anything published by the rest of the fleet.
    [Fact]
    public void SurfacesTheConnectionFailure_RatherThanFallingBack()
    {
        var factory = new Mock<IRedisConnectionFactory>();
        factory
            .Setup(f => f.Connect(It.IsAny<string>()))
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "nope"));

        var backplane = Create("localhost:6380", factory.Object);

        Assert.Throws<RedisConnectionException>(() => backplane.Connection);
    }

    [Fact]
    public void ConnectsOnce_AndReusesTheSameMultiplexer()
    {
        var multiplexer = new Mock<IConnectionMultiplexer>().Object;
        var factory = new Mock<IRedisConnectionFactory>();
        factory.Setup(f => f.Connect(It.IsAny<string>())).Returns(multiplexer);

        var backplane = Create("localhost:6380", factory.Object);

        Assert.Same(multiplexer, backplane.Connection);
        Assert.Same(multiplexer, backplane.Connection);
        factory.Verify(f => f.Connect(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HealthCheckIsHealthy_WhenNoBackplaneIsConfigured()
    {
        var check = new RealtimeBackplaneHealthCheck(Create(null));

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task HealthCheckIsUnhealthy_WhenTheBackplaneCannotBeReached()
    {
        var factory = new Mock<IRedisConnectionFactory>();
        factory
            .Setup(f => f.Connect(It.IsAny<string>()))
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "nope"));

        var check = new RealtimeBackplaneHealthCheck(Create("localhost:6380", factory.Object));

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }
}
