using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.NooTube.DTO;

public record NooTubeVideoStatisticsDTO
{
    [Required]
    [JsonPropertyName("from")]
    public DateTime From { get; init; }

    [Required]
    [JsonPropertyName("to")]
    public DateTime To { get; init; }

    [Required]
    [JsonPropertyName("views")]
    public long Views { get; init; }

    [Required]
    [JsonPropertyName("uniqueViews")]
    public long UniqueViews { get; init; }

    [Required]
    [JsonPropertyName("watchTimeSeconds")]
    public int WatchTimeSeconds { get; init; }

    [Required]
    [JsonPropertyName("playerLoads")]
    public long PlayerLoads { get; init; }

    [Required]
    [JsonPropertyName("timeline")]
    public IEnumerable<NooTubeVideoStatisticsPointDTO> Timeline { get; init; } = [];
}

public record NooTubeVideoStatisticsPointDTO
{
    [Required]
    [JsonPropertyName("date")]
    public DateTime Date { get; init; }

    [Required]
    [JsonPropertyName("views")]
    public long Views { get; init; }

    [Required]
    [JsonPropertyName("uniqueViews")]
    public long UniqueViews { get; init; }

    [Required]
    [JsonPropertyName("watchTimeSeconds")]
    public int WatchTimeSeconds { get; init; }

    [Required]
    [JsonPropertyName("playerLoads")]
    public long PlayerLoads { get; init; }
}
