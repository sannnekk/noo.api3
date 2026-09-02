using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Platform.DTO;

/// <summary>
/// The patchable half of <see cref="PlatformSettingsDTO"/>.
/// </summary>
/// <remarks>
/// Every member is nullable so that a JSON Patch touching one link leaves the
/// rest alone; the mapper writes back only the ones that carry a value. The
/// links are validated as absolute URLs here rather than in the model, so a
/// typo comes back as a 400 instead of reaching every visitor's footer.
/// </remarks>
public record UpdatePlatformSettingsDTO
{
    [JsonPropertyName("shopLink")]
    [Url]
    [MaxLength(255)]
    public string? ShopLink { get; set; }

    [JsonPropertyName("privacyPolicyLink")]
    [Url]
    [MaxLength(255)]
    public string? PrivacyPolicyLink { get; set; }

    [JsonPropertyName("termsLink")]
    [Url]
    [MaxLength(255)]
    public string? TermsLink { get; set; }

    [JsonPropertyName("supportChatLink")]
    [Url]
    [MaxLength(255)]
    public string? SupportChatLink { get; set; }

    [JsonPropertyName("supportChatName")]
    [MaxLength(255)]
    public string? SupportChatName { get; set; }

    [JsonPropertyName("supportEmail")]
    [EmailAddress]
    [MaxLength(255)]
    public string? SupportEmail { get; set; }

    [JsonPropertyName("supportResponseTime")]
    [MaxLength(255)]
    public string? SupportResponseTime { get; set; }
}
