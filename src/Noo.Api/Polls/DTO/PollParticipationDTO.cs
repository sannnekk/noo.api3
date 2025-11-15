using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Polls.Types;
using Noo.Api.Users.DTO;

namespace Noo.Api.Polls.DTO;

public record PollParticipationDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName { get; init; } = "PollParticipation";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [Required]
    [JsonPropertyName("pollId")]
    public Ulid PollId { get; init; }

    [JsonPropertyName("poll")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PollDTO? Poll { get; init; }

    [JsonPropertyName("userId")]
    public Ulid? UserId { get; init; }

    [Required]
    [JsonPropertyName("userType")]
    public ParticipatingUserType UserType { get; init; }

    [JsonPropertyName("userExternalIdentifier")]
    public string? UserExternalIdentifier { get; init; }

    [JsonPropertyName("userExternalData")]
    public object? UserExternalData { get; init; }

    [JsonPropertyName("user")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserDTO? User { get; init; }

    [JsonPropertyName("answers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<PollAnswerDTO>? Answers { get; init; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }
}
