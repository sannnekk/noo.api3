using System.Text.Json.Serialization;

namespace Noo.Api.Statistics.DTO;

public record StatisticsGraphDTO
{
    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("lines")]
    public IReadOnlyList<StatisticsGraphLineDTO> Lines { get; init; } = [];
}
