using Noo.Api.AssignedWorks.Models;
using Noo.Api.Works.Models;

namespace Noo.Api.AssignedWorks.Services;

public interface ITaskCheckService
{
    /// <summary>
    /// Checks the submitted answers against the task requirements and returns a total score.
    /// Only the autmotically checkable tasks are checked
    /// </summary>
    public int CheckTasks(
        IEnumerable<AssignedWorkAnswerModel> answers,
        IEnumerable<WorkTaskModel> tasks
    );
}
