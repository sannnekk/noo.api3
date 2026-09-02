using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;

namespace Noo.Api.Platform.Models;

/// <summary>
/// The links and contacts the frontend shows in its footer, its help section and
/// anywhere else it points a visitor off the platform.
/// </summary>
/// <remarks>
/// Exactly one row, addressed by <see cref="SingletonId"/> rather than by a
/// "take the first" query, so that the primary key is what keeps it single —
/// two admins saving at once collide on the key instead of creating two rows.
/// <para>
/// The property defaults are the values the frontend shipped with before these
/// became editable, so a database with no row yet behaves exactly as it did.
/// </para>
/// </remarks>
[Model("platform_settings")]
public class PlatformSettingsModel : BaseModel
{
    /// <summary>
    /// The id of the one and only settings row.
    /// </summary>
    public static readonly Ulid SingletonId = Ulid.Parse("00000000000000000000000000");

    [Column("shop_link", TypeName = DbDataTypes.Varchar255)]
    [Required]
    [MaxLength(255)]
    public string ShopLink { get; set; } = "https://no-os.ru";

    [Column("privacy_policy_link", TypeName = DbDataTypes.Varchar255)]
    [Required]
    [MaxLength(255)]
    public string PrivacyPolicyLink { get; set; } = "https://no-os.ru/confidentiality";

    [Column("terms_link", TypeName = DbDataTypes.Varchar255)]
    [Required]
    [MaxLength(255)]
    public string TermsLink { get; set; } = "https://no-os.ru/oferta";

    [Column("support_chat_link", TypeName = DbDataTypes.Varchar255)]
    [Required]
    [MaxLength(255)]
    public string SupportChatLink { get; set; } = "https://t.me/+oACQzPflwZQ1ODRi";

    [Column("support_chat_name", TypeName = DbDataTypes.Varchar255)]
    [Required]
    [MaxLength(255)]
    public string SupportChatName { get; set; } = "@noo_support_chat";

    [Column("support_email", TypeName = DbDataTypes.Varchar255)]
    [Required]
    [MaxLength(255)]
    public string SupportEmail { get; set; } = "noohelp@mail.ru";

    /// <summary>
    /// How long an answer from support takes, in the words shown to the reader.
    /// </summary>
    [Column("support_response_time", TypeName = DbDataTypes.Varchar255)]
    [Required]
    [MaxLength(255)]
    public string SupportResponseTime { get; set; } =
        "Обычно отвечаем в течение дня, в будни — быстрее";
}
