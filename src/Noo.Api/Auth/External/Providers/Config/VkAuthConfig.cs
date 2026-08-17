using Noo.Api.Core.Config;

namespace Noo.Api.Auth.External.Providers.Config;

[ModuleConfig]
public class VkAuthConfig : IConfig
{
    public static string SectionName => "ExternalAuth:Vk";

    public bool Enabled { get; set; }

    public string? ClientId { get; set; }

    /// <summary>VK ID has no client secret — PKCE replaces it. Confidential apps send this instead.</summary>
    public string? ServiceToken { get; set; }

    public string[] Scopes { get; set; } = ["vkid.personal_info", "email"];

    public bool TrustEmailForLinking { get; set; } = true;
}
