using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.AssignedWorks.Exceptions;

/// <summary>
/// Error Code: ASSIGNED_WORK.NOT_CHECKED
/// Name: Задание не проверено
/// Description: Это действие может быть выполнено только для проверенных заданий
/// </summary>
public class AssignedWorkNotCheckedException : NooException
{
    public AssignedWorkNotCheckedException() : base("Assigned work is not checked.")
    {
        Id = "ASSIGNED_WORK.NOT_CHECKED";
        StatusCode = HttpStatusCode.Conflict;
    }
}
