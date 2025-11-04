using Noo.Api.Core.Security.Authorization;
using Noo.Api.Sessions.Services;

namespace Noo.Api.Sessions.Middleware;

public class SessionActivityMiddleware
{
    private readonly RequestDelegate _next;

    public SessionActivityMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IOnlineService onlineService, IActiveUserService activeUserService)
    {
        await _next(context);

        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var sessionId = context.User.GetSessionId();
            var userId = context.User.GetId();
            var role = context.User.GetRole();

            if (role == null)
            {
                return;
            }

            await onlineService.SetSessionOnlineAsync(sessionId);
            await onlineService.SetUserOnlineAsync(userId, role.Value);
            await activeUserService.SetUserActiveAsync(userId, role.Value);
        }
    }
}
