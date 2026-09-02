using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Platform;

public class PlatformPolicies : IPolicyRegistrar
{
    public const string CanViewChangelog = nameof(CanViewChangelog);
    public const string CanUpdateSettings = nameof(CanUpdateSettings);

    public void RegisterPolicies(AuthorizationOptions options)
    {
        // Narrower than the support policies on purpose: these links are the
        // offer, the privacy policy and the shop, and a wrong value here is
        // visible to every visitor, signed in or not.
        options.AddPolicy(CanUpdateSettings, policy =>
        {
            policy.RequireRole(nameof(UserRoles.Admin)).RequireNotBlocked();
        });

        options.AddPolicy(CanViewChangelog, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher),
                nameof(UserRoles.Mentor),
                nameof(UserRoles.Assistant)
            ).RequireNotBlocked();
        });
    }
}
