using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.AssignedWorks.DTO;

public record AssignedWorksMetadataDTO
{
    [Required]
    [JsonPropertyName("counts")]
    public AssignedWorksCounts Counts { get; init; } = null!;
}

public record AssignedWorksCounts
{
    [Required]
    [JsonPropertyName("all")]
    public int Total { get; init; }

    [Required]
    [JsonPropertyName("notSolved")]
    public int NotSolved { get; init; }

    [Required]
    [JsonPropertyName("notChecked")]
    public int NotChecked { get; init; }

    [Required]
    [JsonPropertyName("checked")]
    public int Checked { get; init; }
}
