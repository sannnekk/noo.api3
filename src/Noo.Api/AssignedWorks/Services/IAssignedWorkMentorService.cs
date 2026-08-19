using Noo.Api.AssignedWorks.DTO;

namespace Noo.Api.AssignedWorks.Services;

/// <summary>
/// Who checks a work: the main mentor it is handed to, and the helper who may join them.
/// </summary>
public interface IAssignedWorkMentorService
{
    public Task AddHelperMentorAsync(Ulid assignedWorkId, AddHelperMentorOptionsDTO options);
    public Task ReplaceMainMentorAsync(Ulid assignedWorkId, ReplaceMainMentorOptionsDTO options);
}
