using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Media.Types;

namespace Noo.Api.Media.DTO;

public record RequestUploadDTO
{
    [Required]
    [JsonPropertyName("category")]
    public MediaCategory Category { get; init; }

    [Required]
    [MinLength(1)]
    [MaxLength(255)]
    [JsonPropertyName("fileName")]
    public string FileName { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("contentType")]
    public string ContentType { get; init; } = string.Empty;

    /// <summary>
    /// Optional id of the entity this media is attached to (e.g. a course id).
    /// Required by some categories' access rules.
    /// </summary>
    [JsonPropertyName("entityId")]
    public Ulid? EntityId { get; init; }
}
