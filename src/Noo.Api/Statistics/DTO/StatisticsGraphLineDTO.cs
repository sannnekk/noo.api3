using System.Text.Json.Serialization;

namespace Noo.Api.Statistics.DTO;

public record StatisticsGraphLineDTO
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("values")]
    public IReadOnlyDictionary<string, double?> Values { get; init; } = new Dictionary<string, double?>();
}
