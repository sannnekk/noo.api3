using System.ComponentModel.DataAnnotations.Schema;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Courses.Types;
using Noo.Api.Users.Models;

namespace Noo.Api.Courses.Models;

[Model("course_reaction")]
public class CourseMaterialReactionModel : BaseModel
{
    [Column("material_content_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(MaterialContent))]
    public Ulid MaterialContentId { get; set; }

    [Column("user_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(User))]
    public Ulid UserId { get; set; }

    [Column("reaction", TypeName = "ENUM('check', 'thinking')")]
    public CourseMaterialReactionTypes Reaction { get; set; } = CourseMaterialReactionTypes.Check;

    #region Navigation properties

    [InverseProperty(nameof(CourseMaterialContentModel.Reactions))]
    public CourseMaterialContentModel MaterialContent { get; set; } = default!;

    [InverseProperty(nameof(UserModel.CourseMaterialReactions))]
    public UserModel User { get; set; } = default!;

    #endregion
}
