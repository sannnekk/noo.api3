using System.Net;

namespace Noo.Api.Core.Exceptions.Http;

/// <summary>
/// Error Code: CANT_CHANGE_ROLE
/// Name: Невозможно изменить роль
/// Description: Изменить роль можно только у пользователей с ролью "ученик"
/// </summary>
public class CantChangeRoleException : NooException
{
    public CantChangeRoleException(string message = "Role change is not possible") : base(message)
    {
        Id = "CANT_CHANGE_ROLE";
        StatusCode = HttpStatusCode.Forbidden;
    }
}
