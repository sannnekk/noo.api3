namespace Noo.Api.Core.Storage;

/// <summary>
/// Result of a presigned upload request.
/// The frontend must PUT the file to <see cref="Url"/> and include every
/// header in <see cref="Headers"/> exactly as supplied — otherwise the
/// signature does not match and S3 rejects the upload.
/// </summary>
public sealed record S3PresignedUpload(
    string Url,
    IReadOnlyDictionary<string, string> Headers,
    DateTime ExpiresAt);
