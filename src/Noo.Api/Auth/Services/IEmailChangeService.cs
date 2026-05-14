namespace Noo.Api.Auth.Services;

public interface IEmailChangeService
{
    public Task RequestAsync(Ulid userId, string newEmail);

    public Task ConfirmAsync(Ulid userId, string newEmail);
}
