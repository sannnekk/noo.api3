using System.ComponentModel.DataAnnotations;

namespace Noo.Api.Core.Config.Env;

public class GoogleConfig : IConfig
{
    public static string SectionName => "Google";

    [Required]
    public required string ClientId { get; set; }

    [Required]
    public required string ClientSecret { get; set; }

    /// <summary>
    /// Must match the redirect URI registered in the Google Cloud OAuth client
    /// and the route the frontend serves the OAuth popup callback from.
    /// </summary>
    [Required]
    public required string RedirectUri { get; set; }

    /// <summary>
    /// Base64-encoded 256-bit key used to encrypt refresh tokens at rest.
    /// </summary>
    [Required]
    public required string TokenEncryptionKey { get; set; }

    [Range(1, 1_000_000)]
    public int MaxExportRows { get; set; } = 200_000;
}
