using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.Auth.Exceptions;

/// <summary>
/// Error Code: TOKEN_EXPIRED
/// Name: Токен истек
/// Description: Пожалуйста, войдите в систему заново
/// </summary>
public class TokenExpiredException : NooException
{
    public TokenExpiredException() : base("Token has expired.")
    {
        Id = "TOKEN_EXPIRED";
        StatusCode = HttpStatusCode.RequestedRangeNotSatisfiable;
    }
}
