using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.Exceptions.Http;

namespace Noo.Api.Core.Exceptions;

public static class ExceptionHelpers
{
    public static void ThrowNotFoundIfNull([NotNull] this BaseModel? obj)
    {
        if (obj == null)
        {
            throw new NotFoundException();
        }
    }

    public static void ThrowValidationExceptionIfInvalid(this ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => error.Exception?.Message ?? error.ErrorMessage).ToArray()
            );

        if (errors.Count > 0)
        {
            throw new BadRequestException()
            {
                Payload = errors
            };
        }
    }
}
