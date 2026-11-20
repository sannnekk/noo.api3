using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Notifications;

public class NotificationPolicies : IPolicyRegistrar
{
    public const string CanViewNotifications = nameof(CanViewNotifications);
    public const string CanReadNotifications = nameof(CanReadNotifications);
    public const string CanBulkCreateNotifications = nameof(CanBulkCreateNotifications);
    public const string CanDeleteNotifications = nameof(CanDeleteNotifications);
    public const string CanManagePushSubscriptions = nameof(CanManagePushSubscriptions);

    public void RegisterPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(CanViewNotifications, policy =>
        {
            policy.RequireAuthenticatedUser().RequireNotBlocked();
        });

        options.AddPolicy(CanReadNotifications, policy =>
        {
            policy.RequireAuthenticatedUser().RequireNotBlocked();
        });

        options.AddPolicy(CanBulkCreateNotifications, policy =>
        {
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked();
        });

        options.AddPolicy(CanDeleteNotifications, policy =>
        {
            policy.RequireAuthenticatedUser().RequireNotBlocked();
        });

        options.AddPolicy(CanManagePushSubscriptions, policy =>
        {
            policy.RequireAuthenticatedUser().RequireNotBlocked();
        });
    }
}
