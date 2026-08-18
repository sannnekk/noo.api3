using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.Auth.External.Exceptions;

/// <summary>
/// Error Code: EXTERNAL_AUTH_EMAIL_TAKEN
/// Name: Почта уже занята
/// Description: Аккаунт с такой почтой уже существует. Войдите обычным способом и привяжите сервис в настройках
/// </summary>
public class ExternalAuthEmailConflictException : NooException
{
    public ExternalAuthEmailConflictException()
        : base(
            "Аккаунт с такой почтой уже существует. Войдите с паролем и привяжите этот сервис в настройках."
        )
    {
        Id = "EXTERNAL_AUTH_EMAIL_TAKEN";
        StatusCode = HttpStatusCode.Conflict;
    }
}
