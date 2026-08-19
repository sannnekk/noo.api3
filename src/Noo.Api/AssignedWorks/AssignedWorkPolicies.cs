using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.AssignedWorks;

public class AssignedWorkPolicies : IPolicyRegistrar
{
    public const string CanCreateAssignedWork = nameof(CanCreateAssignedWork);
    public const string CanGetAssignedWorks = nameof(CanGetAssignedWorks);
    public const string CanGetAssignedWork = nameof(CanGetAssignedWork);
    public const string CanGetAssignedWorkProgress = nameof(CanGetAssignedWorkProgress);
    public const string CanRemakeAssignedWork = nameof(CanRemakeAssignedWork);
    public const string CanEditAssignedWork = nameof(CanEditAssignedWork);
    public const string CanCommentAssignedWork = nameof(CanCommentAssignedWork);
    public const string CanSolveAssignedWork = nameof(CanSolveAssignedWork);
    public const string CanCheckAssignedWork = nameof(CanCheckAssignedWork);
    public const string CanArchiveAssignedWork = nameof(CanArchiveAssignedWork);
    public const string CanUnarchiveAssignedWork = nameof(CanUnarchiveAssignedWork);
    public const string CanAddHelperMentorToAssignedWork = nameof(CanAddHelperMentorToAssignedWork);
    public const string CanReplaceMainMentorOfAssignedWork = nameof(
        CanReplaceMainMentorOfAssignedWork
    );
    public const string CanShiftDeadlineOfAssignedWork = nameof(CanShiftDeadlineOfAssignedWork);
    public const string CanReturnAssignedWorkToSolve = nameof(CanReturnAssignedWorkToSolve);
    public const string CanReturnAssignedWorkToCheck = nameof(CanReturnAssignedWorkToCheck);
    public const string CanDeleteAssignedWork = nameof(CanDeleteAssignedWork);
    public const string CanRevealTaskAnswer = nameof(CanRevealTaskAnswer);
    public const string CanCheckOwnTask = nameof(CanCheckOwnTask);

    public void RegisterPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(
            CanCreateAssignedWork,
            policy =>
            {
                policy.RequireRole(nameof(UserRoles.Student)).RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanGetAssignedWorks,
            policy =>
            {
                policy.RequireAuthenticatedUser().RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanGetAssignedWorkProgress,
            policy =>
            {
                policy.RequireRole(nameof(UserRoles.Student)).RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanGetAssignedWork,
            policy =>
            {
                policy.RequireAuthenticatedUser().RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanRemakeAssignedWork,
            policy =>
            {
                policy.RequireRole(nameof(UserRoles.Student)).RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanEditAssignedWork,
            policy =>
            {
                policy
                    .RequireRole([nameof(UserRoles.Student), nameof(UserRoles.Mentor)])
                    .RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanCommentAssignedWork,
            policy =>
            {
                policy
                    .RequireRole([nameof(UserRoles.Student), nameof(UserRoles.Mentor)])
                    .RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanSolveAssignedWork,
            policy =>
            {
                policy.RequireRole(nameof(UserRoles.Student)).RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanCheckAssignedWork,
            policy =>
            {
                policy.RequireRole(nameof(UserRoles.Mentor)).RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanArchiveAssignedWork,
            policy =>
            {
                policy
                    .RequireRole(
                        [
                            nameof(UserRoles.Student),
                            nameof(UserRoles.Mentor),
                            nameof(UserRoles.Assistant),
                            nameof(UserRoles.Teacher),
                            nameof(UserRoles.Admin),
                        ]
                    )
                    .RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanUnarchiveAssignedWork,
            policy =>
            {
                policy
                    .RequireRole(
                        [
                            nameof(UserRoles.Student),
                            nameof(UserRoles.Mentor),
                            nameof(UserRoles.Assistant),
                            nameof(UserRoles.Teacher),
                            nameof(UserRoles.Admin),
                        ]
                    )
                    .RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanAddHelperMentorToAssignedWork,
            policy =>
            {
                policy
                    .RequireRole(
                        [
                            nameof(UserRoles.Mentor),
                            nameof(UserRoles.Teacher),
                            nameof(UserRoles.Admin),
                        ]
                    )
                    .RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanReplaceMainMentorOfAssignedWork,
            policy =>
            {
                policy
                    .RequireRole(
                        [
                            nameof(UserRoles.Assistant),
                            nameof(UserRoles.Teacher),
                            nameof(UserRoles.Admin),
                        ]
                    )
                    .RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanShiftDeadlineOfAssignedWork,
            policy =>
            {
                policy
                    .RequireRole([nameof(UserRoles.Student), nameof(UserRoles.Mentor)])
                    .RequireNotBlocked();
            }
        );

        // Both "send back" actions are the checking mentor's call, and the service looks
        // the work up as one of the caller's own, so only a mentor can reach them.
        options.AddPolicy(
            CanReturnAssignedWorkToSolve,
            policy =>
            {
                policy.RequireRole(nameof(UserRoles.Mentor)).RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanReturnAssignedWorkToCheck,
            policy =>
            {
                policy.RequireRole(nameof(UserRoles.Mentor)).RequireNotBlocked();
            }
        );

        // Both are the student's own doing while they solve; the service narrows it
        // further to the work actually being theirs.
        options.AddPolicy(
            CanRevealTaskAnswer,
            policy =>
            {
                policy.RequireRole(nameof(UserRoles.Student)).RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanCheckOwnTask,
            policy =>
            {
                policy.RequireRole(nameof(UserRoles.Student)).RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanDeleteAssignedWork,
            policy =>
            {
                policy
                    .RequireRole([nameof(UserRoles.Student), nameof(UserRoles.Admin)])
                    .RequireNotBlocked();
            }
        );
    }
}
