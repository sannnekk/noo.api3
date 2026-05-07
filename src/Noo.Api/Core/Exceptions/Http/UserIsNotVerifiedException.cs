using System.Net;

namespace Noo.Api.Core.Exceptions.Http;

/// <summary>
/// Error Code: USER_NOT_VERIFIED
/// Name: Пользователь не подтвержден
/// Description: Пожалуйста, подтвердите свою почту для доступа к платформе
/// </summary>
public class UserIsNotVerifiedException : NooException
{
    public UserIsNotVerifiedException(string message = "User is not verified") : base(message)
    {
        Id = "USER_NOT_VERIFIED";
        StatusCode = HttpStatusCode.Unauthorized;
    }
}
