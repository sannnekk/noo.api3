namespace Noo.Api.Core.System.Realtime;

/// <summary>
/// How application code pushes to connected clients. Domain services depend on this rather than
/// <c>IHubContext</c> so they carry no SignalR types and can be faked in a test.
/// </summary>
/// <typeparam name="TClient">
/// The hub's client contract. Sends are expressed against it — <c>client =&gt; client.FooAsync(x)</c>
/// — so a renamed or wrongly-typed message is a compile error rather than a silently dropped one.
/// </typeparam>
public interface IRealtimePublisher<TClient>
    where TClient : class
{
    public Task SendToUserAsync(
        Ulid userId,
        Func<TClient, Task> send,
        CancellationToken ct = default
    );

    public Task SendToUsersAsync(
        IReadOnlyCollection<Ulid> userIds,
        Func<TClient, Task> send,
        CancellationToken ct = default
    );

    public Task SendToGroupAsync(
        string group,
        Func<TClient, Task> send,
        CancellationToken ct = default
    );

    public Task BroadcastAsync(Func<TClient, Task> send, CancellationToken ct = default);
}
