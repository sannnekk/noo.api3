using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Media.DTO;
using Noo.Api.Users.DTO;
using Noo.Api.Works.DTO;
using Noo.Api.Works.Types;

namespace Noo.Api.AssignedWorks.DTO;

public record AssignedWorkDTO : IHasPresignedMedia
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "AssignedWork";

    public IEnumerable<MediaDTO?> GetMediaForPresigning()
    {
        return PresignedMedia.Collect(Student, MainMentor, HelperMentor);
    }

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [Required]
    [JsonPropertyName("title")]
    public string Title { get; init; } = null!;

    [Required]
    [JsonPropertyName("type")]
    public WorkType Type { get; init; }

    [Required]
    [JsonPropertyName("attempt")]
    public int Attempt { get; init; }

    [Required]
    [JsonPropertyName("solveStatus")]
    public AssignedWorkSolveStatus SolveStatus { get; init; }

    [JsonPropertyName("soldeDeadlineAt")]
    public DateTime? SolveDeadlineAt { get; init; }

    [JsonPropertyName("solvedAt")]
    public DateTime? SolvedAt { get; init; }

    [Required]
    [JsonPropertyName("checkStatus")]
    public AssignedWorkCheckStatus CheckStatus { get; init; }

    [JsonPropertyName("checkDeadlineAt")]
    public DateTime? CheckDeadlineAt { get; init; }

    [JsonPropertyName("checkedAt")]
    public DateTime? CheckedAt { get; init; }

    [JsonPropertyName("score")]
    public int? Score { get; init; }

    [Required]
    [JsonPropertyName("maxScore")]
    public int MaxScore { get; init; }

    [Required]
    [JsonPropertyName("isArchivedByStudent")]
    public bool IsArchivedByStudent { get; init; }

    [Required]
    [JsonPropertyName("isArchivedByMentors")]
    public bool IsArchivedByMentors { get; init; }

    [Required]
    [JsonPropertyName("isArchivedByAssistants")]
    public bool IsArchivedByAssistants { get; init; }

    [JsonPropertyName("studentCommentId")]
    public Ulid? StudentCommentId { get; init; }

    [JsonPropertyName("mainMentorCommentId")]
    public Ulid? MainMentorCommentId { get; init; }

    [JsonPropertyName("helperMentorCommentId")]
    public Ulid? HelperMentorCommentId { get; init; }

    [Required]
    [JsonPropertyName("studentId")]
    public Ulid StudentId { get; init; }

    [JsonPropertyName("mainMentorId")]
    public Ulid? MainMentorId { get; init; }

    [JsonPropertyName("helperMentorId")]
    public Ulid? HelperMentorId { get; init; }

    [JsonPropertyName("student")]
    public UserDTO? Student { get; init; }

    [JsonPropertyName("mainMentor")]
    public UserDTO? MainMentor { get; init; }

    [JsonPropertyName("helperMentor")]
    public UserDTO? HelperMentor { get; init; }

    [JsonPropertyName("work")]
    public WorkDTO? Work { get; init; }

    [JsonPropertyName("answers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<AssignedWorkAnswerDTO>? Answers { get; init; }
}
