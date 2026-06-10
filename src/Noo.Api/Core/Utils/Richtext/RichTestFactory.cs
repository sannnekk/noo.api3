using Noo.Api.Core.Utils.Richtext.Tiptap;

namespace Noo.Api.Core.Utils.Richtext;

public static class RichTextFactory
{
    public static IRichTextType Create(string? text = null)
    {
        return TiptapRichText.FromString(text);
    }
}
