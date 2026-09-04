using System.Collections.Concurrent;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Core.System.Realtime;

/// <summary>
/// Who is connected to <em>this</em> instance, and how many times. Deliberately per-instance and
/// in memory: it exists so the presence heartbeat knows whom to refresh, not to answer "is user
/// X online" — that question is answered from the shared cache, across the whole fleet.
/// </summary>
public sealed class RealtimeConnectionRegistry
{
    private readonly ConcurrentDictionary<Ulid, ConnectedUser> _users = new();

    public IReadOnlyCollection<ConnectedUser> Connected => _users.Values.ToArray();

    public int UserCount => _users.Count;

    public void Add(Ulid userId, UserRoles role)
    {
        if (userId == Ulid.Empty)
        {
            return;
        }

        // A user with several tabs open is one entry with a count, so closing one tab does not
        // mark them offline while the others are still connected.
        _users.AddOrUpdate(
            userId,
            _ => new ConnectedUser(userId, role, 1),
            (_, existing) => existing with { Connections = existing.Connections + 1 }
        );
    }

    public void Remove(Ulid userId)
    {
        if (userId == Ulid.Empty)
        {
            return;
        }

        while (_users.TryGetValue(userId, out var existing))
        {
            if (existing.Connections <= 1)
            {
                if (_users.TryRemove(new KeyValuePair<Ulid, ConnectedUser>(userId, existing)))
                {
                    return;
                }

                continue;
            }

            var decremented = existing with { Connections = existing.Connections - 1 };

            if (_users.TryUpdate(userId, decremented, existing))
            {
                return;
            }
        }
    }

    public record ConnectedUser(Ulid UserId, UserRoles Role, int Connections);
}
