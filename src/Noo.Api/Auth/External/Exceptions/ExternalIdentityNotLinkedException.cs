using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.Auth.External.Exceptions;

/// <summary>
/// Error Code: EXTERNAL_AUTH_NOT_LINKED
/// Name: Аккаунт не привязан
/// Description: К профилю не привязан аккаунт этого сервиса
/// </summary>
public class ExternalIdentityNotLinkedException : NooException
{
    public ExternalIdentityNotLinkedException()
        : base("К вашему профилю не привязан аккаунт этого сервиса.")
    {
        Id = "EXTERNAL_AUTH_NOT_LINKED";
        StatusCode = HttpStatusCode.NotFound;
    }
}
