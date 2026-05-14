using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Core.Utils.Json;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Works.Models;

namespace Noo.Api.AssignedWorks.Models;

[Model("assigned_work_answer")]
public class AssignedWorkAnswerModel : BaseModel
{
    [RichTextColumn("rich_text_content")]
    public IRichTextType? RichTextContent { get; set; }

    [Column("word_content", TypeName = DbDataTypes.Varchar63)]
    public string? WordContent { get; set; }

    [RichTextColumn("mentor_comment")]
    public IRichTextType? MentorComment { get; set; }

    [Column("score", TypeName = DbDataTypes.SmallIntUnsigned)]
    [Range(0, 500)]
    public int? Score { get; set; }

    [Column("max_score", TypeName = DbDataTypes.SmallIntUnsigned)]
    [Range(0, 500)]
    public int MaxScore { get; set; }

    [Column("status", TypeName = AssignedWorkEnumDbDataTypes.AssignedWorkAnswerStatus)]
    [Required]
    public AssignedWorkAnswerStatus Status { get; set; } = AssignedWorkAnswerStatus.NotSubmitted;

    [JsonColumn("detailed_score")]
    public Dictionary<string, int>? DetailedScore { get; set; }

    [Column("task_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Task))]
    public Ulid TaskId { get; set; }

    [Column("assigned_work_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(AssignedWork))]
    public Ulid AssignedWorkId { get; set; }

    #region Navigation Properties

    public WorkTaskModel Task { get; set; } = default!;

    public AssignedWorkModel AssignedWork { get; set; } = default!;

    #endregion
}
