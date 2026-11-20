using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.UserHistory;

public class UserHistoryPolicies : IPolicyRegistrar
{
    public const string CanGetUserHistory = nameof(CanGetUserHistory);

    public void RegisterPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(
            CanGetUserHistory,
            policy =>
            {
                policy
                    .RequireRole(
                        nameof(UserRoles.Admin),
                        nameof(UserRoles.Teacher),
                        nameof(UserRoles.Assistant)
                    )
                    .RequireNotBlocked();
            }
        );
    }
}
