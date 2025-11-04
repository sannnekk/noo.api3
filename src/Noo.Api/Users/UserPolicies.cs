using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Users.AuthorizationRequirements;

namespace Noo.Api.Users;

public class UserPolicies : IPolicyRegistrar
{
    public const string CanGetUser = nameof(CanGetUser);
    public const string CanPatchUser = nameof(CanPatchUser);
    public const string CanSearchUsers = nameof(CanSearchUsers);
    public const string CanBlockUser = nameof(CanBlockUser);
    public const string CanChangeRole = nameof(CanChangeRole);
    public const string CanVerifyUser = nameof(CanVerifyUser);
    public const string CanAssignMentor = nameof(CanAssignMentor);
    public const string CanDeleteUser = nameof(CanDeleteUser);

    public void RegisterPolicies(AuthorizationOptions options)
    {
        // Every user can get their own data, mentors can get data of their students, privileged roles can get data of anybody
        options.AddPolicy(CanGetUser, policy =>
        {
            policy.RequireAuthenticatedUser()
                .AddRequirements(new UserAccessRequirement())
                .RequireNotBlocked();
        });

        // Every user can patch only own data
        options.AddPolicy(CanPatchUser, policy =>
        {
            policy.RequireAuthenticatedUser()
                .AddRequirements(new UserPatchRequirement())
                .RequireNotBlocked();
        });

        options.AddPolicy(CanSearchUsers, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher),
                nameof(UserRoles.Mentor),
                nameof(UserRoles.Assistant)
            ).RequireNotBlocked();
        });

        options.AddPolicy(CanBlockUser, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Teacher),
                nameof(UserRoles.Admin)
            ).RequireNotBlocked();
        });

        options.AddPolicy(CanChangeRole, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Teacher),
                nameof(UserRoles.Admin)
            ).RequireNotBlocked();
        });

        options.AddPolicy(CanVerifyUser, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Teacher),
                nameof(UserRoles.Admin)
            ).RequireNotBlocked();
        });

        // Teachers/Admin can assign any mentor. Mentors can assign only themselves.
        options.AddPolicy(CanAssignMentor, policy =>
        {
            policy.RequireAuthenticatedUser()
                .AddRequirements(new MentorAssignRequirement())
                .RequireNotBlocked();
        });

        // Every user can delete their own account, admin can delete anyone
        options.AddPolicy(CanDeleteUser, policy =>
        {
            policy.RequireAuthenticatedUser()
                .AddRequirements(new UserDeleteRequirement())
                .RequireNotBlocked();
        });
    }
}
