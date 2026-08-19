using Noo.Api.AssignedWorks.DTO;
using Noo.Api.AssignedWorks.Filters;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.Core.DataAbstraction.Db;

namespace Noo.Api.AssignedWorks.Services;

/// <summary>
/// The work as a record: handing one out, reading it, listing it, putting it away.
/// What happens <em>to</em> it while it is being solved and checked lives in
/// <see cref="IAssignedWorkLifecycleService"/>, <see cref="IAssignedWorkEditingService"/>
/// and <see cref="IAssignedWorkMentorService"/>.
/// </summary>
public interface IAssignedWorkService
{
    public Task<Ulid> CreateAsync(Ulid workAssignmentId);
    public Task<AssignedWorkModel?> GetAsync(Ulid assignedWorkId);
    public Task<List<AssignedWorkModel>> GetByWorkAssignmentAsync(Ulid workAssignmentId);
    public Task<SearchResult<AssignedWorkModel>> GetAssignedWorksAsync(AssignedWorkFilter filter);
    public Task<AssignedWorksMetadataDTO> GetMetadataAsync(Ulid userId);
    public Task<Ulid> RemakeAsync(Ulid assignedWorkId, RemakeAssignedWorkOptionsDTO options);
    public Task ArchiveAsync(Ulid assignedWorkId);
    public Task UnarchiveAsync(Ulid assignedWorkId);
    public Task DeleteAsync(Ulid assignedWorkId);
}
