using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.AssignedWorks.Exceptions;

/// <summary>
/// Error Code: ASSIGNED_WORK.ALREADY_CHECKED
/// Name: Задание уже проверено
/// Description: Это действие может быть выполнено только для непроверенных заданий
/// </summary>
public class AssignedWorkAlreadyCheckedException : NooException
{
    public AssignedWorkAlreadyCheckedException()
        : base("The assigned work is already checked.")
    {
        Id = "ASSIGNED_WORK.ALREADY_CHECKED";
        StatusCode = HttpStatusCode.Conflict;
    }
}
