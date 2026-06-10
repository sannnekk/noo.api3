using System.Text.Json.Serialization;

namespace Noo.Api.Core.Utils.Richtext.Tiptap;

/// <summary>
/// A single node of a tiptap (ProseMirror) document tree. A node is either a
/// leaf text node (with <see cref="Text"/>) or a container with child
/// <see cref="Content"/>.
/// </summary>
public class TiptapNode
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<TiptapNode>? Content { get; set; }

    [JsonPropertyName("marks")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<TiptapMark>? Marks { get; set; }

    [JsonPropertyName("attrs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Attrs { get; set; }

    /// <summary>
    /// Length of the plain text contained in this node and its descendants.
    /// </summary>
    public int Length()
    {
        return (Text?.Length ?? 0) + (Content?.Sum(node => node.Length()) ?? 0);
    }

    public override string ToString()
    {
        return (Text ?? string.Empty)
            + string.Concat(Content?.Select(node => node.ToString()) ?? []);
    }
}
