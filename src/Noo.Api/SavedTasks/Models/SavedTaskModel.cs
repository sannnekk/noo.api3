using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Users.Models;
using Noo.Api.Works.Models;
using IndexAttribute = Microsoft.EntityFrameworkCore.IndexAttribute;

namespace Noo.Api.SavedTasks.Models;

[Model("saved_task")]
[Index(nameof(UserId), nameof(TaskId), IsUnique = true)]
public class SavedTaskModel : BaseModel
{
    [Column("user_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(User))]
    [Required]
    public Ulid UserId { get; set; }

    [Column("task_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Task))]
    [Required]
    public Ulid TaskId { get; set; }

    [Column("assigned_work_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(AssignedWork))]
    public Ulid? AssignedWorkId { get; set; }

    #region Navigation Properties

    [InverseProperty(nameof(UserModel.SavedTasks))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel User { get; set; } = default!;

    [DeleteBehavior(DeleteBehavior.Cascade)]
    public WorkTaskModel Task { get; set; } = default!;

    [DeleteBehavior(DeleteBehavior.SetNull)]
    public AssignedWorkModel? AssignedWork { get; set; }

    #endregion
}
