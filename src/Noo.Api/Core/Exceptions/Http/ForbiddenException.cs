using System.Net;

namespace Noo.Api.Core.Exceptions.Http;

/// <summary>
/// Error Code: FORBIDDEN
/// Name: Доступ запрещен
/// Description: У вас нет прав доступа к этому ресурсу
/// </summary>
public class ForbiddenException : NooException
{
    public ForbiddenException(string message = "Forbidden") : base(message)
    {
        Id = "FORBIDDEN";
        StatusCode = HttpStatusCode.Forbidden;
    }
}
