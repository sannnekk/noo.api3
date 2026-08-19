using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Core.Utils.Richtext.Tiptap;

namespace Noo.UnitTests.Core.Utils;

public class RichTextJsonSerializerTests
{
    [Fact]
    public void Serialize_IncludesTypeDiscriminator()
    {
        var richText = RichTextFactory.Create("abc");

        var json = RichTextJsonSerializer.Serialize(richText);

        Assert.NotNull(json);
        Assert.Contains("\"$type\":\"tiptap\"", json);
    }

    [Fact]
    public void Deserialize_PayloadWithType_ReturnsTheFormatItNames()
    {
        const string typedPayload = "{\"$type\":\"tiptap\",\"type\":\"doc\",\"content\":[]}";

        var result = RichTextJsonSerializer.Deserialize(typedPayload);

        Assert.IsType<TiptapRichText>(result);
    }

    /// <summary>
    /// A format nobody serves any more is not silently read as the one that is.
    /// Nothing in the database should carry a discriminator like this, and if
    /// something does, failing to read it beats reading it wrongly.
    /// </summary>
    [Fact]
    public void Deserialize_PayloadOfAnUnknownFormat_Throws()
    {
        const string deltaPayload = "{\"$type\":\"delta\",\"ops\":[]}";

        Assert.ThrowsAny<Exception>(() => RichTextJsonSerializer.Deserialize(deltaPayload));
    }
}
