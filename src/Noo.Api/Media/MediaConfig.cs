namespace Noo.Api.Media;

public static class MediaConfig
{
    /// <summary>
    /// Maximum file size in bytes (150 MiB).
    /// </summary>
    public const long MaxFileSize = 150L * 1024 * 1024;

    /// <summary>
    /// Allowed MIME types for upload.
    /// </summary>
    public static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "image/svg+xml",
        "application/pdf",
    };

    private static readonly IReadOnlyDictionary<string, string> _contentTypesByExtension =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["jpg"] = "image/jpeg",
            ["jpeg"] = "image/jpeg",
            ["png"] = "image/png",
            ["gif"] = "image/gif",
            ["webp"] = "image/webp",
            ["svg"] = "image/svg+xml",
            ["pdf"] = "application/pdf",
        };

    /// <summary>
    /// The MIME type a stored file's extension stands for, or <c>null</c> when the
    /// extension is not one of the allowed ones. The content type the uploader
    /// declared is not kept on the media record, so callers that have to match a
    /// file against a MIME allow-list resolve it from the extension instead.
    /// </summary>
    public static string? ResolveContentType(string extension)
    {
        return _contentTypesByExtension.GetValueOrDefault(extension.TrimStart('.'));
    }
}
