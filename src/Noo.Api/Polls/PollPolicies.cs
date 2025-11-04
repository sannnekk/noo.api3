using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Polls;

public class PollPolicies : IPolicyRegistrar
{
    public const string CanCreatePoll = nameof(CanCreatePoll);
    public const string CanUpdatePoll = nameof(CanUpdatePoll);
    public const string CanDeletePoll = nameof(CanDeletePoll);
    public const string CanGetPolls = nameof(CanGetPolls);
    public const string CanGetPoll = nameof(CanGetPoll);
    public const string CanGetPollResults = nameof(CanGetPollResults);
    public const string CanGetPollResult = nameof(CanGetPollResult);
    public const string CanParticipateInPoll = nameof(CanParticipateInPoll);
    public const string CanUpdateAnswer = nameof(CanUpdateAnswer);

    public void RegisterPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(CanCreatePoll, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked();
        });

        options.AddPolicy(CanUpdatePoll, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked();
        });

        options.AddPolicy(CanDeletePoll, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked();
        });

        options.AddPolicy(CanGetPolls, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked();
        });

        options.AddPolicy(CanGetPoll, policy =>
        {
            policy.RequireAuthenticatedUser().RequireNotBlocked();
        });

        options.AddPolicy(CanGetPollResults, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked();
        });

        options.AddPolicy(CanGetPollResult, policy =>
        {
            policy
                .RequireAuthenticatedUser()
                .RequireNotBlocked()
                .AddRequirements(new AuthorizationRequirements.PollParticipationAccessRequirement());
        });

        options.AddPolicy(CanParticipateInPoll, policy =>
        {
            policy
                .RequireAuthenticatedUser()
                .RequireNotBlocked()
                .AddRequirements(new AuthorizationRequirements.PollParticipationCreationRequirement());
        });

        options.AddPolicy(CanUpdateAnswer, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked();
        });
    }
}
