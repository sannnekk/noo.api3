using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Core.Utils.Json;
using Noo.Api.Users.Models;

namespace Noo.Api.AssignedWorks.Models;

[Model("assigned_work_history")]
public class AssignedWorkHistoryModel : BaseModel
{
    [Column("type", TypeName = AssignedWorkEnumDbDataTypes.AssignedWorkHistoryType)]
    public AssignedWorkHistoryType Type { get; set; } = default!;

    [Column("changed_at", TypeName = DbDataTypes.DateTimeWithoutTZ)]
    public DateTime ChangedAt { get; set; }

    [JsonColumn("value")]
    public Dictionary<string, string>? Value { get; set; }

    [Column("assigned_work_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(AssignedWork))]
    public Ulid AssignedWorkId { get; set; }

    [Column("changed_by_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(ChangedBy))]
    public Ulid? ChangedById { get; set; }

    #region Navigation Properties

    [InverseProperty(nameof(AssignedWorkModel.History))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public AssignedWorkModel AssignedWork { get; set; } = default!;

    [InverseProperty(nameof(UserModel.AssignedWorkHistoryChanges))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public UserModel? ChangedBy { get; set; }

    #endregion
}
