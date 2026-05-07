using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Media.DTO;

public record UploadTicketDTO
{
    [Required]
    [JsonPropertyName("mediaId")]
    public Ulid MediaId { get; init; }

    [Required]
    [JsonPropertyName("uploadUrl")]
    public string UploadUrl { get; init; } = string.Empty;

    /// <summary>
    /// Headers the client MUST include verbatim on the PUT request to S3.
    /// </summary>
    [Required]
    [JsonPropertyName("headers")]
    public IReadOnlyDictionary<string, string> Headers { get; init; }
        = new Dictionary<string, string>();

    [Required]
    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; init; }
}
