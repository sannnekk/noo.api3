using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.Auth.External.Exceptions;

/// <summary>
/// Error Code: EXTERNAL_AUTH_INVALID_STATE
/// Name: Сессия входа истекла
/// Description: Ссылка входа устарела или уже была использована. Начните вход заново
/// </summary>
public class ExternalAuthStateInvalidException : NooException
{
    public ExternalAuthStateInvalidException()
        : base("Сессия входа истекла или уже была использована. Начните вход заново.")
    {
        Id = "EXTERNAL_AUTH_INVALID_STATE";
        StatusCode = HttpStatusCode.BadRequest;
    }
}
