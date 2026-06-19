using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.NooTube.Exceptions;

/// <summary>
/// Error Code: NOOTUBE.ENCODING_NOT_FINISHED
/// Name: Видео ещё обрабатывается
/// Description: Видео ещё обрабатывается видеосервисом. Попробуйте позже
/// </summary>
public class EncodingNotFinishedYetException : NooException
{
    public EncodingNotFinishedYetException(string message = "The video is still being processed by the video service.")
        : base(message)
    {
        StatusCode = HttpStatusCode.Locked;
        Id = "NOOTUBE.ENCODING_NOT_FINISHED";
    }
}
