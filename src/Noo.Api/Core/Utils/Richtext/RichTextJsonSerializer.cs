using System.Text.Json;

namespace Noo.Api.Core.Utils.Richtext;

public static class RichTextJsonSerializer
{
    private static JsonSerializerOptions _options = new()
    {
        AllowOutOfOrderMetadataProperties = true,
    };

    public static IRichTextType? Deserialize(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<IRichTextType>(json, _options);
    }

    public static string? Serialize(IRichTextType? richText)
    {
        if (richText == null)
        {
            return null;
        }

        return JsonSerializer.Serialize<IRichTextType?>((IRichTextType)richText, _options);
    }
}
