using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Calendar.Models;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Core.Utils.Ulid;
using Noo.Api.Users.Models;
using Noo.Api.Works.Models;
using Noo.Api.Works.Types;
using IndexAttribute = Microsoft.EntityFrameworkCore.IndexAttribute;

namespace Noo.Api.AssignedWorks.Models;

[Model("assigned_work")]
[Index(nameof(Title), IsUnique = false)]
[Index(nameof(SolveStatus), IsUnique = false)]
[Index(nameof(CheckStatus), IsUnique = false)]
public class AssignedWorkModel : BaseModel
{
    [Column("title", TypeName = DbDataTypes.Varchar512)]
    [Required]
    public string Title { get; set; } = default!;

    [Column("type", TypeName = WorkEnumDbDataTypes.WorkType)]
    [Required]
    public WorkType Type { get; set; } = default!;

    [Column("attempt", TypeName = DbDataTypes.TinyIntUnsigned)]
    [Required]
    public int Attempt { get; set; }

    [Column("solve_status", TypeName = AssignedWorkEnumDbDataTypes.AssignedWorkSolveStatus)]
    [Required]
    public AssignedWorkSolveStatus SolveStatus { get; set; } = AssignedWorkSolveStatus.NotSolved;

    [Column("solve_deadline_at", TypeName = DbDataTypes.DateTimeWithoutTZ)]
    public DateTime? SolveDeadlineAt { get; set; }

    [Column("is_solve_deadline_shifted", TypeName = DbDataTypes.Boolean)]
    public bool IsSolveDeadlineShifted { get; set; }

    [Column("solved_at", TypeName = DbDataTypes.DateTimeWithoutTZ)]
    public DateTime? SolvedAt { get; set; }

    [Column("check_status", TypeName = AssignedWorkEnumDbDataTypes.AssignedWorkCheckStatus)]
    [Required]
    public AssignedWorkCheckStatus CheckStatus { get; set; } = AssignedWorkCheckStatus.NotChecked;

    [Column("check_deadline_at", TypeName = DbDataTypes.DateTimeWithoutTZ)]
    public DateTime? CheckDeadlineAt { get; set; }

    [Column("checked_at", TypeName = DbDataTypes.DateTimeWithoutTZ)]
    public DateTime? CheckedAt { get; set; }

    [Column("is_check_deadline_shifted", TypeName = DbDataTypes.Boolean)]
    public bool IsCheckDeadlineShifted { get; set; }

    [Column("score", TypeName = DbDataTypes.SmallIntUnsigned)]
    [Range(0, 5000)]
    public int? Score { get; set; }

    [Column("max_score", TypeName = DbDataTypes.SmallIntUnsigned)]
    [Range(0, 5000)]
    public int MaxScore { get; set; }

    [Column("is_archived_by_student", TypeName = DbDataTypes.Boolean)]
    [Required]
    public bool IsArchivedByStudent { get; set; }

    [Column("is_archived_by_mentors", TypeName = DbDataTypes.Boolean)]
    [Required]
    public bool IsArchivedByMentors { get; set; }

    [Column("is_archived_by_assistants", TypeName = DbDataTypes.Boolean)]
    public bool IsArchivedByAssistants { get; set; }

    [UlidArrayColumn("excluded_task_ids")]
    public Ulid[]? ExcludedTaskIds { get; set; }

    [Column("student_comment_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(StudentComment))]
    public Ulid? StudentCommentId { get; set; }

    [Column("main_mentor_comment_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(MainMentorComment))]
    public Ulid? MainMentorCommentId { get; set; }

    [Column("helper_mentor_comment_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(HelperMentorComment))]
    public Ulid? HelperMentorCommentId { get; set; }

    [Column("student_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Student))]
    public Ulid StudentId { get; set; }

    [Column("main_mentor_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(MainMentor))]
    public Ulid MainMentorId { get; set; }

    [Column("helper_mentor_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(HelperMentor))]
    public Ulid? HelperMentorId { get; set; }

    [Column("work_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Work))]
    public Ulid? WorkId { get; set; }

    #region Navigation Properties

    [DeleteBehavior(DeleteBehavior.SetNull)]
    public WorkModel? Work { get; set; }

    [DeleteBehavior(DeleteBehavior.NoAction)]
    public UserModel Student { get; set; } = default!;

    [DeleteBehavior(DeleteBehavior.NoAction)]
    public UserModel MainMentor { get; set; } = default!;

    [DeleteBehavior(DeleteBehavior.SetNull)]
    public UserModel? HelperMentor { get; set; }

    [DeleteBehavior(DeleteBehavior.SetNull)]
    public AssignedWorkCommentModel? StudentComment { get; set; }

    [DeleteBehavior(DeleteBehavior.SetNull)]
    public AssignedWorkCommentModel? MainMentorComment { get; set; }

    [DeleteBehavior(DeleteBehavior.SetNull)]
    public AssignedWorkCommentModel? HelperMentorComment { get; set; }

    public ICollection<AssignedWorkAnswerModel> Answers { get; set; } = [];

    public ICollection<AssignedWorkStatusHistoryModel> StatusHistory { get; set; } = [];

    public ICollection<CalendarEventModel> Events { get; set; } = [];

    #endregion

    public bool IsChecked() => CheckedAt.HasValue;

    public bool IsSolved() => SolvedAt.HasValue;

    public bool IsRemakeable() => IsChecked() && Type == WorkType.Test;

    public AssignedWorkModel NewAttemptCopy()
    {
        return new AssignedWorkModel
        {
            WorkId = WorkId,
            Title = $"[Пересдача] {Title}",
            Attempt = Attempt + 1,
            StudentId = StudentId,
            ExcludedTaskIds = ExcludedTaskIds
        };
    }
}
