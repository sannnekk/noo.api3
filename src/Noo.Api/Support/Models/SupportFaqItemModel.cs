using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Support.Types;

namespace Noo.Api.Support.Models;

/// <summary>
/// One question and its answer on the help home page.
/// </summary>
/// <remarks>
/// A FAQ item is not a short article: it is read where it stands, in an
/// accordion, and only points at <see cref="SupportArticleModel"/> for the rest.
/// The category is therefore optional — "I forgot my password" belongs to no
/// category in particular, and an item without one simply carries no link on.
/// </remarks>
[Model("support_faq_item")]
public class SupportFaqItemModel : OrderedModel
{
    [Column("question", TypeName = DbDataTypes.Varchar255)]
    [Required]
    [MaxLength(255)]
    public string Question { get; set; } = string.Empty;

    [RichTextColumn("answer")]
    public IRichTextType Answer { get; set; } = RichTextFactory.Create("\n");

    [Required]
    [Column("is_active", TypeName = DbDataTypes.Boolean)]
    public bool IsActive { get; set; } = true;

    [Column("category", TypeName = SupportDbDataTypes.Category)]
    public SupportCategory? Category { get; set; }
}
