using System.Net;

namespace Noo.Api.Core.Exceptions.Http;

/// <summary>
/// Error Code: UNAUTHORIZED
/// Name: Запрос не авторизован
/// Description: Пожалуйста, войдите в систему, чтобы продолжить
/// </summary>
public class UnauthorizedException : NooException
{
    public UnauthorizedException(string message = "Unauthorized") : base(message)
    {
        Id = "UNAUTHORIZED";
        StatusCode = HttpStatusCode.Unauthorized;
    }
}
