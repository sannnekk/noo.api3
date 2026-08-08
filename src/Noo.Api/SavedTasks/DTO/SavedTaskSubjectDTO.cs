using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Subjects.DTO;

namespace Noo.Api.SavedTasks.DTO;

/// <summary>
/// One subject a student has saved tasks on, with how many. What a quiz is set
/// up from: which subjects are on offer and which have enough cards to run.
/// </summary>
public record SavedTaskSubjectDTO
{
    /// <summary>
    /// Null for tasks whose work has no subject.
    /// </summary>
    [JsonPropertyName("subject")]
    public SubjectDTO? Subject { get; init; }

    [Required]
    [JsonPropertyName("savedTaskCount")]
    public int SavedTaskCount { get; init; }
}
