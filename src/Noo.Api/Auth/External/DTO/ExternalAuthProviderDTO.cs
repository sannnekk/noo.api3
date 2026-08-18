using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Auth.External.Types;

namespace Noo.Api.Auth.External.DTO;

/// <summary>
/// A provider the current environment can actually sign users in with.
/// </summary>
public record ExternalAuthProviderDTO
{
    [Required]
    [JsonPropertyName("provider")]
    public ExternalAuthProviderType Provider { get; init; }

    [Required]
    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;
}
