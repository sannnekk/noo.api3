using Noo.Api.Core.System.Events;
using Noo.Api.UserHistory.Services;
using Noo.Api.UserHistory.Types;
using Noo.Api.Users.Services;

namespace Noo.Api.Users.Events;

public sealed record MentorAssignedEvent(
    Ulid StudentId,
    Ulid MentorId,
    Ulid SubjectId,
    Ulid? AssignedById
) : IDomainEvent;

public sealed class MentorAssignedUserHistoryHandler : IEventHandler<MentorAssignedEvent>
{
    private readonly IUserHistoryService _userHistoryService;
    private readonly IUserRepository _userRepository;

    public MentorAssignedUserHistoryHandler(
        IUserHistoryService userHistoryService,
        IUserRepository userRepository
    )
    {
        _userHistoryService = userHistoryService;
        _userRepository = userRepository;
    }

    public async Task HandleAsync(MentorAssignedEvent @event, CancellationToken ct = default)
    {
        var mentor = await _userRepository.GetByIdAsync(@event.MentorId);

        _userHistoryService.Record(
            @event.StudentId,
            @event.AssignedById,
            UserHistoryType.MentorAssigned,
            new Dictionary<string, string>
            {
                ["mentorId"] = @event.MentorId.ToString(),
                ["mentorName"] = mentor?.Name ?? string.Empty,
                ["subjectId"] = @event.SubjectId.ToString(),
            }
        );
    }
}
