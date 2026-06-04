using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Support.Types;

namespace Noo.Api.Support.Models;

[Model("support_article")]
public class SupportArticleModel : OrderedModel
{
    [Column("title", TypeName = DbDataTypes.Varchar255)]
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Column("slug", TypeName = DbDataTypes.Varchar255)]
    [Required]
    [MaxLength(255)]
    public string Slug { get; set; } = string.Empty;

    [RichTextColumn("content")]
    public IRichTextType Content { get; set; } = RichTextFactory.Create("\n");

    [Required]
    [Column("is_active", TypeName = DbDataTypes.Boolean)]
    public bool IsActive { get; set; } = true;

    [Required]
    [Column("category", TypeName = SupportDbDataTypes.Category)]
    public SupportCategory Category { get; set; } = SupportCategory.Courses;
}
