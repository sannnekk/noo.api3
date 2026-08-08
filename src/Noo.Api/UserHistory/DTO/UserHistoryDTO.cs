using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.UserHistory.Types;
using Noo.Api.Users.DTO;

namespace Noo.Api.UserHistory.DTO;

public record UserHistoryDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "UserHistory";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [Required]
    [JsonPropertyName("type")]
    public UserHistoryType Type { get; init; }

    [Required]
    [JsonPropertyName("subjectUserId")]
    public Ulid SubjectUserId { get; init; }

    [JsonPropertyName("actorUserId")]
    public Ulid? ActorUserId { get; init; }

    [JsonPropertyName("actor")]
    public UserDTO? Actor { get; init; }

    [JsonPropertyName("payload")]
    public Dictionary<string, string>? Payload { get; init; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; init; }
}
