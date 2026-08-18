using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Works.Models;

namespace Noo.Api.AssignedWorks.Services;

public interface ITaskCheckService
{
    /// <summary>
    /// Checks the submitted answers against the task requirements and returns the total score
    /// of the automatically checkable tasks, along with whether every given task was one of
    /// them — in which case the work needs no manual check at all.
    /// </summary>
    public TaskCheckResult CheckTasks(
        IEnumerable<AssignedWorkAnswerModel> answers,
        IEnumerable<WorkTaskModel> tasks
    );

    /// <summary>
    /// Scores a single word answer against a task's answer key, or returns
    /// <c>null</c> when the task is not one that can be checked automatically.
    /// </summary>
    public int? CheckWord(WorkTaskModel task, string? word);
}
