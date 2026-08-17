namespace Noo.Api.Users.Types;

public enum UserAvatarType
{
    None,

    /// <summary>Uploaded image, backed by <c>MediaId</c>.</summary>
    Custom,

    Telegram,

    /// <summary>Provider-hosted picture, backed by <c>AvatarUrl</c>.</summary>
    External
}
