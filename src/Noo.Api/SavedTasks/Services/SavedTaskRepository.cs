using Microsoft.EntityFrameworkCore;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.SavedTasks.DTO;
using Noo.Api.SavedTasks.Models;

namespace Noo.Api.SavedTasks.Services;

[RegisterScoped(typeof(ISavedTaskRepository))]
public class SavedTaskRepository : Repository<SavedTaskModel>, ISavedTaskRepository
{
    public SavedTaskRepository(NooDbContext context)
        : base(context) { }

    public Task<SavedTaskModel?> GetAsync(Ulid userId, Ulid taskId)
    {
        return Context
            .GetDbSet<SavedTaskModel>()
            .FirstOrDefaultAsync(savedTask =>
                savedTask.UserId == userId && savedTask.TaskId == taskId
            );
    }

    public async Task<IEnumerable<SavedTaskReferenceDTO>> GetReferencesAsync(
        Ulid userId,
        Ulid? assignedWorkId
    )
    {
        return await Context
            .GetDbSet<SavedTaskModel>()
            .Where(savedTask =>
                savedTask.UserId == userId
                && (assignedWorkId == null || savedTask.AssignedWorkId == assignedWorkId)
            )
            .Select(savedTask => new SavedTaskReferenceDTO
            {
                Id = savedTask.Id,
                TaskId = savedTask.TaskId,
            })
            .ToListAsync();
    }

    public Task<AssignedWorkModel?> GetSavableWorkAsync(
        Ulid studentId,
        Ulid assignedWorkId,
        Ulid taskId
    )
    {
        return Context
            .GetDbSet<AssignedWorkModel>()
            .AsNoTracking()
            .FirstOrDefaultAsync(assignedWork =>
                assignedWork.Id == assignedWorkId
                && assignedWork.StudentId == studentId
                && assignedWork.Work!.Tasks!.Any(task => task.Id == taskId)
            );
    }
}
