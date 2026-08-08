using Noo.Api.Core.System.Events;
using Noo.Api.UserHistory.Services;
using Noo.Api.UserHistory.Types;
using Noo.Api.Users.Services;

namespace Noo.Api.Users.Events;

public sealed record MentorUnassignedEvent(
    Ulid StudentId,
    Ulid MentorId,
    Ulid? SubjectId,
    Ulid? UnassignedById
) : IDomainEvent;

public sealed class MentorUnassignedUserHistoryHandler : IEventHandler<MentorUnassignedEvent>
{
    private readonly IUserHistoryService _userHistoryService;
    private readonly IUserRepository _userRepository;

    public MentorUnassignedUserHistoryHandler(
        IUserHistoryService userHistoryService,
        IUserRepository userRepository
    )
    {
        _userHistoryService = userHistoryService;
        _userRepository = userRepository;
    }

    public async Task HandleAsync(MentorUnassignedEvent @event, CancellationToken ct = default)
    {
        var mentor = await _userRepository.GetByIdAsync(@event.MentorId);

        var payload = new Dictionary<string, string>
        {
            ["mentorId"] = @event.MentorId.ToString(),
            ["mentorName"] = mentor?.Name ?? string.Empty,
        };

        if (@event.SubjectId.HasValue)
        {
            payload["subjectId"] = @event.SubjectId.Value.ToString();
        }

        _userHistoryService.Record(
            @event.StudentId,
            @event.UnassignedById,
            UserHistoryType.MentorUnassigned,
            payload
        );
    }
}
