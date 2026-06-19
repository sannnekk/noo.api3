using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.NooTube.Exceptions;

/// <summary>
/// Error Code: NOOTUBE.UNSUPPORTED_ENGINE
/// Name: Видеосервис не поддерживается
/// Description: Выбранный видеосервис не поддерживается
/// </summary>
public class UnsupportedVideoEngineException : NooException
{
    public UnsupportedVideoEngineException(string message = "The requested video engine is not supported.")
        : base(message)
    {
        StatusCode = HttpStatusCode.BadRequest;
        Id = "NOOTUBE.UNSUPPORTED_ENGINE";
    }
}
