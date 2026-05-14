using Noo.Api.Auth.DTO;

namespace Noo.Api.Auth.Services;

public interface IAuthService
{
    public Task<LoginResponseDTO> LoginAsync(LoginDTO request);

    public Task RegisterAsync(RegisterDTO request);

    public Task RequestPasswordResetAsync(string email);

    public Task ConfirmPasswordResetAsync(string token, string newPassword);

    public Task ConfirmEmailAsync(string token);

    public Task<bool> IsUsernameFreeAsync(string username);
}
