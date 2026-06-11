using System.Linq.Expressions;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace Noo.Api.Core.Request.Patching;

public static class JsonPatchExtensions
{
    public static Operation<T>? GetOperation<T, TProp>(
        this JsonPatchDocument<T> patchDocument,
        Expression<Func<T, TProp>> prop
    )
        where T : class
    {
        var propertyName = GetPropertyName(prop);

        return patchDocument.Operations.FirstOrDefault(op =>
            string.Equals(op.Path?.Trim('/'), propertyName, StringComparison.OrdinalIgnoreCase)
        );
    }

    public static TProp? GetValue<T, TProp>(
        this JsonPatchDocument<T> patchDocument,
        Expression<Func<T, TProp>> prop
    )
        where T : class
    {
        return ConvertValue<TProp>(GetOperation(patchDocument, prop)?.Value);
    }

    private static TValue? ConvertValue<TValue>(object? value)
    {
        if (value is null)
        {
            return default;
        }

        if (value is TValue typed)
        {
            return typed;
        }

        var targetType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

        if (targetType.IsEnum)
        {
            return (TValue)Enum.Parse(targetType, value.ToString()!, ignoreCase: true);
        }

        return (TValue)Convert.ChangeType(value, targetType);
    }

    public static bool ContainsOperation<T, TProp>(
        this JsonPatchDocument<T> patchDocument,
        Expression<Func<T, TProp>> prop
    )
        where T : class
    {
        var propertyName = GetPropertyName(prop);

        return patchDocument.Operations.Any(op =>
            string.Equals(op.Path?.Trim('/'), propertyName, StringComparison.OrdinalIgnoreCase)
        );
    }

    public static (string? Op, object? Value) RemoveOperation<T, TProp>(
        this JsonPatchDocument<T> patchDocument,
        Expression<Func<T, TProp>> prop
    )
        where T : class
    {
        var propertyName = GetPropertyName(prop);

        var operation = patchDocument.Operations.FirstOrDefault(op =>
            string.Equals(op.Path?.Trim('/'), propertyName, StringComparison.OrdinalIgnoreCase)
        );

        if (operation is null)
        {
            return (null, null);
        }

        patchDocument.Operations.Remove(operation);

        return (operation.Op, operation.Value);
    }

    private static string GetPropertyName<T, TProp>(Expression<Func<T, TProp>> prop)
    {
        if (prop.Body is not MemberExpression memberExpression)
        {
            throw new ArgumentException("Expression must be a member expression", nameof(prop));
        }

        return memberExpression.Member.Name;
    }
}
