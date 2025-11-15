using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Users.DTO;

namespace Noo.Api.NooTube.DTO;

public record NooTubeVideoCommentDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "NooTubeVideoComment";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [Required]
    [JsonPropertyName("userId")]
    public Ulid UserId { get; init; }

    [JsonPropertyName("user")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserDTO? User { get; init; }

    [Required]
    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }
}
