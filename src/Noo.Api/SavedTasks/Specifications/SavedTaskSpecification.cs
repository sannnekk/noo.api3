using Ardalis.Specification;
using Noo.Api.SavedTasks.Models;

namespace Noo.Api.SavedTasks.Specifications;

/// <summary>
/// Scopes the saved tasks to their owner and pulls in the context they are read
/// with: the task, the work it belongs to and that work's subject.
/// </summary>
public class SavedTaskSpecification : Specification<SavedTaskModel>
{
    public SavedTaskSpecification(
        Ulid userId,
        string? search = null,
        IEnumerable<Ulid>? subjectIds = null
    )
    {
        Query.Where(savedTask => savedTask.UserId == userId);

        // Compared as nullable so a work with no subject takes part in the
        // comparison rather than needing a null check of its own.
        var subjects = subjectIds?.Cast<Ulid?>().ToList();

        if (subjects is { Count: > 0 })
        {
            Query.Where(savedTask => subjects.Contains(savedTask.Task.Work!.SubjectId));
        }

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
