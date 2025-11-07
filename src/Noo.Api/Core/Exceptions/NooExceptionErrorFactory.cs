using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Response;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.Exceptions;

[RegisterSingleton(typeof(IClientErrorFactory))]
public class NooExceptionErrorFactory : IClientErrorFactory
{
    public IActionResult GetClientError(ActionContext actionContext, IClientErrorActionResult clientError)
    {
        var statusCode = clientError.StatusCode ?? StatusCodes.Status400BadRequest;
        var error = ResolveException(clientError, statusCode);
        var errorResponse = new ErrorApiResponseDTO(error.Serialize());

        return new ObjectResult(errorResponse)
        {
            StatusCode = (int)error.StatusCode
        };
    }

    private static NooException ResolveException(IClientErrorActionResult clientError, int statusCode)
    {
        if (clientError is ObjectResult { Value: NooException embedded })
        {
            return embedded;
        }

        var error = statusCode switch
        {
            StatusCodes.Status400BadRequest => new BadRequestException(),
            StatusCodes.Status401Unauthorized => new UnauthorizedException(),
            StatusCodes.Status403Forbidden => new ForbiddenException(),
            StatusCodes.Status404NotFound => new NotFoundException(),
            StatusCodes.Status409Conflict => new AlreadyExistsException(),
            StatusCodes.Status415UnsupportedMediaType => new UnsupportedMediaTypeException(),
            _ => CreateGenericClientError(clientError, statusCode)
        };

        if (clientError is ObjectResult { Value: ProblemDetails details })
        {
            if (details.Extensions.Count > 0)
            {
                error.Payload = details.Extensions;
            }
        }

        return error;
    }

    private static NooException CreateGenericClientError(IClientErrorActionResult clientError, int statusCode)
    {
        var hasHttpStatus = Enum.IsDefined(typeof(HttpStatusCode), statusCode);
        var httpStatusCode = hasHttpStatus
            ? (HttpStatusCode)statusCode
            : HttpStatusCode.BadRequest;

        var message = clientError switch
        {
            ObjectResult { Value: ProblemDetails details } => details.Detail ?? details.Title,
            _ => null
        } ?? "The request could not be processed.";

        return new NooException(httpStatusCode, message)
        {
            Id = $"HTTP_{statusCode}"
        };
    }
}
