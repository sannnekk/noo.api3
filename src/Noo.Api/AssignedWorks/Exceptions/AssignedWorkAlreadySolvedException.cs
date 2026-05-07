using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.AssignedWorks.Exceptions;

/// <summary>
/// Error Code: ASSIGNED_WORK.ALREADY_SOLVED
/// Name: Задание уже выполнено
/// Description: Это действие может быть выполнено только для невыполненных заданий
/// </summary>
public class AssignedWorkAlreadySolvedException : NooException
{
    public AssignedWorkAlreadySolvedException()
        : base("The assigned work is already solved.")
    {
        Id = "ASSIGNED_WORK.ALREADY_SOLVED";
        StatusCode = HttpStatusCode.Conflict;
    }
}
