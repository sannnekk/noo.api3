using System.Net;

namespace Noo.Api.Core.Exceptions.Http;

/// <summary>
/// Error Code: UNSUPPORTED_MEDIA_TYPE
/// Name: Неподдерживаемый тип данных
/// Description: Используемый формат файла не поддерживается
/// </summary>
public class UnsupportedMediaTypeException : NooException
{
    public UnsupportedMediaTypeException(string message = "Unsupported media type") : base(message)
    {
        Id = "UNSUPPORTED_MEDIA_TYPE";
        StatusCode = HttpStatusCode.UnsupportedMediaType;
    }
}
