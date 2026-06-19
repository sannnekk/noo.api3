using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.NooTube;

public class NooTubePolicies : IPolicyRegistrar
{
    public const string CanGetNooTubeVideos = nameof(CanGetNooTubeVideos);

    public const string CanEditNooTubeVideos = nameof(CanEditNooTubeVideos);

    public const string CanDeleteNooTubeVideos = nameof(CanDeleteNooTubeVideos);

    public const string CanCommentOnNooTubeVideos = nameof(CanCommentOnNooTubeVideos);

    public const string CanEditNooTubeComments = nameof(CanEditNooTubeComments);

    public const string CanDeleteNooTubeComments = nameof(CanDeleteNooTubeComments);

    public void RegisterPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(
            CanGetNooTubeVideos,
            policy =>
            {
                policy.RequireAuthenticatedUser().RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanEditNooTubeVideos,
            policy =>
            {
                policy
                    .RequireAuthenticatedUser()
                    .RequireNotBlocked()
                    .RequireRole(nameof(UserRoles.Admin), nameof(UserRoles.Teacher));
            }
        );

        options.AddPolicy(
            CanDeleteNooTubeVideos,
            policy =>
            {
                policy
                    .RequireAuthenticatedUser()
                    .RequireNotBlocked()
                    .RequireRole(nameof(UserRoles.Admin), nameof(UserRoles.Teacher));
            }
        );

        options.AddPolicy(
            CanCommentOnNooTubeVideos,
            policy =>
            {
                policy.RequireAuthenticatedUser().RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanEditNooTubeComments,
            policy =>
            {
                policy.RequireAuthenticatedUser().RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanDeleteNooTubeComments,
            policy =>
            {
                policy.RequireAuthenticatedUser().RequireNotBlocked();
            }
        );
    }
}
