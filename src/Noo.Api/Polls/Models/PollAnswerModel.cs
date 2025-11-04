using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Core.Utils.Json;
using Noo.Api.Polls.Types;

namespace Noo.Api.Polls.Models;

[Model("poll_answer")]
public class PollAnswerModel : BaseModel
{
    [Column("poll_question_id", TypeName = DbDataTypes.Ulid)]
    [Required]
    [ForeignKey(nameof(PollQuestion))]
    public Ulid PollQuestionId { get; set; }

    [JsonColumn("value", Converter = typeof(PollAnswerValueConverter))]
    [Required]
    public PollAnswerValue Value { get; set; }

    #region Navigation Properties

    [DeleteBehavior(DeleteBehavior.Cascade)]
    public PollQuestionModel PollQuestion { get; set; } = default!;

    #endregion

    public string? StringValue()
    {
        switch (Value.Type)
        {
            case PollQuestionType.Text:
            case PollQuestionType.SingleChoice:
            case PollQuestionType.Number:
                return Value.Value?.ToString();
            case PollQuestionType.MultipleChoice:
                return Value.Value is IEnumerable<string> choices
                    ? string.Join(", ", choices)
                    : null;
            case PollQuestionType.Date:
                return Value.Value is DateTime date
                    ? date.ToString("yyyy.MM.dd")
                    : null;
            case PollQuestionType.DateTime:
                return Value.Value is DateTimeOffset dateTimeOffset
                    ? dateTimeOffset.ToString("yyyy.MM.dd HH:mm:ss zzz")
                    : null;
            case PollQuestionType.Checkbox:
                return Value.Value is bool boolValue
                    ? (boolValue ? "Да" : "Нет")
                    : null;
            case PollQuestionType.Rating:
                return Value.Value is int rating
                    ? rating.ToString()
                    : null;
            case PollQuestionType.Files:
                return "<Some files>"; // TODO: Implement file handling
            default:
                return "<Unknown question type>";
        }
    }
}
