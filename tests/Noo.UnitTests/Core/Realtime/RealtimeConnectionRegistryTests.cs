using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Realtime;

namespace Noo.UnitTests.Core.Realtime;

public class RealtimeConnectionRegistryTests
{
    [Fact]
    public void TracksAConnectedUser()
    {
        var registry = new RealtimeConnectionRegistry();
        var userId = Ulid.NewUlid();

        registry.Add(userId, UserRoles.Student);

        var connected = Assert.Single(registry.Connected);
        Assert.Equal(userId, connected.UserId);
        Assert.Equal(UserRoles.Student, connected.Role);
    }

    // Several tabs is one user. Closing one must not make them look offline while the rest of
    // their tabs are still holding sockets.
    [Fact]
    public void KeepsAUserWhileAnyOfTheirConnectionsRemain()
    {
        var registry = new RealtimeConnectionRegistry();
        var userId = Ulid.NewUlid();

        registry.Add(userId, UserRoles.Student);
        registry.Add(userId, UserRoles.Student);
        registry.Remove(userId);

        Assert.Single(registry.Connected);

        registry.Remove(userId);

        Assert.Empty(registry.Connected);
    }

    [Fact]
    public void IgnoresAnonymousConnections()
    {
        var registry = new RealtimeConnectionRegistry();

        registry.Add(Ulid.Empty, UserRoles.Student);

        Assert.Empty(registry.Connected);
    }

    [Fact]
    public void ToleratesRemovingSomeoneWhoWasNeverAdded()
    {
        var registry = new RealtimeConnectionRegistry();

        registry.Remove(Ulid.NewUlid());
        registry.Remove(Ulid.Empty);

        Assert.Empty(registry.Connected);
    }

    [Fact]
    public void CountsEachUserOnceRegardlessOfConnectionCount()
    {
        var registry = new RealtimeConnectionRegistry();
        var first = Ulid.NewUlid();
        var second = Ulid.NewUlid();

        registry.Add(first, UserRoles.Teacher);
        registry.Add(first, UserRoles.Teacher);
        registry.Add(second, UserRoles.Mentor);

        Assert.Equal(2, registry.UserCount);
    }

    [Fact]
    public async Task SurvivesConcurrentConnectAndDisconnect()
    {
        var registry = new RealtimeConnectionRegistry();
        var userId = Ulid.NewUlid();

        await Task.WhenAll(
            Enumerable
                .Range(0, 200)
                .Select(_ =>
                    Task.Run(() =>
                    {
                        registry.Add(userId, UserRoles.Student);
                        registry.Remove(userId);
                    })
                )
        );

        Assert.Empty(registry.Connected);
    }
}
