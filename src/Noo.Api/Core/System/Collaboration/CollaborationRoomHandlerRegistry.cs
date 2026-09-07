using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.System.Collaboration;

/// <summary>
/// Resolves the handler for a room type coming off the wire. Registered handlers are discovered
/// through DI, so a module opts its entity into collaborative editing by registering one class.
/// </summary>
[RegisterScoped]
public class CollaborationRoomHandlerRegistry
{
    private readonly IReadOnlyDictionary<string, ICollaborationRoomHandler> _handlers;

    public CollaborationRoomHandlerRegistry(IEnumerable<ICollaborationRoomHandler> handlers)
    {
        _handlers = handlers.ToDictionary(
            handler => handler.RoomType,
            StringComparer.OrdinalIgnoreCase
        );
    }

    /// <summary>
    /// A room type nobody registered is a client asking for a document that cannot be edited
    /// together, which is a bad request rather than a missing page.
    /// </summary>
    public ICollaborationRoomHandler Resolve(string roomType) =>
        _handlers.TryGetValue(roomType, out var handler)
            ? handler
            : throw new BadRequestException($"Unknown collaboration room type '{roomType}'.");

    public bool Knows(string roomType) => _handlers.ContainsKey(roomType);
}
