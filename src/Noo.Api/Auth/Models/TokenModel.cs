using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Users.Models;

namespace Noo.Api.Auth.Models;

[Model("token")]
public class TokenModel : BaseModel
{
    [Required]
    [MinLength(1)]
    [MaxLength(63)]
    [Column("token", TypeName = DbDataTypes.Varchar63)]
    public string Token { get; set; } = null!;

    [Required]
    [Column("expires_at", TypeName = DbDataTypes.DateTimeWithoutTZ)]
    public DateTime ExpiresAt { get; set; }

    [Required]
    [Column("type", TypeName = AuthEnumDbDataTypes.TokenType)]
    public TokenType Type { get; set; }

    [MaxLength(127)]
    [Column("payload", TypeName = DbDataTypes.Varchar127)]
    public string? Payload { get; set; }

    [Required]
    [Column("user_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey("user_id")]
    public Ulid UserId { get; set; }

    #region Navigation Properties

    public UserModel User { get; set; } = null!;

    #endregion
}
