using SlugGenerator;

namespace Noo.Api.Core.Utils;

public static class Slug
{
    public static string Generate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return input.GenerateSlug();
    }
}
