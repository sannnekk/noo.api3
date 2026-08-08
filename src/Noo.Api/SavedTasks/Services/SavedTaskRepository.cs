using Microsoft.EntityFrameworkCore;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.SavedTasks.DTO;
using Noo.Api.SavedTasks.Models;
using Noo.Api.Subjects.DTO;

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

    public async Task<IEnumerable<SavedTaskSubjectDTO>> GetSubjectSummariesAsync(Ulid userId)
    {
        var summaries = await OwnedBy(userId)
            .GroupBy(savedTask => savedTask.Task.Work!.Subject)
            .Select(group => new { Subject = group.Key, Count = group.Count() })
            .ToListAsync();

        return summaries
            .Select(summary => new SavedTaskSubjectDTO
            {
                Subject =
                    summary.Subject == null
                        ? null
                        : new SubjectDTO
                        {
                            Id = summary.Subject.Id,
                            Name = summary.Subject.Name,
                            Color = summary.Subject.Color,
                            CreatedAt = summary.Subject.CreatedAt,
                            UpdatedAt = summary.Subject.UpdatedAt,
                        },
                SavedTaskCount = summary.Count,
            })
            .OrderByDescending(summary => summary.SavedTaskCount)
            .ToList();
    }

    public Task<int> CountAsync(Ulid userId, Ulid? subjectId)
    {
        return OnSubject(OwnedBy(userId), subjectId).CountAsync();
    }

    public async Task<IEnumerable<SavedTaskModel>> GetRandomAsync(
        Ulid userId,
        Ulid? subjectId,
        int count
    )
    {
        return await OnSubject(OwnedBy(userId), subjectId)
            .Include(savedTask => savedTask.Task)
            .ThenInclude(task => task.Work!)
            .ThenInclude(work => work.Subject)
            .OrderBy(_ => EF.Functions.Random())
            .Take(count)
            .ToListAsync();
    }

    public Task<SavedTaskModel?> GetWithTaskAsync(Ulid userId, Ulid savedTaskId)
    {
        return OwnedBy(userId)
            .Include(savedTask => savedTask.Task)
            .FirstOrDefaultAsync(savedTask => savedTask.Id == savedTaskId);
    }

    private IQueryable<SavedTaskModel> OwnedBy(Ulid userId)
    {
        return Context.GetDbSet<SavedTaskModel>().Where(savedTask => savedTask.UserId == userId);
    }

    private static IQueryable<SavedTaskModel> OnSubject(
        IQueryable<SavedTaskModel> query,
        Ulid? subjectId
    )
    {
        return subjectId == null
            ? query
            : query.Where(savedTask => savedTask.Task.Work!.SubjectId == subjectId);
    }
}
