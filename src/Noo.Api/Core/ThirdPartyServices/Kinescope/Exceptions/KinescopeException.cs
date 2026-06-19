using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.Core.ThirdPartyServices.Kinescope.Exceptions;

/// <summary>
/// Error Code: KINESCOPE.SERVICE_PROBLEM
/// Name: Ошибка видеосервиса
/// Description: Возникла ошибка при работе с видеосервисом. Попробуйте позже
/// </summary>
public class KinescopeException : NooException
{
    public KinescopeException(string message = "Problem with the Kinescope video service occured.")
        : base(message)
    {
        StatusCode = HttpStatusCode.BadGateway;
        Id = "KINESCOPE.SERVICE_PROBLEM";
        IsInternal = true;
    }
}
