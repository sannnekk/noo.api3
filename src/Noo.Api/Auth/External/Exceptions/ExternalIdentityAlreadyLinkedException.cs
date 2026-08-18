using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.Auth.External.Exceptions;

/// <summary>
/// Error Code: EXTERNAL_AUTH_ALREADY_LINKED
/// Name: Аккаунт уже привязан
/// Description: Этот аккаунт провайдера уже привязан к профилю
/// </summary>
public class ExternalIdentityAlreadyLinkedException : NooException
{
    public ExternalIdentityAlreadyLinkedException(string message)
        : base(message)
    {
        Id = "EXTERNAL_AUTH_ALREADY_LINKED";
        StatusCode = HttpStatusCode.Conflict;
    }
}
