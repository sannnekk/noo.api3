using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Courses.Services;
using Noo.Api.Media.Types;

namespace Noo.Api.Media.Access.Rules;

/// <summary>
/// Restricts <see cref="MediaCategory.CourseAttachment"/> downloads
/// to course members; staff roles bypass the membership check.
/// </summary>
[RegisterScoped(typeof(IMediaAccessRule))]
public class CourseAttachmentAccessRule : IMediaAccessRule
{
    private static readonly IReadOnlySet<UserRoles> _staffRoles = new HashSet<UserRoles>
    {
        UserRoles.Admin,
        UserRoles.Teacher,
        UserRoles.Mentor,
        UserRoles.Assistant,
    };

    private readonly ICourseMembershipService _memberships;

    public CourseAttachmentAccessRule(ICourseMembershipService memberships)
    {
        _memberships = memberships;
    }

    public IReadOnlySet<MediaCategory> Categories { get; } =
        new HashSet<MediaCategory> { MediaCategory.CourseAttachment };

    public async Task<MediaAccessDecision> EvaluateAsync(
        MediaAccessContext context,
        CancellationToken cancellationToken = default
    )
    {
        if (context.User.UserRole is { } role && _staffRoles.Contains(role))
        {
            return MediaAccessDecision.Allow();
        }

        if (context.User.UserId is not { } userId)
        {
            return MediaAccessDecision.Deny("Not authenticated");
        }

        if (context.Media.EntityId is not { } courseId)
        {
            return MediaAccessDecision.Deny("Course attachment is not linked to a course");
        }

        var hasAccess = await _memberships.HasAccessAsync(courseId, userId);

        return hasAccess
            ? MediaAccessDecision.Allow()
            : MediaAccessDecision.Deny("User is not a member of the course");
    }
}
