using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.AssignedWorks.DTO;

public record AssignedWorkAnswerDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "AssignedWorkAnswer";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }
}
