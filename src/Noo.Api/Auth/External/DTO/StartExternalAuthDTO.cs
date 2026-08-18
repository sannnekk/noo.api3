using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Auth.External.DTO;

public record StartExternalAuthDTO
{
    /// <summary>Relative path to land on once the callback succeeds.</summary>
    [MaxLength(255)]
    [JsonPropertyName("returnUrl")]
    public string? ReturnUrl { get; init; }
}
