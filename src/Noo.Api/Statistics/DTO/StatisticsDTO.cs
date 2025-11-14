using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Statistics.DTO;

public record StatisticsDTO
{
    [Required]
    [JsonPropertyName("blocks")]
    public IEnumerable<StatisticsBlockDTO> Blocks { get; init; } = [];
}
