using System.Net;

namespace Noo.Api.Core.Exceptions.Http;

/// <summary>
/// Error Code: NOT_FOUND
/// Name: Не найдено
/// Description: Запрашиваемый ресурс не найден
/// </summary>
public class NotFoundException : NooException
{
    public NotFoundException(string message = "Not found") : base(message)
    {
        Id = "NOT_FOUND";
        StatusCode = HttpStatusCode.NotFound;
    }
}
