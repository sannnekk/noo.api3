using System.Text.Json.Serialization;

namespace Noo.Api.Core.Utils.Richtext.Tiptap;

/// <summary>
/// Rich text stored in the tiptap (ProseMirror) JSON format. The root is always
/// a "doc" node; its children live in <see cref="Content"/>. Unlike Delta, this
/// is the editor's native format, so no conversion happens server-side.
/// </summary>
[JsonDerivedType(
    derivedType: typeof(TiptapRichText),
    typeDiscriminator: TypeDiscriminator
)]
public class TiptapRichText : IRichTextType
{
    public const string TypeDiscriminator = "tiptap";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "doc";

    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<TiptapNode>? Content { get; set; }

    /// <summary>
    /// Creates an empty tiptap document.
    /// </summary>
    public static TiptapRichText Empty()
    {
        return new TiptapRichText
        {
            Type = "doc",
            Content = []
        };
    }

    /// <summary>
    /// Creates a tiptap document from plain text. Blank text yields an empty
    /// document; otherwise the text is wrapped in a single paragraph.
    /// </summary>
    public static TiptapRichText FromString(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Empty();
        }

        return new TiptapRichText
        {
            Type = "doc",
            Content =
            [
                new TiptapNode
                {
                    Type = "paragraph",
                    Content = [new TiptapNode { Type = "text", Text = text }]
                }
            ]
        };
    }

    public bool IsEmpty()
    {
        return Length() == 0;
    }

    public int Length()
    {
        return Content?.Sum(node => node.Length()) ?? 0;
    }

    public override string ToString()
    {
        return string.Concat(Content?.Select(node => node.ToString()) ?? []);
    }
}
