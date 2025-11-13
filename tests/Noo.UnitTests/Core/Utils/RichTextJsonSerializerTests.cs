using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Core.Utils.Richtext.Delta;

namespace Noo.UnitTests.Core.Utils;

public class RichTextJsonSerializerTests
{
    [Fact]
    public void Serialize_IncludesTypeDiscriminator()
    {
        var richText = DeltaRichText.FromString("abc");

        var json = RichTextJsonSerializer.Serialize(richText);

        Assert.NotNull(json);
        Assert.Contains("\"$type\":\"delta\"", json);
    }

    [Fact]
    public void Deserialize_PayloadWithType_ReturnsDeltaRichText()
    {
        const string typedPayload = "{\"$type\":\"delta\",\"ops\":[]}";

        var result = RichTextJsonSerializer.Deserialize(typedPayload);

        Assert.IsType<DeltaRichText>(result);
    }
}
