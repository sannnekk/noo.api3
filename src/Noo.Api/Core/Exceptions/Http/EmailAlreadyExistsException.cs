using System.Net;

namespace Noo.Api.Core.Exceptions.Http;

/// <summary>
/// Error Code: EMAIL_ALREADY_EXISTS
/// Name: Email уже занят
/// Description: Пользователь с таким email уже существует
/// </summary>
public class EmailAlreadyExistsException : NooException
{
    public EmailAlreadyExistsException(string message = "Email already exists") : base(message)
    {
        Id = "EMAIL_ALREADY_EXISTS";
        StatusCode = HttpStatusCode.Conflict;
    }
}
