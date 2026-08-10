using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Polls.DTO;

public record PollDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "Poll";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [Required]
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [Required]
    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [Required]
    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; init; }

    [Required]
    [JsonPropertyName("isAuthRequired")]
    public bool IsAuthRequired { get; init; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; init; }

    [JsonPropertyName("questions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ICollection<PollQuestionDTO>? Questions { get; init; }

    [Required]
    [JsonPropertyName("participationsCount")]
    public int ParticipationsCount { get; init; }

    /// <summary>
    /// Whether the caller has already answered this poll. Only computed when a
    /// single poll is fetched by its id, and always false for an anonymous
    /// caller — an unauthenticated visitor cannot be recognized before they
    /// identify themselves.
    /// </summary>
    [Required]
    [JsonPropertyName("hasParticipated")]
    public bool HasParticipated { get; init; }
}
