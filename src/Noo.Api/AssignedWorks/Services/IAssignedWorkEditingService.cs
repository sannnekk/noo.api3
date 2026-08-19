using Noo.Api.AssignedWorks.DTO;

namespace Noo.Api.AssignedWorks.Services;

/// <summary>
/// The writes that fill a work in: the student's answers, and the mentors' scores and
/// comments on top of them.
/// </summary>
public interface IAssignedWorkEditingService
{
    public Task<Ulid> SaveAnswerAsync(Ulid assignedWorkId, UpsertAssignedWorkAnswerDTO answer);
    public Task<Ulid> SaveCommentAsync(Ulid assignedWorkId, UpsertAssignedWorkCommentDTO comment);
}
