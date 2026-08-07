using Ardalis.Specification;
using Noo.Api.SavedTasks.Models;

namespace Noo.Api.SavedTasks.Specifications;

/// <summary>
/// Scopes the saved tasks to their owner and pulls in the context they are read
/// with: the task, the work it belongs to and that work's subject.
/// </summary>
public class SavedTaskSpecification : Specification<SavedTaskModel>
{
    public SavedTaskSpecification(Ulid userId, string? search = null)
    {
        Query.Where(savedTask => savedTask.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();

            Query.Where(savedTask =>
                savedTask.Task.Work!.Title.ToLower().Contains(term)
                || savedTask.Task.Work!.Subject!.Name.ToLower().Contains(term)
            );
        }

        Query
            .Include(savedTask => savedTask.Task)
            .ThenInclude(task => task.Work!)
            .ThenInclude(work => work.Subject);
    }
}
