namespace Noo.Api.Auth;

public static class AuthConfig
{
    public static TimeSpan ConfirmEmailExpireTime => TimeSpan.FromHours(12);

    public static TimeSpan ResetPasswordExpireTime => TimeSpan.FromHours(1);
}
