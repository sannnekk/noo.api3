using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Noo.Api.Core.Utils.Richtext;
using SystemTextJsonPatch;

namespace Noo.Api.Core.Utils.Json;

public static class JsonPatchDocumentExtensions
{
    // Deep enough for the course chapter tree (CourseConfig.MaxChapterTreeDepth) plus the
    // DTO/collection wrapper hops, with headroom; the visited set guards against cycles so
    // this is only a backstop against pathological graphs.
    private const int _maxValidationDepth = 32;

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

        // Validate DataAnnotations across the whole patched DTO graph and add errors to
        // ModelState. Validator.TryValidateObject only inspects an object's own properties,
        // so nested update DTOs reached through dictionaries/collections (work tasks, course
        // chapters, work-assignments, ...) would otherwise bypass their own annotations on PATCH.
        var validationResults = new List<ValidationResult>();
        ValidateGraph(target, validationResults, new HashSet<object>(ReferenceEqualityComparer.Instance), depth: 0);

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

    private static void ValidateGraph(
        object? value,
        List<ValidationResult> results,
        HashSet<object> visited,
        int depth
    )
    {
        if (value is null || depth > _maxValidationDepth || value is string || value is IRichTextType)
        {
            return;
        }

        if (value is IDictionary dictionary)
        {
            foreach (var item in dictionary.Values)
            {
                ValidateGraph(item, results, visited, depth + 1);
            }

            return;
        }

        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                ValidateGraph(item, results, visited, depth + 1);
            }

            return;
        }

        // Only recurse into our own DTO graph; primitives, Ulid, DateTime, enums and other
        // framework types are validated in place by their owner's TryValidateObject pass.
        var type = value.GetType();
        if (type.Namespace?.StartsWith("Noo.Api", StringComparison.Ordinal) != true || !visited.Add(value))
        {
            return;
        }

        Validator.TryValidateObject(value, new ValidationContext(value), results, validateAllProperties: true);

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length != 0)
            {
                continue;
            }

            object? child;
            try
            {
                child = property.GetValue(value);
            }
            catch
            {
                continue;
            }

            ValidateGraph(child, results, visited, depth + 1);
        }
    }
}
