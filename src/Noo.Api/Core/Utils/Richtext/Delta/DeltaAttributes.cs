using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Core.Validation;

namespace Noo.Api.Core.Utils.Richtext.Delta;

public class DeltaAttributes
{
    [JsonPropertyName("bold")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Bold { get; set; }

    [JsonPropertyName("italic")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Italic { get; set; }

    [JsonPropertyName("underline")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Underline { get; set; }

    [JsonPropertyName("strike")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Strike { get; set; }

    [JsonPropertyName("script")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DeltaScriptAttribute? Script { get; set; }

    [JsonPropertyName("link")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Url]
    public string? Link { get; set; }

    [JsonPropertyName("header")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Range(1, 6)]
    public int? Header { get; set; }

    [JsonPropertyName("list")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DeltaListAttribute? List { get; set; }

    [JsonPropertyName("align")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DeltaAlignAttribute? Align { get; set; }

    [JsonPropertyName("color")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Color
    {
        get => _parsedColorValue; set
        {
            if (StringValidation.IsCSSVariable(value))
            {
                _parsedColorValue = value;
            }
            else
            {
                _parsedColorValue = null;
            }
        }
    }
    private string? _parsedColorValue;

    [JsonPropertyName("comment")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Comment { get; set; }

    [JsonPropertyName("image-comment")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ImageComment { get; set; }
}

public enum DeltaScriptAttribute
{
    Sub,
    Super
}

public enum DeltaListAttribute
{
    Ordered,
    Bullet
}

public enum DeltaAlignAttribute
{
    Left,
    Center,
    Right,
    Justify
}
