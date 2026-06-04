using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Noo.Api.Core.Utils;

public struct FrontendLink
{
    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("params")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Params { get; set; }

    internal static FrontendLink Deserialize(string v)
    {
        return JsonSerializer.Deserialize<FrontendLink>(v);
    }

    internal readonly string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }
}
