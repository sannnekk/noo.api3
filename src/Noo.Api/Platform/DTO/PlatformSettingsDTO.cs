using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Platform.DTO;

public record PlatformSettingsDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "PlatformSettings";

    [Required]
    [JsonPropertyName("shopLink")]
    public string ShopLink { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("privacyPolicyLink")]
    public string PrivacyPolicyLink { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("termsLink")]
    public string TermsLink { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("supportChatLink")]
    public string SupportChatLink { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("supportChatName")]
    public string SupportChatName { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("supportEmail")]
    public string SupportEmail { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("supportResponseTime")]
    public string SupportResponseTime { get; set; } = string.Empty;
}
