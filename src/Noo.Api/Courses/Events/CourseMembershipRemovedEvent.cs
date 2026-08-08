using Noo.Api.Core.System.Events;
using Noo.Api.Courses.Services;
using Noo.Api.UserHistory.Services;
using Noo.Api.UserHistory.Types;

namespace Noo.Api.Courses.Events;

public sealed record CourseMembershipRemovedEvent(
    Ulid MembershipId,
    Ulid StudentId,
    Ulid CourseId,
    Ulid? RemovedById
) : IDomainEvent;

public sealed class CourseMembershipRemovedUserHistoryHandler
    : IEventHandler<CourseMembershipRemovedEvent>
{
    private readonly IUserHistoryService _userHistoryService;
    private readonly ICourseRepository _courseRepository;

    public CourseMembershipRemovedUserHistoryHandler(
        IUserHistoryService userHistoryService,
        ICourseRepository courseRepository
    )
    {
        _userHistoryService = userHistoryService;
        _courseRepository = courseRepository;
    }

    public async Task HandleAsync(
        CourseMembershipRemovedEvent @event,
        CancellationToken ct = default
    )
    {
        var course = await _courseRepository.GetByIdAsync(@event.CourseId);

        _userHistoryService.Record(
            @event.StudentId,
            @event.RemovedById,
            UserHistoryType.RemovedFromCourse,
            new Dictionary<string, string>
            {
                ["courseId"] = @event.CourseId.ToString(),
                ["courseName"] = course?.Name ?? string.Empty,
            }
        );
    }
}
