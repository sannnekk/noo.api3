using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.SavedTasks.Exceptions;

/// <summary>
/// Error Code: SAVED_TASK.NOT_ENOUGH_FOR_QUIZ
/// Name: Недостаточно карточек
/// Description: Чтобы начать квиз, сохраните больше заданий по этому предмету
/// </summary>
public class NotEnoughSavedTasksException : NooException
{
    public NotEnoughSavedTasksException()
        : base(
            $"At least {SavedTaskConfig.MinQuizCardCount} saved tasks are needed to start a quiz."
        )
    {
        Id = "SAVED_TASK.NOT_ENOUGH_FOR_QUIZ";
        StatusCode = HttpStatusCode.Conflict;
    }
}
