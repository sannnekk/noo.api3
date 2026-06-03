using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Sessions.Models;
using IndexAttribute = Microsoft.EntityFrameworkCore.IndexAttribute;

namespace Noo.Api.Auth.Models;

[Model("refresh_token")]
[Index(nameof(TokenHash), IsUnique = true)]
public class RefreshTokenModel : BaseModel
{
    [Required]
    [MaxLength(63)]
    [Column("token_hash", TypeName = DbDataTypes.Varchar63)]
    public string TokenHash { get; set; } = null!;

    [Required]
    [Column("expires_at", TypeName = DbDataTypes.DateTimeWithoutTZ)]
    public DateTime ExpiresAt { get; set; }

    [Column("used_at", TypeName = DbDataTypes.DateTimeWithoutTZ)]
    public DateTime? UsedAt { get; set; }

    [Required]
    [Column("session_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Session))]
    public Ulid SessionId { get; set; }

    #region Navigation Properties

    public SessionModel Session { get; set; } = null!;

    #endregion
}
