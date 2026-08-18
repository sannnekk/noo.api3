using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.Auth.External.Exceptions;

/// <summary>
/// Error Code: EXTERNAL_AUTH_LAST_CREDENTIAL
/// Name: Единственный способ входа
/// Description: Это единственный способ войти в аккаунт. Задайте пароль, прежде чем отвязывать его
/// </summary>
public class LastCredentialException : NooException
{
    public LastCredentialException()
        : base("Это единственный способ войти в аккаунт. Сначала задайте пароль.")
    {
        Id = "EXTERNAL_AUTH_LAST_CREDENTIAL";
        StatusCode = HttpStatusCode.Conflict;
    }
}
