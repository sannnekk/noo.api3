namespace Noo.Api.Core.System.Realtime.Ping;

/// <summary>
/// Method names are the wire names SignalR uses, so the <c>Async</c> suffix this codebase
/// requires shows up verbatim in the TypeScript contract too.
/// </summary>
public interface IRealtimePingClient
{
    public Task PongAsync(RealtimePong pong);
}
