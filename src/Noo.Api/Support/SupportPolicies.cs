using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Support;

public class SupportPolicies : IPolicyRegistrar
{
    public const string CanCreateArticle = nameof(CanCreateArticle);
    public const string CanUpdateArticle = nameof(CanUpdateArticle);
    public const string CanDeleteArticle = nameof(CanDeleteArticle);
    public const string CanCreateCategory = nameof(CanCreateCategory);
    public const string CanUpdateCategory = nameof(CanUpdateCategory);
    public const string CanDeleteCategory = nameof(CanDeleteCategory);
    public const string CanCreateFaqItem = nameof(CanCreateFaqItem);
    public const string CanUpdateFaqItem = nameof(CanUpdateFaqItem);
    public const string CanDeleteFaqItem = nameof(CanDeleteFaqItem);

    public void RegisterPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(CanCreateArticle, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked();
        });

        options.AddPolicy(CanUpdateArticle, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked();
        });

        options.AddPolicy(CanDeleteArticle, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked();
        });

        options.AddPolicy(CanCreateCategory, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked();
        });

        options.AddPolicy(CanUpdateCategory, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked();
        });
        options.AddPolicy(CanDeleteCategory, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked();
        });

        options.AddPolicy(CanCreateFaqItem, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked();
        });

        options.AddPolicy(CanUpdateFaqItem, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked();
        });

        options.AddPolicy(CanDeleteFaqItem, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked();
        });
    }
}
