using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Statistics.DTO;

public record StatisticsGraphLineDTO
{
    [Required]
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("values")]
    public IReadOnlyDictionary<string, double?> Values { get; init; } = new Dictionary<string, double?>();
}
