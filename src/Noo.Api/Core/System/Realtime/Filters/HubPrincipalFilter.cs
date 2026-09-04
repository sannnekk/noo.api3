using Microsoft.AspNetCore.SignalR;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Core.System.Realtime.Filters;

/// <summary>
/// Supplies the caller to <see cref="ICurrentUser"/> for the duration of a hub invocation.
/// SignalR never flows an <see cref="HttpContext"/> into hub methods, so without this every
/// service injected into a hub would report an anonymous user — silently, with no error.
/// </summary>
public class HubPrincipalFilter : IHubFilter
{
    public ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next
    )
    {
        SetPrincipal(invocationContext.ServiceProvider, invocationContext.Context);

        return next(invocationContext);
    }

    public Task OnConnectedAsync(
        HubLifetimeContext context,
        Func<HubLifetimeContext, Task> next
    )
    {
        SetPrincipal(context.ServiceProvider, context.Context);

        return next(context);
    }

    public Task OnDisconnectedAsync(
        HubLifetimeContext context,
        Exception? exception,
        Func<HubLifetimeContext, Exception?, Task> next
    )
    {
        SetPrincipal(context.ServiceProvider, context.Context);

        return next(context, exception);
    }

    private static void SetPrincipal(IServiceProvider services, HubCallerContext context)
    {
        services.GetRequiredService<ClaimsPrincipalAccessor>().Principal = context.User;
    }
}
