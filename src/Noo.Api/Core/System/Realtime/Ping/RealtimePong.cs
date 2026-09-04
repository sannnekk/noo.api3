namespace Noo.Api.Core.System.Realtime.Ping;

/// <summary>
/// The answer to a ping. <c>UserId</c> is the caller as the server resolved it, so a client can
/// confirm the connection authenticated as who it expected.
/// </summary>
public record RealtimePong(string ConnectionId, string? UserId, DateTime ServerTime);
