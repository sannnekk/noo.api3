using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Users.Types;

public class UserCreationPayload
{
    public string Username { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? PasswordHash { get; set; }

    public string Name { get; set; } = string.Empty;

    public UserRoles Role { get; set; } = UserRoles.Student;
}
