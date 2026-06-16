using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Works.Types;

namespace Noo.Api.Works.Models;

[Model("work_task")]
public class WorkTaskModel : OrderedModel
{
    [RichTextColumn("content")]
    [Required]
    public IRichTextType? Content { get; set; }

    [RichTextColumn("solve_hint")]
    public IRichTextType? SolveHint { get; set; }

    [RichTextColumn("explanation")]
    public IRichTextType? Explanation { get; set; }

    [Column("right_answers", TypeName = DbDataTypes.StringArray)]
    [MaxLength(16)]
    public IEnumerable<string>? RightAnswers { get; set; }

    [Column("type", TypeName = WorkEnumDbDataTypes.WorkTaskType)]
    [Required]
    public WorkTaskType Type { get; set; }

    [Column("check_strategy", TypeName = WorkEnumDbDataTypes.WorkTaskCheckSteategy)]
    [Required]
    public WorkTaskCheckStrategy CheckStrategy { get; set; } = WorkTaskCheckStrategy.Manual;

    [Column("max_score", TypeName = DbDataTypes.TinyIntUnsigned)]
    [Required]
    [Range(0, int.MaxValue)]
    public int MaxScore { get; set; }

    [NotMapped]
    public double? AverageScore { get; set; }

    [Column("show_answer_before_check", TypeName = DbDataTypes.Boolean)]
    public bool ShowAnswerBeforeCheck { get; set; }

    [Column("check_one_by_one", TypeName = DbDataTypes.Boolean)]
    public bool CheckOneByOne { get; set; }

    [Column("work_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Work))]
    public Ulid WorkId { get; set; }

    #region Navigation Properties

    [DeleteBehavior(DeleteBehavior.Cascade)]
    [InverseProperty(nameof(WorkModel.Tasks))]
    public WorkModel? Work { get; set; }

    #endregion

    public bool IsAutomaticallyCheckable =>
        Type switch
        {
            WorkTaskType.Word => true,
            _ => false,
        };
}
