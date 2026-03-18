using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Noo.Api.Core.Request;

public class HyphenLowerCaseEnumModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var targetType = context.Metadata.UnderlyingOrModelType;
        if (!targetType.IsEnum)
        {
            return null;
        }

        return new HyphenLowerCaseEnumModelBinder();
    }
}
