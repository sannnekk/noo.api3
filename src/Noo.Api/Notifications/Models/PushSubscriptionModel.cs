using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Users.Models;
using IndexAttribute = Microsoft.EntityFrameworkCore.IndexAttribute;

namespace Noo.Api.Notifications.Models;

[Model("push_subscription")]
[Index(nameof(Endpoint), IsUnique = true)]
public class PushSubscriptionModel : BaseModel
{
    [Column("user_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(User))]
    [Required]
    public Ulid UserId { get; set; }

    [Column("endpoint", TypeName = DbDataTypes.Varchar1024)]
    [MaxLength(1024)]
    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Column("p256dh", TypeName = DbDataTypes.Varchar127)]
    [MaxLength(127)]
    [Required]
    public string P256dh { get; set; } = string.Empty;

    [Column("auth", TypeName = DbDataTypes.Varchar63)]
    [MaxLength(63)]
    [Required]
    public string Auth { get; set; } = string.Empty;

    [Column("user_agent", TypeName = DbDataTypes.Varchar512)]
    [MaxLength(512)]
    public string? UserAgent { get; set; }

    #region Navigation Props

    [InverseProperty(nameof(UserModel.PushSubscriptions))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel User { get; set; } = default!;

    #endregion
}
