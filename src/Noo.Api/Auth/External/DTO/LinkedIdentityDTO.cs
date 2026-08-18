using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Auth.External.Types;

namespace Noo.Api.Auth.External.DTO;

public record LinkedIdentityDTO
{
    [Required]
    [JsonPropertyName("provider")]
    public ExternalAuthProviderType Provider { get; init; }

    /// <summary>The email the provider reported, for telling two linked accounts apart.</summary>
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("lastLoginAt")]
    public DateTime? LastLoginAt { get; init; }

    [Required]
    [JsonPropertyName("linkedAt")]
    public DateTime LinkedAt { get; init; }
}
