using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Calendar;

public class CalendarPolicies : IPolicyRegistrar
{
    public const string CanGetCalendarEvents = nameof(CanGetCalendarEvents);
    public const string CanCreateCalendarEvent = nameof(CanCreateCalendarEvent);
    public const string CanDeleteCalendarEvent = nameof(CanDeleteCalendarEvent);

    public void RegisterPolicies(AuthorizationOptions options)
    {
        // TODO: Refine these policies as needed
        options.AddPolicy(CanGetCalendarEvents, policy =>
        {
            policy.RequireAuthenticatedUser().RequireNotBlocked();
        });

        options.AddPolicy(CanCreateCalendarEvent, policy =>
        {
            policy.RequireAuthenticatedUser().RequireNotBlocked();
        });

        options.AddPolicy(CanDeleteCalendarEvent, policy =>
        {
            policy.RequireAuthenticatedUser().RequireNotBlocked();
        });
    }
}
