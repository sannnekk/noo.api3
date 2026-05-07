using Noo.Api.Core.Exceptions;

namespace Noo.Api.AssignedWorks.Exceptions;

/// <summary>
/// Error Code: NO_MENTOR_ASSIGNED
/// Name: Не назначен наставник
/// Description: Для выполнения этого задания требуется наставник
/// </summary>
public class NoMentorAssignedException : NooException
{
    public NoMentorAssignedException(
        string message =
            "The user has no mentor assigned for this subject and the work requires a mentor"
    )
        : base(message)
    {
        Id = "NO_MENTOR_ASSIGNED";
        StatusCode = System.Net.HttpStatusCode.BadRequest;
    }
}
