using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Media.Models;
using Noo.Api.NooTube.Models;
using Noo.Api.Polls.Models;

namespace Noo.Api.Courses.Models;

[Model("course_material_content")]
public class CourseMaterialContentModel : BaseModel
{
    [RichTextColumn("content")]
    public IRichTextType? Content { get; set; }

    [Column("poll_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Poll))]
    public Ulid? PollId { get; set; }

    #region Navigation properties

    public ICollection<CourseWorkAssignmentModel> WorkAssignments { get; set; } = default!;

    [DeleteBehavior(DeleteBehavior.Cascade)]
    public CourseMaterialModel Material { get; set; } = default!;

    [InverseProperty(nameof(PollModel.CourseMaterialContents))]
    public PollModel? Poll { get; set; }

    [InverseProperty(nameof(NooTubeVideoModel.CourseMaterialContents))]
    public ICollection<NooTubeVideoModel>? NooTubeVideos { get; set; }

    [InverseProperty(nameof(MediaModel.CourseMaterialContents))]
    public ICollection<MediaModel>? Medias { get; set; }

    [InverseProperty(nameof(CourseMaterialReactionModel.MaterialContent))]
    public ICollection<CourseMaterialReactionModel>? Reactions { get; set; }

    #endregion
}
