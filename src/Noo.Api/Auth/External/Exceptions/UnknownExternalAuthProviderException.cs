using System.Net;
using Noo.Api.Auth.External.Types;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.Auth.External.Exceptions;

/// <summary>
/// Error Code: EXTERNAL_AUTH_UNKNOWN_PROVIDER
/// Name: Провайдер недоступен
/// Description: Вход через этот сервис не настроен
/// </summary>
public class UnknownExternalAuthProviderException : NooException
{
    public UnknownExternalAuthProviderException(ExternalAuthProviderType type)
        : base($"Вход через \"{type}\" не настроен.")
    {
        Id = "EXTERNAL_AUTH_UNKNOWN_PROVIDER";
        StatusCode = HttpStatusCode.BadRequest;
    }
}
