using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Request;
using Noo.Api.Core.Response;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Collaboration.Realtime;
using Noo.Api.Core.System.Realtime;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.Users.Services;
using ProducesAttribute = Noo.Api.Core.Documentation.ProducesAttribute;

namespace Noo.Api.Core.System.Collaboration;

/// <summary>
/// The parts of collaborative editing that belong on HTTP rather than the hub.
///
/// Fetching the draft is here because it is a bulk read that would otherwise have to fit inside a
/// hub message limit sized for the small frames the hub actually carries. Saving is here because
/// it is a write to MySQL, and only requests through the MVC pipeline get the unit of work, the
/// exception translation and the media presigning that hub invocations skip entirely.
/// </summary>
[ApiVersion(NooApiVersions.Current)]
[ApiController]
[Route("collaboration")]
[Authorize(Policy = CollaborationPolicies.CanCollaborate)]
public class CollaborationController : ApiController
{
    private readonly ICollaborationStore _store;
    private readonly CollaborationRoomHandlerRegistry _handlers;
    private readonly IRealtimePublisher<ICollaborationHubClient> _publisher;
    private readonly ICurrentUser _currentUser;
    private readonly IUserService _users;

    public CollaborationController(
        IMapper mapper,
        ICollaborationStore store,
        CollaborationRoomHandlerRegistry handlers,
        IRealtimePublisher<ICollaborationHubClient> publisher,
        ICurrentUser currentUser,
        IUserService users
    )
        : base(mapper)
    {
        _store = store;
        _handlers = handlers;
        _publisher = publisher;
        _currentUser = currentUser;
        _users = users;
    }

    /// <summary>
    /// The shared draft, on top of the entity the client fetched separately. Returns the whole op
    /// log, which is why this is not a hub call.
    /// </summary>
    [HttpGet("{roomType}/{roomId}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Produces(
        typeof(ApiResponseDTO<CollaborationRoomState>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetRoomAsync(
        [FromRoute] string roomType,
        [FromRoute] Ulid roomId
    )
    {
        var room = await RequireEditableRoomAsync(roomType, roomId);
        var log = await _store.GetOpsAsync(room);

        return SendResponse(
            new CollaborationRoomState
            {
                Seq = log.Seq,
                Version = await _store.GetVersionAsync(room),
                Members = await _store.GetMembersAsync(room),
                Leases = await _store.GetLeasesAsync(room),
                Ops = log.Ops,
            }
        );
    }

    /// <summary>
    /// Applies the accumulated draft to the entity through its own patch pipeline, then clears it.
    ///
    /// <c>expectedSeq</c> is what the saver had applied when it pressed Save. Refusing a save that
    /// does not match is what stops an operation still in flight from being dropped: the client
    /// catches up and saves again, rather than silently discarding someone's edit.
    /// </summary>
    [HttpPost("{roomType}/{roomId}/save")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Produces(
        typeof(ApiResponseDTO<CollaborationSaved>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status409Conflict
    )]
    public async Task<IActionResult> SaveAsync(
        [FromRoute] string roomType,
        [FromRoute] Ulid roomId,
        [FromBody] CollaborationSaveRequestDTO request
    )
    {
        var room = await RequireEditableRoomAsync(roomType, roomId);
        var log = await _store.GetOpsAsync(room);

        if (request.ExpectedSeq != log.Seq)
        {
            throw new ConflictException(
                "Someone edited this while you were saving. Reload and save again."
            );
        }

        if (log.Ops.Count == 0)
        {
            throw new BadRequestException("There is nothing to save.");
        }

        await _handlers.Resolve(roomType).SaveAsync(roomId, log.Ops, HttpContext.RequestAborted);

        var userId = _currentUser.RequireUserId();
        var user = await _users.GetUserByIdAsync(userId);

        var saved = new CollaborationSaved
        {
            Version = await _store.ClearDraftAsync(room),
            SavedBy = userId,
            SavedByName = user?.Name ?? "—",
            SavedAt = Clock.Now,
        };

        // Everyone, the saver included: the entity now differs from what any of them holds,
        // because the server settles task numbering and the total score rather than taking them.
        await _publisher.SendToGroupAsync(
            room.GroupName,
            client => client.DocumentSavedAsync(saved)
        );

        return SendResponse(saved);
    }

    /// <summary>
    /// Throws the shared draft away, leaving the saved entity as it stands. The way out when a
    /// room's draft is wrong and nobody wants it applied.
    /// </summary>
    [HttpDelete("{roomType}/{roomId}/draft")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Produces(
        typeof(void),
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> DiscardDraftAsync(
        [FromRoute] string roomType,
        [FromRoute] Ulid roomId
    )
    {
        var room = await RequireEditableRoomAsync(roomType, roomId);

        await _store.ClearDraftAsync(room);
        await _publisher.SendToGroupAsync(
            room.GroupName,
            client => client.ResyncRequiredAsync("discarded")
        );

        return SendResponse();
    }

    private async Task<CollaborationRoom> RequireEditableRoomAsync(string roomType, Ulid roomId)
    {
        var handler = _handlers.Resolve(roomType);

        if (!await handler.CanEditAsync(roomId, HttpContext.RequestAborted))
        {
            throw new ForbiddenException("You may not edit this document.");
        }

        return new CollaborationRoom(handler.RoomType, roomId);
    }
}
