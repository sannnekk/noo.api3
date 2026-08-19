using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.AssignedWorks.Exceptions;

/// <summary>
/// Error Code: ASSIGNED_WORK.TASK_ANSWER_KEY_NOT_OFFERED
/// Name: Ответ на это задание не показывается
/// Description: Правильный ответ можно посмотреть только у заданий, где это разрешено
/// </summary>
public class TaskAnswerKeyNotOfferedException : NooException
{
    public TaskAnswerKeyNotOfferedException()
        : base("This task does not offer its answer key before checking.")
    {
        Id = "ASSIGNED_WORK.TASK_ANSWER_KEY_NOT_OFFERED";
        StatusCode = HttpStatusCode.Conflict;
    }
}
