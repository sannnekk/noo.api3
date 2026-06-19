using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.NooTube.DTO;

public record CreateNooTubeVideoCommentDTO
{
    [JsonPropertyName("content")]
    [Required]
    [MinLength(1)]
    [MaxLength(512)]
    public string Content { get; set; } = string.Empty;
}
