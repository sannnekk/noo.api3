using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Auth.External.DTO;

public record ExternalAuthCallbackDTO
{
    /// <summary>
    /// The callback query string as the browser received it. Passed through untouched so
    /// provider-specific parameters (VK's <c>device_id</c>) need no shared plumbing.
    /// </summary>
    [Required]
    [JsonPropertyName("parameters")]
    public Dictionary<string, string> Parameters { get; init; } = [];
}
