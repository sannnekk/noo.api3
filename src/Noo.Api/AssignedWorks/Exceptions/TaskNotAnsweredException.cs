using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.AssignedWorks.Exceptions;

/// <summary>
/// Error Code: ASSIGNED_WORK.TASK_NOT_ANSWERED
/// Name: Задание не решено
/// Description: Нечего проверять: на это задание ещё нет ответа
/// </summary>
public class TaskNotAnsweredException : NooException
{
    public TaskNotAnsweredException()
        : base("There is no answer to this task to check.")
    {
        Id = "ASSIGNED_WORK.TASK_NOT_ANSWERED";
        StatusCode = HttpStatusCode.Conflict;
    }
}
