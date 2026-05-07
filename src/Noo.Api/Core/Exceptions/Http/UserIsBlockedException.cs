using System.Net;

namespace Noo.Api.Core.Exceptions.Http;

/// <summary>
/// Error Code: USER_IS_BLOCKED
/// Name: Пользователь заблокирован
/// Description: Обратитесь в поддержку для получения дополнительной информации
/// </summary>
public class UserIsBlockedException : NooException
{
    public UserIsBlockedException(string message = "A user is blocked") : base(message)
    {
        Id = "USER_IS_BLOCKED";
        StatusCode = HttpStatusCode.Forbidden;
    }
}
