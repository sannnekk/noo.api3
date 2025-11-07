using Noo.Api.Core.Response;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.Exceptions;

[RegisterScoped]
public class PipelineExceptionHandler : IMiddleware
{
    private readonly ILogger<PipelineExceptionHandler> _logger;

    public PipelineExceptionHandler(ILogger<PipelineExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            if (!await TryHandleExceptionAsync(context, exception))
            {
                throw;
            }
        }
    }

    internal async Task<bool> TryHandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning(exception, "Cannot handle exception because the response has already started.");
            return false;
        }

        var error = exception is NooException known
            ? known
            : NooException.FromUnhandled(exception);

        if (error.IsInternal)
        {
            _logger.LogError(exception, "Unhandled exception occurred. LogId: {LogId}", error.LogId);
        }
        else if (!ReferenceEquals(error, exception))
        {
            _logger.LogError(exception, "Unhandled exception converted to NooException. LogId: {LogId}", error.LogId);
        }

        context.Response.Clear();
        context.Response.StatusCode = (int)error.StatusCode;
        context.Response.ContentType = "application/json";

        var response = new ErrorApiResponseDTO(error.Serialize());
        await context.Response.WriteAsJsonAsync(response);
        return true;
    }
}
