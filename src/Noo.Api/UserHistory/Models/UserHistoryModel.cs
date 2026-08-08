using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Core.Utils.Json;
using Noo.Api.UserHistory.Types;
using Noo.Api.Users.Models;
using IndexAttribute = Microsoft.EntityFrameworkCore.IndexAttribute;

namespace Noo.Api.UserHistory.Models;

/// <summary>
/// One entry in a user's activity log.
/// </summary>
/// <remarks>
/// The indexes pair the user column with <c>Id</c> because ULIDs are time-ordered and
/// <c>ApplyDefaultOrdering</c> sorts by <c>Id DESC</c>: one index then serves both the filter and
/// the sort, keeping a user's page cost independent of how large the table has grown.
///
/// There is deliberately no <c>changed_at</c> column — <see cref="BaseModel.CreatedAt"/> already
/// records when the entry was written, and these rows are never updated.
/// </remarks>
[Model("user_history")]
[Index(nameof(SubjectUserId), nameof(Id))]
[Index(nameof(ActorUserId), nameof(Id))]
public class UserHistoryModel : BaseModel
{
    [Column("type", TypeName = DbDataTypes.Varchar63)]
    [MaxLength(63)]
    [Required]
    public UserHistoryType Type { get; set; }

    /// <summary>
    /// The user whose history this entry belongs to.
    /// </summary>
    [Column("subject_user_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(SubjectUser))]
    [Required]
    public Ulid SubjectUserId { get; set; }

    /// <summary>
    /// Who performed the action. Null when the subject acted on their own behalf or when the
    /// action was performed by the system.
    /// </summary>
    [Column("actor_user_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Actor))]
    public Ulid? ActorUserId { get; set; }

    /// <summary>
    /// Display data captured when the entry was written, so rendering needs no joins and the
    /// entry stays readable after the entities it refers to are renamed or deleted.
    /// </summary>
    [JsonColumn("payload")]
    public Dictionary<string, string>? Payload { get; set; }

    #region Navigation Properties

    [InverseProperty(nameof(UserModel.History))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel SubjectUser { get; set; } = default!;

    [InverseProperty(nameof(UserModel.HistoryActions))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public UserModel? Actor { get; set; }

    #endregion
}
