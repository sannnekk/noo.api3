using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Auth.External.Types;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Users.Models;
using IndexAttribute = Microsoft.EntityFrameworkCore.IndexAttribute;

namespace Noo.Api.Auth.External.Models;

[Model("user_identity")]
[Index(nameof(Provider), nameof(SubjectId), IsUnique = true)]
[Index(nameof(UserId), nameof(Provider), IsUnique = true)]
public class UserIdentityModel : BaseModel
{
    [Required]
    [Column("provider", TypeName = ExternalAuthEnumDbDataTypes.ExternalAuthProviderType)]
    public ExternalAuthProviderType Provider { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(127)]
    [Column("subject_id", TypeName = DbDataTypes.Varchar127)]
    public string SubjectId { get; set; } = null!;

    /// <summary>As the provider reported it, for display only. The account's own email lives on the user.</summary>
    [MaxLength(255)]
    [Column("email", TypeName = DbDataTypes.Varchar255)]
    public string? Email { get; set; }

    [MaxLength(255)]
    [Column("display_name", TypeName = DbDataTypes.Varchar255)]
    public string? DisplayName { get; set; }

    [Column("last_login_at", TypeName = DbDataTypes.DateTimeWithoutTZ)]
    public DateTime? LastLoginAt { get; set; }

    [Required]
    [Column("user_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(User))]
    public Ulid UserId { get; set; }

    #region Navigation Properties

    [DeleteBehavior(DeleteBehavior.Cascade)]
    [InverseProperty(nameof(UserModel.ExternalIdentities))]
    public UserModel User { get; set; } = null!;

    #endregion
}
