using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.AssignedWorks.Exceptions;

/// <summary>
/// Error Code: ASSIGNED_WORK.TASK_NOT_CHECKABLE_ON_ITS_OWN
/// Name: Задание нельзя проверить по отдельности
/// Description: Это задание проверяется вместе со всей работой
/// </summary>
public class TaskNotCheckableOnItsOwnException : NooException
{
    public TaskNotCheckableOnItsOwnException()
        : base("This task is not checked on its own.")
    {
        Id = "ASSIGNED_WORK.TASK_NOT_CHECKABLE_ON_ITS_OWN";
        StatusCode = HttpStatusCode.Conflict;
    }
}
