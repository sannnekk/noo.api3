using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.SavedTasks;

public class SavedTaskPolicies : IPolicyRegistrar
{
    public const string CanSaveTask = nameof(CanSaveTask);
    public const string CanGetSavedTasks = nameof(CanGetSavedTasks);
    public const string CanRemoveSavedTask = nameof(CanRemoveSavedTask);

    public void RegisterPolicies(AuthorizationOptions options)
    {
        // Saved tasks are a student's own revision pile, so the whole module is
        // closed to every other role.
        options.AddPolicy(
            CanSaveTask,
            policy =>
            {
                policy.RequireRole(nameof(UserRoles.Student)).RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanGetSavedTasks,
            policy =>
            {
                policy.RequireRole(nameof(UserRoles.Student)).RequireNotBlocked();
            }
        );

        options.AddPolicy(
            CanRemoveSavedTask,
            policy =>
            {
                policy.RequireRole(nameof(UserRoles.Student)).RequireNotBlocked();
            }
        );
    }
}
