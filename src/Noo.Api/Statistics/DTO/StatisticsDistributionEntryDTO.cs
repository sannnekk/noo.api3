using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Statistics.DTO;

public record StatisticsDistributionEntryDTO
{
    [Required]
    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("value")]
    public double Value { get; init; }

    /// <summary>
    /// A stable key the client draws an icon for, e.g. <c>chrome</c> or <c>tablet</c>.
    /// Clients that do not know the key fall back to a generic icon.
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; init; }
}
