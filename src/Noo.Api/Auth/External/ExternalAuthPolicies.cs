using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Auth.External;

public class ExternalAuthPolicies : IPolicyRegistrar
{
    public const string CanManageOwnIdentities = nameof(CanManageOwnIdentities);

    public void RegisterPolicies(AuthorizationOptions options)
    {
        // Linking and unlinking only ever touch the caller's own identities.
        options.AddPolicy(
            CanManageOwnIdentities,
            policy =>
            {
                policy.RequireAuthenticatedUser().RequireNotBlocked();
            }
        );
    }
}
