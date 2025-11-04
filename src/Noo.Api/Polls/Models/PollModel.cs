using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;

namespace Noo.Api.Polls.Models;

[Model("poll")]
public class PollModel : BaseModel
{
    [Column("title", TypeName = DbDataTypes.Varchar255)]
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Column("description", TypeName = DbDataTypes.Varchar512)]
    [MaxLength(512)]
    public string? Description { get; set; }

    [Column("is_active", TypeName = DbDataTypes.Boolean)]
    [Required]
    public bool IsActive { get; set; } = true;

    [Column("expires_at", TypeName = DbDataTypes.DateTimeWithoutTZ)]
    public DateTime ExpiresAt { get; set; }

    [Column("is_auth_required", TypeName = DbDataTypes.Boolean)]
    [Required]
    public bool IsAuthRequired { get; set; } = true;

    #region Navigation Properties

    public ICollection<PollQuestionModel> Questions { get; set; } = [];

    public ICollection<PollParticipationModel> Participations { get; set; } = [];

    #endregion
}
