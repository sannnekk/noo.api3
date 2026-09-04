using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.Core.System.Realtime.Filters;

/// <summary>
/// The hub counterpart of <see cref="PipelineExceptionHandler"/>. Hubs bypass the middleware
/// pipeline entirely, so without this a <see cref="NooException"/> reaches the client as a bare
/// "An unexpected error occurred" and the client cannot tell one failure from another.
/// </summary>
public class HubExceptionFilter : IHubFilter
{
    private readonly ILogger<HubExceptionFilter> _logger;

    public HubExceptionFilter(ILogger<HubExceptionFilter> logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next
    )
    {
        try
        {
            return await next(invocationContext);
        }
        catch (Exception exception) when (exception is not HubException)
        {
            var error = exception as NooException ?? NooException.FromUnhandled(exception);

            if (error.IsInternal)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception in {Hub}.{Method}. LogId: {LogId}",
                    invocationContext.Hub.GetType().Name,
                    invocationContext.HubMethodName,
                    error.LogId
                );
            }

            // Serialized the same way the HTTP error body is, so a client can reuse one parser.
            throw new HubException(JsonSerializer.Serialize(error.Serialize()));
        }
    }
}
