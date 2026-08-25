using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Courses.Types;
using Noo.Api.Users.Models;
using IndexAttribute = Microsoft.EntityFrameworkCore.IndexAttribute;

namespace Noo.Api.Courses.Models;

/// <summary>
/// Grants a course to a whole population at once. A single <see cref="CourseAudienceKind.Everyone"/>
/// row is what makes a course public, so flipping public/private is one insert or delete no matter
/// how many students exist.
/// </summary>
[Model("course_audience")]
[Index(nameof(CourseId), nameof(Kind), nameof(TargetId), IsUnique = true)]
public class CourseAudienceModel : BaseModel
{
    [Column("course_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Course))]
    public Ulid CourseId { get; set; }

    [Column("kind", TypeName = CourseEnumDbDataTypes.CourseAudienceKind)]
    public CourseAudienceKind Kind { get; set; }

    /// <summary>
    /// Null for <see cref="CourseAudienceKind.Everyone"/>; carries the tier id once subscriptions land.
    /// </summary>
    [Column("target_id", TypeName = DbDataTypes.Ulid)]
    public Ulid? TargetId { get; set; }

    [Column("granted_by_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(GrantedBy))]
    public Ulid? GrantedById { get; set; }

    #region Navigation Properties

    [DeleteBehavior(DeleteBehavior.Cascade)]
    [InverseProperty(nameof(CourseModel.Audiences))]
    public CourseModel Course { get; set; } = default!;

    [DeleteBehavior(DeleteBehavior.SetNull)]
    [InverseProperty(nameof(UserModel.CourseAudiencesAsGranter))]
    public UserModel? GrantedBy { get; set; }

    #endregion
}
