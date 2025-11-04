namespace Noo.Api.Core.ThirdPartyServices.Google;

public interface IGoogleAuthService
{
    public Task<GoogleAuth> AuthenticateAsync(GoogleAuthData googleAuthData);
}
