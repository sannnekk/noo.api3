using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Users.DTO;

namespace Noo.Api.AssignedWorks.DTO;

public record AssignedWorkHistoryDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "AssignedWorkHistory";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [Required]
    [JsonPropertyName("type")]
    public AssignedWorkHistoryType Type { get; init; }

    [Required]
    [JsonPropertyName("changedAt")]
    public DateTime ChangedAt { get; init; }

    [JsonPropertyName("value")]
    public Dictionary<string, string>? Value { get; init; }

    [JsonPropertyName("changedBy")]
    public UserDTO? ChangedBy { get; init; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; init; }
}
