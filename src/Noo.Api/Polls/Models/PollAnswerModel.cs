using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Core.Utils.Json;
using Noo.Api.Media.Models;
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

    /// <summary>
    /// Files attached to the answer. Only a <see cref="PollQuestionType.Files"/>
    /// question ever has them.
    /// </summary>
    [InverseProperty(nameof(MediaModel.PollAnswers))]
    public ICollection<MediaModel>? Medias { get; set; }

    #endregion

    public string? StringValue()
    {
        return PollAnswerFormatter.Stringify(Value, Medias?.Select(media => media.ActualName));
    }
}
