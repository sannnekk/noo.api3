using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using SystemTextJsonPatch;

namespace Noo.Api.Core.Utils.Json;

public static class JsonPatchDocumentExtensions
{
    public static void ApplyToAndValidate<T>(
        this JsonPatchDocument<T> patch,
        T target,
        ModelStateDictionary? modelState = null
    ) where T : class
    {
        modelState ??= new ModelStateDictionary();

        patch.ApplyTo(target, (error) =>
        {
            // Ignore errors related to missing path segments (these occur when trying to patch nested properties that don't exist)
            // Unfortunately, SystemTextJsonPatch does not provide a specific error type for this case, so we have to rely on the error message content
            if (error.ErrorMessage.StartsWith("The target location specified by path segment"))
            {
                return;
            }

            modelState.AddModelError(error.Operation.ToString() ?? "Unknown operation", error.ErrorMessage);
        });

        // Validate DataAnnotations on the patched DTO and add errors to ModelState
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(target);
        Validator.TryValidateObject(target, context, validationResults, validateAllProperties: true);

        if (validationResults.Count > 0)
        {
            foreach (var result in validationResults)
            {
                foreach (var member in result.MemberNames?.Any() == true ? result.MemberNames : [string.Empty])
                {
                    modelState.AddModelError(member, result.ErrorMessage ?? "Validation error");
                }
            }

            // Add a generic error to guarantee IsValid == false in all cases
            modelState.AddModelError(string.Empty, "Validation failed");
        }
    }
}
