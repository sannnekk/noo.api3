using Microsoft.AspNetCore.SignalR;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Core.System.Realtime;

/// <summary>
/// Normalises the user identifier SignalR routes <c>Clients.User(...)</c> by, so that a caller
/// holding a <see cref="Ulid"/> addresses the same string the token was issued with.
/// </summary>
public class NooUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var userId = connection.User?.GetId() ?? Ulid.Empty;

        return userId == Ulid.Empty ? null : userId.ToString();
    }
}
