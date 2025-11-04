using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.Calendar.Models;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Courses.Models;
using Noo.Api.NooTube.Models;
using Noo.Api.Notifications.Models;
using Noo.Api.Polls.Models;
using Noo.Api.Sessions.Models;
using Noo.Api.Snippets.Models;
using Noo.Api.UserSettings.Models;
using IndexAttribute = Microsoft.EntityFrameworkCore.IndexAttribute;

namespace Noo.Api.Users.Models;

[Model("user")]
[Index(nameof(Name), IsUnique = false)]
[Index(nameof(Username), IsUnique = true)]
[Index(nameof(Email), IsUnique = true)]
[Index(nameof(TelegramId), IsUnique = true)]
[Index(nameof(TelegramUsername), IsUnique = false)]
public class UserModel : BaseModel
{
    [Required]
    [MinLength(1)]
    [MaxLength(255)]
    [Column("name", TypeName = DbDataTypes.Varchar255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(63)]
    [Column("username", TypeName = DbDataTypes.Varchar63)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Column("email", TypeName = DbDataTypes.Varchar255)]
    public string Email { get; set; } = string.Empty;

    [Column("telegram_id", TypeName = DbDataTypes.Varchar63)]
    [MaxLength(63)]
    public string? TelegramId { get; set; }

    [Column("telegram_username", TypeName = DbDataTypes.Varchar255)]
    [MaxLength(255)]
    public string? TelegramUsername { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(255)]
    [Column("password_hash", TypeName = DbDataTypes.Varchar255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [Column("role", TypeName = DbDataTypes.UserRolesEnum)]
    public UserRoles Role { get; set; } = UserRoles.Student;

    [Required]
    [Column("is_blocked", TypeName = DbDataTypes.Boolean)]
    public bool IsBlocked { get; set; }

    [Required]
    [Column("is_verified", TypeName = DbDataTypes.Boolean)]
    public bool IsVerified { get; set; }

    #region Navigation Properties

    // Many-to-many relationship on other side
    public ICollection<CourseModel> CoursesAsAuthor { get; set; } = [];

    // Many-to-many relationship on other side
    public ICollection<CourseModel> CoursesAsEditor { get; set; } = [];

    public UserAvatarModel? Avatar { get; set; }

    [InverseProperty(nameof(CourseMembershipModel.Student))]
    public ICollection<CourseMembershipModel> CoursesAsMember { get; set; } = [];

    [InverseProperty(nameof(CourseMembershipModel.Assigner))]
    public ICollection<CourseMembershipModel> CoursesAsAssigner { get; set; } = [];

    public ICollection<CourseMaterialReactionModel> CourseMaterialReactions { get; set; } = [];

    public ICollection<SessionModel> Sessions { get; set; } = [];

    public ICollection<SnippetModel> Snippets { get; set; } = [];

    public ICollection<PollParticipationModel> PollParticipations { get; set; } = [];

    public ICollection<CalendarEventModel> CalendarEvents { get; set; } = [];

    public ICollection<NotificationModel> Notifications { get; set; } = [];

    public UserSettingsModel? Settings { get; set; }

    public ICollection<NooTubeVideoModel> UploadedVideos { get; set; } = [];

    public ICollection<NooTubeVideoCommentModel> NooTubeVideoComments { get; set; } = [];

    public ICollection<NooTubeVideoReactionModel> NooTubeVideoReactions { get; set; } = [];

    public ICollection<AssignedWorkStatusHistoryModel> AssignedWorkHistoryChanges { get; set; } = [];

    // Mentor / Student assignments
    [InverseProperty(nameof(MentorAssignmentModel.Student))]
    public ICollection<MentorAssignmentModel> MentorAssignmentsAsStudent { get; set; } = [];

    [InverseProperty(nameof(MentorAssignmentModel.Mentor))]
    public ICollection<MentorAssignmentModel> MentorAssignmentsAsMentor { get; set; } = [];

    #endregion
}
