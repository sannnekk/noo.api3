using Noo.Api.Auth.DTO;
using Noo.Api.Users.Models;

namespace Noo.Api.Auth.Services;

public interface IAuthService
{
    public Task<AuthTokensResult> LoginAsync(LoginDTO request);

    /// <summary>
    /// Opens a session for a user the caller has already authenticated. Enforces only
    /// the block check that applies to every login path.
    /// </summary>
    public Task<AuthTokensResult> IssueSessionAsync(UserModel user);

    public Task<RefreshResult> RefreshAsync(string? rawRefreshToken);

    public Task RegisterAsync(RegisterDTO request);

    public Task RequestPasswordResetAsync(string email);

    public Task ConfirmPasswordResetAsync(string token, string newPassword);

    public Task ConfirmEmailAsync(string token);

    public Task<bool> IsUsernameFreeAsync(string username);
}
