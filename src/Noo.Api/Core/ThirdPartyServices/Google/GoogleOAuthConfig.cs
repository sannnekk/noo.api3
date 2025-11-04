namespace Noo.Api.Core.ThirdPartyServices.Google;

public class GoogleOAuthConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty; // must match frontend flow
}
