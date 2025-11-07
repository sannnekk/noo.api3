using Noo.Api.Core.Exceptions;

namespace Noo.Api.Core.Initialization.App;

public static class ExceptionHandlingExtension
{
    public static void UseExceptionHandling(this WebApplication app)
    {
        app.UseMiddleware<PipelineExceptionHandler>();
    }
}
