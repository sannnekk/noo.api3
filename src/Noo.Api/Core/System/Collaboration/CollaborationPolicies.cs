using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Core.System.Collaboration;

public class CollaborationPolicies : IPolicyRegistrar
{
    /// <summary>
    /// The gate on the hub and the endpoints, and deliberately no more than that: a signed-in
    /// user who is not blocked.
    ///
    /// Which roles may edit which document is the room handler's answer, because it differs per
    /// document type — a course editor will not have the same roster as a work editor — and a
    /// role list here would be Core knowing the union of every module's rules, which is exactly
    /// what registering a handler is supposed to avoid.
    ///
    /// So the hub is reachable by anyone signed in, as the ping and notification hubs already
    /// are, and useless to them: every method but the join needs a room, and the join is the
    /// handler's to refuse.
    /// </summary>
    public const string CanCollaborate = nameof(CanCollaborate);

    public void RegisterPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(
            CanCollaborate,
            policy =>
            {
                policy.RequireAuthenticatedUser().RequireNotBlocked();
            }
        );
    }
}
