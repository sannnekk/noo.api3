using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Statistics.DTO;

/// <summary>
/// A breakdown of one total into named parts, ordered from the largest part down.
/// </summary>
public record StatisticsDistributionDTO
{
    [Required]
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [Required]
    [JsonPropertyName("entries")]
    public IReadOnlyList<StatisticsDistributionEntryDTO> Entries { get; init; } = [];
}
