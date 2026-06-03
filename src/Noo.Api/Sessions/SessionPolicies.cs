using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Sessions;

public class SessionPolicies : IPolicyRegistrar
{
    public const string CanGetOwnSessions = nameof(CanGetOwnSessions);
    public const string CanDeleteOwnSessions = nameof(CanDeleteOwnSessions);
    public const string CanGetOnlineInfo = nameof(CanGetOnlineInfo);

    public void RegisterPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(
            CanGetOwnSessions,
            policy =>
            {
                policy.RequireAuthenticatedUser().RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanDeleteOwnSessions,
            policy =>
            {
                policy.RequireAuthenticatedUser().RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanGetOnlineInfo,
            policy =>
            {
                policy
                    .RequireRole(
                        nameof(UserRoles.Admin),
                        nameof(UserRoles.Teacher),
                        nameof(UserRoles.Assistant),
                        nameof(UserRoles.Mentor)
                    )
                    .RequireNotBlocked();
            }
        );
    }
}
