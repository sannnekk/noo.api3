using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.SavedTasks.DTO;

public record CheckSavedTaskAnswerDTO
{
    [JsonPropertyName("answer")]
    [MaxLength(255)]
    public string? Answer { get; init; }
}
