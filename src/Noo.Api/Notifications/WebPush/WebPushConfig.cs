using System.ComponentModel.DataAnnotations;
using Noo.Api.Core.Config;

namespace Noo.Api.Notifications.WebPush;

[ModuleConfig]
public class WebPushConfig : IConfig
{
    public static string SectionName => "WebPush";

    public bool Enabled { get; set; }

    /// <summary>
    /// A mailto: or https: contact that push services use to reach the sender about abuse.
    /// </summary>
    public string Subject { get; set; } = "mailto:support@noo-school.ru";

    public string? PublicKey { get; set; }

    public string? PrivateKey { get; set; }

    [Range(60, 2419200)]
    public int TtlSeconds { get; set; } = 86400;

    public bool IsUsable =>
        Enabled && !string.IsNullOrWhiteSpace(PublicKey) && !string.IsNullOrWhiteSpace(PrivateKey);
}
