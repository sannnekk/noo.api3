namespace Noo.Api.Media.Types;

public static class MediaCategoryExtensions
{
    /// <summary>
    /// Returns the kebab-case slug used in S3 keys and tag values
    /// (e.g. <c>UserAvatar</c> → <c>user-avatar</c>).
    /// </summary>
    public static string ToSlug(this MediaCategory category)
    {
        var name = category.ToString();
        var sb = new System.Text.StringBuilder(name.Length + 8);

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c))
            {
                sb.Append('-');
            }
            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }
}
