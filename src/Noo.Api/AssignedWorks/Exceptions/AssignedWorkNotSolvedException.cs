using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.AssignedWorks.Exceptions;

/// <summary>
/// Error Code: ASSIGNED_WORK.NOT_SOLVED
/// Name: Задание не выполнено
/// Description: Это действие может быть выполнено только для выполненных заданий
/// </summary>
public class AssignedWorkNotSolvedException : NooException
{
    public AssignedWorkNotSolvedException() : base("Assigned work is not solved.")
    {
        Id = "ASSIGNED_WORK.NOT_SOLVED";
        StatusCode = HttpStatusCode.Conflict;
    }
}
