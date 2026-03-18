using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Noo.Api.Core.Request;

public class HyphenLowerCaseEnumModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

        var value = valueProviderResult.FirstValue;
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.CompletedTask;
        }

        var targetType = Nullable.GetUnderlyingType(bindingContext.ModelType) ?? bindingContext.ModelType;
        if (!targetType.IsEnum)
        {
            return Task.CompletedTask;
        }

        if (TryParseEnum(targetType, value, out var parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(parsed);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(
            bindingContext.ModelName,
            $"The value '{value}' is not valid for {targetType.Name}."
        );

        return Task.CompletedTask;
    }

    private static bool TryParseEnum(Type enumType, string rawValue, out object? parsed)
    {
        if (Enum.TryParse(enumType, rawValue, ignoreCase: true, out var standard))
        {
            parsed = standard;
            return true;
        }

        if (rawValue.Contains('-'))
        {
            var withoutHyphen = rawValue.Replace("-", string.Empty);
            if (Enum.TryParse(enumType, withoutHyphen, ignoreCase: true, out var hyphenParsed))
            {
                parsed = hyphenParsed;
                return true;
            }
        }

        parsed = null;
        return false;
    }
}
