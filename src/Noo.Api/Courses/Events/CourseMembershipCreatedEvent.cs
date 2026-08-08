using Noo.Api.Core.System.Events;
using Noo.Api.Courses.Services;
using Noo.Api.UserHistory.Services;
using Noo.Api.UserHistory.Types;

namespace Noo.Api.Courses.Events;

public sealed record CourseMembershipCreatedEvent(
    Ulid MembershipId,
    Ulid StudentId,
    Ulid CourseId,
    Ulid? AssignerId
) : IDomainEvent;

public sealed class CourseMembershipCreatedUserHistoryHandler
    : IEventHandler<CourseMembershipCreatedEvent>
{
    private readonly IUserHistoryService _userHistoryService;
    private readonly ICourseRepository _courseRepository;

    public CourseMembershipCreatedUserHistoryHandler(
        IUserHistoryService userHistoryService,
        ICourseRepository courseRepository
    )
    {
        _userHistoryService = userHistoryService;
        _courseRepository = courseRepository;
    }

    public async Task HandleAsync(
        CourseMembershipCreatedEvent @event,
        CancellationToken ct = default
    )
    {
        // Snapshot the name rather than the id alone: the entry has to stay readable after the
        // course is renamed or removed.
        var course = await _courseRepository.GetByIdAsync(@event.CourseId);

        _userHistoryService.Record(
            @event.StudentId,
            @event.AssignerId,
            UserHistoryType.AddedToCourse,
            new Dictionary<string, string>
            {
                ["courseId"] = @event.CourseId.ToString(),
                ["courseName"] = course?.Name ?? string.Empty,
            }
        );
    }
}
