using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.AssignedWorks.Exceptions;

/// <summary>
/// Error Code: ASSIGNED_WORK.NOT_REMAKEABLE
/// Name: Задание невозможно переделать
/// Description: Это задание больше невозможно переделать
/// </summary>
public class AssignedWorkNotRemakeableException : NooException
{
    public AssignedWorkNotRemakeableException() : base("Assigned work is not remakeable.")
    {
        Id = "ASSIGNED_WORK.NOT_REMAKEABLE";
        StatusCode = HttpStatusCode.BadRequest;
    }
}
