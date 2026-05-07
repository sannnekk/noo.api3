using MediatR;

namespace Noo.Api.AssignedWorks.Events;

public record HelperMentorRemovedEvent(Ulid RemovedMentorId, Ulid AssignedWorkId) : INotification;

public record HelperMentorAddedEvent(Ulid HelperMentorId, Ulid AssignedWorkId) : INotification;

public record AssignedWorkCheckedEvent(Ulid AssignedWorkId, Ulid CheckedBy) : INotification;

public record AssignedWorkSolvedEvent(Ulid AssignedWorkId) : INotification;

public record MainMentorChangedEvent(
    Ulid? OldMentorId,
    Ulid NewMentorId,
    Ulid AssignedWorkId,
    bool NotifyMentor,
    bool NotifyStudent
) : INotification;

public record AssignedWorkCheckDeadlineShiftedEvent(
    Ulid AssignedWorkId,
    Ulid ShiftedById,
    bool NotifyOthers
) : INotification;

public record AssignedWorkSolveDeadlineShiftedEvent(Ulid AssignedWorkId, bool NotifyOthers)
    : INotification;

public record AssignedWorkReturnedToCheckEvent(Ulid AssignedWorkId) : INotification;

public record AssignedWorkReturnedToSolveEvent(Ulid AssignedWorkId) : INotification;
