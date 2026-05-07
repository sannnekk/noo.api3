using System.Net;

namespace Noo.Api.Core.Exceptions.Http;

/// <summary>
/// Error Code: BAD_REQUEST
/// Name: Некорректный запрос
/// Description: Проверьте правильность введенных данных
/// </summary>
public class BadRequestException : NooException
{
    public BadRequestException(string message = "Bad request") : base(message)
    {
        Id = "BAD_REQUEST";
        StatusCode = HttpStatusCode.BadRequest;
    }
}
