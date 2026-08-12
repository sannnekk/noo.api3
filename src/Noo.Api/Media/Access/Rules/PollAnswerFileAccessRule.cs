using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Media.Types;

namespace Noo.Api.Media.Access.Rules;

/// <summary>
/// Restricts <see cref="MediaCategory.PollAnswerFile"/> downloads to the people who
/// may read the participation the file was submitted with: the participant who
/// uploaded it, and the staff roles that can open a poll's results.
/// </summary>
[RegisterScoped(typeof(IMediaAccessRule))]
public class PollAnswerFileAccessRule : IMediaAccessRule
{
    private static readonly IReadOnlySet<UserRoles> _staffRoles = new HashSet<UserRoles>
    {
        UserRoles.Admin,
        UserRoles.Teacher,
    };

    public IReadOnlySet<MediaCategory> Categories { get; } =
        new HashSet<MediaCategory> { MediaCategory.PollAnswerFile };

    public Task<MediaAccessDecision> EvaluateAsync(
        MediaAccessContext context,
        CancellationToken cancellationToken = default
    )
    {
        if (context.User.UserRole is { } role && _staffRoles.Contains(role))
        {
            return Task.FromResult(MediaAccessDecision.Allow());
        }

        if (context.User.UserId is not { } userId)
        {
            return Task.FromResult(MediaAccessDecision.Deny("Not authenticated"));
        }

        return Task.FromResult(
            context.Media.OwnerId == userId
                ? MediaAccessDecision.Allow()
                : MediaAccessDecision.Deny("File belongs to another participant")
        );
    }
}
