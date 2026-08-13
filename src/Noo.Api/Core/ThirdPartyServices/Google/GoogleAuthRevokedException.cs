namespace Noo.Api.Core.ThirdPartyServices.Google;

/// <summary>
/// Thrown when Google refuses a stored refresh token — typically because the user revoked the
/// grant, changed their password, or the token expired through disuse. Unlike a transient failure
/// this cannot be retried: the user has to reconnect their Google account.
/// </summary>
public class GoogleAuthRevokedException : Exception
{
    public GoogleAuthRevokedException()
        : base("Доступ к Google-аккаунту отозван. Необходимо заново подключить аккаунт.") { }

    public GoogleAuthRevokedException(Exception innerException)
        : base("Доступ к Google-аккаунту отозван. Необходимо заново подключить аккаунт.", innerException) { }

    public GoogleAuthRevokedException(string message)
        : base(message) { }

    public GoogleAuthRevokedException(string message, Exception innerException)
        : base(message, innerException) { }
}
