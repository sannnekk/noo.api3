using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.Auth.External.Exceptions;

/// <summary>
/// Error Code: EXTERNAL_AUTH_INVALID_CALLBACK
/// Name: Некорректный ответ провайдера
/// Description: Провайдер не вернул обязательные данные. Попробуйте войти ещё раз
/// </summary>
public class ExternalAuthCallbackParameterException : NooException
{
    public ExternalAuthCallbackParameterException(string parameter)
        : base($"Провайдер не передал обязательный параметр \"{parameter}\".")
    {
        Id = "EXTERNAL_AUTH_INVALID_CALLBACK";
        StatusCode = HttpStatusCode.BadRequest;
    }
}
