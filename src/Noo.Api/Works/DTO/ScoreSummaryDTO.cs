using System.Text.Json.Serialization;

namespace Noo.Api.Works.DTO;

public record ScoreSummaryDTO
{
    [JsonPropertyName("absolute")]
    public double? Absolute { get; init; }

    [JsonPropertyName("percentage")]
    public double? Percentage { get; init; }
}
