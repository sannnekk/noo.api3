using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Core.Utils.Richtext;

namespace Noo.Api.AssignedWorks.DTO;

public record AssignedWorkCommentDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "AssignedWorkComment";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [JsonPropertyName("content")]
    public IRichTextType? Content { get; init; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; init; }
}
