using System.Text.Json.Serialization;
using Noo.Api.Core.Utils;
using Noo.Api.Platform.Types;

namespace Noo.Api.Platform.DTO;

public record ChangeLogDTO
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public DateTime Date { get; set; } = Clock.Now;

    [JsonPropertyName("changes")]
    public IEnumerable<PlatformChange> Changes { get; set; } = [];
}
