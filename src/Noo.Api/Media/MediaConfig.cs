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
}
