using Noo.Api.AssignedWorks.DTO;
using Noo.Api.AssignedWorks.Filters;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.Core.DataAbstraction.Db;

namespace Noo.Api.AssignedWorks.Services;

public interface IAssignedWorkService
{
    public Task<Ulid> CreateAsync(Ulid workAssignmentId);
    public Task<AssignedWorkModel?> GetAsync(Ulid assignedWorkId);
    public Task<List<AssignedWorkModel>> GetByWorkAssignmentAsync(Ulid workAssignmentId);
    public Task<Ulid> RemakeAsync(Ulid assignedWorkId, RemakeAssignedWorkOptionsDTO options);
    public Task<Ulid> SaveAnswerAsync(Ulid assignedWorkId, UpsertAssignedWorkAnswerDTO answer);
    public Ulid SaveComment(Ulid assignedWorkId, UpsertAssignedWorkCommentDTO comment);
    public Task MarkAsSolvedAsync(Ulid assignedWorkId);
    public Task MarkAsCheckedAsync(Ulid assignedWorkId);
    public Task ArchiveAsync(Ulid assignedWorkId);
    public Task UnarchiveAsync(Ulid assignedWorkId);
    public Task AddHelperMentorAsync(Ulid assignedWorkId, AddHelperMentorOptionsDTO options);
    public Task ReplaceMainMentorAsync(Ulid assignedWorkId, ReplaceMainMentorOptionsDTO options);
    public Task ShiftDeadlineAsync(
        Ulid assignedWorkId,
        ShiftAssignedWorkDeadlineOptionsDTO options
    );
    public Task ReturnToSolveAsync(Ulid assignedWorkId);
    public Task ReturnToCheckAsync(Ulid assignedWorkId);
    public Task DeleteAsync(Ulid assignedWorkId);
    public Task<SearchResult<AssignedWorkModel>> GetAssignedWorksAsync(AssignedWorkFilter filter);
}
