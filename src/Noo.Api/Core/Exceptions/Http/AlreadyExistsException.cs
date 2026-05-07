using System.Net;

namespace Noo.Api.Core.Exceptions.Http;

/// <summary>
/// Error Code: ALREADY_EXISTS
/// Name: Уже существует
/// Description: Данный элемент уже существует в системе
/// </summary>
public class AlreadyExistsException : NooException
{
    public AlreadyExistsException(string message = "Already exists") : base(message)
    {
        Id = "ALREADY_EXISTS";
        StatusCode = HttpStatusCode.Conflict;
    }
}
