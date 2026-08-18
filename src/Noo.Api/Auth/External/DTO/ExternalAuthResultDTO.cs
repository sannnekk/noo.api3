using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Auth.DTO;
using Noo.Api.Auth.External.Types;

namespace Noo.Api.Auth.External.DTO;

/// <summary>
/// One callback endpoint serves both intents, because the browser lands on a single
/// registered redirect URI. The intent tells the frontend how to finish.
/// </summary>
public record ExternalAuthResultDTO
{
    [Required]
    [JsonPropertyName("intent")]
    public ExternalAuthIntent Intent { get; init; }

    [Required]
    [JsonPropertyName("provider")]
    public ExternalAuthProviderType Provider { get; init; }

    [JsonPropertyName("returnUrl")]
    public string? ReturnUrl { get; init; }

    /// <summary>Null when the intent was to link a provider to an already open session.</summary>
    [JsonPropertyName("session")]
    public LoginResponseDTO? Session { get; init; }
}
