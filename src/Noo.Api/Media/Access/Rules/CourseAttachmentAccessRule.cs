using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Courses.Services;
using Noo.Api.Media.Types;

namespace Noo.Api.Media.Access.Rules;

/// <summary>
/// Restricts <see cref="MediaCategory.CourseAttachment"/> downloads
/// to students who can reach the course; staff roles bypass the check.
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

    private readonly ICourseAccessService _access;

    private readonly ICourseContentRepository _contents;

    public CourseAttachmentAccessRule(
        ICourseAccessService access,
        ICourseContentRepository contents
    )
    {
        _access = access;
        _contents = contents;
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

        if (context.Media.EntityId is not { } entityId)
        {
            return MediaAccessDecision.Deny("Course attachment is not linked to a course");
        }

        // The uploader tags an attachment with the material content it belongs to, so the course
        // has to be resolved through that. Older rows carry the course id itself, hence the fallback.
        var courseId = await _contents.GetCourseIdByContentIdAsync(entityId) ?? entityId;

        var hasAccess = await _access.HasAccessAsync(courseId, userId);

        return hasAccess
            ? MediaAccessDecision.Allow()
            : MediaAccessDecision.Deny("User has no access to the course");
    }
}
