using System.Net;

namespace Noo.Api.Core.Exceptions.Http;

/// <summary>
/// Error Code: CONFLICT.ALREADY_EXISTS
/// Name: Такая запись уже существует
/// Description: Запись с такими данными уже есть, повторная не создаётся
/// </summary>
/// <remarks>
/// What a unique index reports when it turns a duplicate away. A caller that can say
/// something more useful about the particular duplicate should raise its own exception
/// before it gets this far; this is the answer of last resort, for the race that slips
/// between a check and the insert it guarded.
/// </remarks>
public class ConflictException : NooException
{
    public ConflictException(string message = "The record already exists.")
        : base(message)
    {
        Id = "CONFLICT.ALREADY_EXISTS";
        StatusCode = HttpStatusCode.Conflict;
    }
}
