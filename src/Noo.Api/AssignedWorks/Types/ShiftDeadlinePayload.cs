using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.AssignedWorks.Types;

public struct ShiftDeadlinePayload
{
    public DateTime NewDeadlineAt { get; init; }

    public UserRoles ShiftedByRole { get; init; }

    public Ulid ShiftedById { get; init; }
}
