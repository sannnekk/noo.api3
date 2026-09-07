using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Core.System.Collaboration;

public class CollaborationPolicies : IPolicyRegistrar
{
    /// <summary>
    /// The role check for opening any room at all. Whether this particular document may be
    /// edited by this particular user is the room handler's answer, not a policy's — the policy
    /// only keeps everyone who edits nothing off the hub entirely.
    /// </summary>
    public const string CanCollaborate = nameof(CanCollaborate);

    public void RegisterPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(
            CanCollaborate,
            policy =>
            {
                policy
                    .RequireRole(nameof(UserRoles.Admin), nameof(UserRoles.Teacher))
                    .RequireNotBlocked();
            }
        );
    }
}
