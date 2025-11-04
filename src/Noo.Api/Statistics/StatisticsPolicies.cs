using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Statistics;

public class StatisticsPolicies : IPolicyRegistrar
{
    public const string CanGetPlatformStatistics = nameof(CanGetPlatformStatistics);
    public const string CanGetUserStatistics = nameof(CanGetUserStatistics);

    public void RegisterPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(CanGetPlatformStatistics, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked();
        });

        options.AddPolicy(CanGetUserStatistics, policy =>
        {
            policy
                .RequireAuthenticatedUser()
                .RequireNotBlocked()
                .AddRequirements(new AuthorizationRequirements.UserStatisticsAccessRequirement());
        });
    }
}
