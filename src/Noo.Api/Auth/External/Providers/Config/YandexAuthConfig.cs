using Noo.Api.Core.Config;

namespace Noo.Api.Auth.External.Providers.Config;

/// <summary>
/// No [Required] anywhere: ValidateOnStart would crash every environment that has not
/// configured the provider. An unconfigured provider is simply disabled instead.
/// </summary>
[ModuleConfig]
public class YandexAuthConfig : IConfig
{
    public static string SectionName => "ExternalAuth:Yandex";

    public bool Enabled { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string[] Scopes { get; set; } = ["login:info", "login:email", "login:avatar"];

    public bool TrustEmailForLinking { get; set; } = true;
}
