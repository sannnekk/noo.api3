using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Core.Utils.Richtext.Delta;
using Noo.Api.Core.Utils.Richtext.Tiptap;

namespace Noo.UnitTests.Core.Utils.Richtext;

public class TiptapRichTextTests
{
    [Fact]
    public void Deserialize_TiptapJson_ReturnsTiptapRichText()
    {
        const string json = """
        {
            "$type": "tiptap",
            "type": "doc",
            "content": [
                {
                    "type": "paragraph",
                    "content": [
                        { "type": "text", "text": "Hello ", "marks": [{ "type": "bold" }] },
                        { "type": "text", "text": "world" }
                    ]
                }
            ]
        }
        """;

        var result = RichTextJsonSerializer.Deserialize(json);

        var tiptap = Assert.IsType<TiptapRichText>(result);
        Assert.Equal("doc", tiptap.Type);
        Assert.False(tiptap.IsEmpty());
        Assert.Equal("Hello world".Length, tiptap.Length());
    }

    [Fact]
    public void Deserialize_DeltaJson_StillReturnsDeltaRichText()
    {
        const string json = """
        { "$type": "delta", "ops": [{ "insert": "Hello" }] }
        """;

        var result = RichTextJsonSerializer.Deserialize(json);

        Assert.IsType<DeltaRichText>(result);
    }

    [Fact]
    public void SerializeDeserialize_Tiptap_RoundTrips()
    {
        var original = new TiptapRichText
        {
            Type = "doc",
            Content =
            [
                new TiptapNode
                {
                    Type = "paragraph",
                    Content =
                    [
                        new TiptapNode { Type = "text", Text = "abc" }
                    ]
                }
            ]
        };

        var json = RichTextJsonSerializer.Serialize(original);
        var roundTripped = Assert.IsType<TiptapRichText>(RichTextJsonSerializer.Deserialize(json));

        Assert.Equal(original.Length(), roundTripped.Length());
        Assert.Equal(original.ToString(), roundTripped.ToString());
        Assert.Contains("\"$type\":\"tiptap\"", json);
    }

    [Fact]
    public void IsEmpty_TrueForEmptyDocument()
    {
        Assert.True(TiptapRichText.Empty().IsEmpty());

        var whitespaceOnly = new TiptapRichText
        {
            Type = "doc",
            Content = [new TiptapNode { Type = "paragraph" }]
        };

        Assert.True(whitespaceOnly.IsEmpty());
    }
}
