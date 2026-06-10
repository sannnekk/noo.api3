using System.Text.Json.Serialization;

namespace Noo.Api.Core.Utils.Richtext.Tiptap;

/// <summary>
/// A tiptap mark applied to a text node (e.g. bold, italic, link).
/// </summary>
public class TiptapMark
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("attrs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Attrs { get; set; }
}
