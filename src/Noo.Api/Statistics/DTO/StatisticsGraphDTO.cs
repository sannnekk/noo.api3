using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Statistics.DTO;

public record StatisticsGraphDTO
{
    [Required]
    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("lines")]
    public IReadOnlyList<StatisticsGraphLineDTO> Lines { get; init; } = [];
}
