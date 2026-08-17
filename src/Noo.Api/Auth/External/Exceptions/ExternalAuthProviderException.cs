using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.Auth.External.Exceptions;

/// <summary>
/// Error Code: EXTERNAL_AUTH_PROVIDER_ERROR
/// Name: Ошибка провайдера
/// Description: Сервис авторизации вернул ошибку. Попробуйте войти ещё раз
/// </summary>
public class ExternalAuthProviderException : NooException
{
    public ExternalAuthProviderException(string message) : base(message)
    {
        Id = "EXTERNAL_AUTH_PROVIDER_ERROR";
        StatusCode = HttpStatusCode.BadRequest;
    }
}
