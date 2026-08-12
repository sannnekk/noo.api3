using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Noo.Api.Media;

namespace Noo.Api.Polls.Types;

public struct PollQuestionConfig
{
    [JsonPropertyName("type")]
    [Required]
    public PollQuestionType Type { get; set; }

    [JsonPropertyName("minChoices")]
    public int? MinChoices { get; set; }

    [JsonPropertyName("maxChoices")]
    public int? MaxChoices { get; set; }

    [JsonPropertyName("minTextLength")]
    public int? MinTextLength { get; set; }

    [JsonPropertyName("maxTextLength")]
    public int? MaxTextLength { get; set; }

    [JsonPropertyName("minIntValue")]
    public int? MinIntValue { get; set; }

    [JsonPropertyName("maxIntValue")]
    public int? MaxIntValue { get; set; }

    [JsonPropertyName("minRating")]
    public int? MinRating { get; set; }

    [JsonPropertyName("maxRating")]
    public int? MaxRating { get; set; }

    /// <summary>
    /// Maximum file size in bytes
    /// </summary>
    [JsonPropertyName("maxFileSize")]
    [Range(1, MediaConfig.MaxFileSize)]
    public int? MaxFileSize { get; set; }

    /// <summary>
    /// Allowed file types (MIME types)
    /// </summary>
    [JsonPropertyName("allowedFileTypes")]
    [MaxLength(5)]
    public string[]? AllowedFileTypes { get; set; }

    [JsonPropertyName("maxFileCount")]
    [Range(1, PollFileAnswerLimits.MaxFileCount)]
    public int? MaxFileCount { get; set; }

    internal static PollQuestionConfig Deserialize(string v)
    {
        return JsonSerializer.Deserialize<PollQuestionConfig>(v);
    }

    internal string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }
}
