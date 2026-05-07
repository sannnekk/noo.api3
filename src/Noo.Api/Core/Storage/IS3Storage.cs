namespace Noo.Api.Core.Storage;

/// <summary>
/// Thin wrapper around the S3 SDK that exposes only the operations the app needs:
/// presigned upload/download URLs, deletion, existence, and head metadata.
/// All keys are bucket-relative.
/// </summary>
public interface IS3Storage
{
    public Task<S3PresignedUpload> CreatePresignedUploadAsync(
        string key,
        string contentType,
        IReadOnlyDictionary<string, string>? tags = null,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    public Task<string> CreatePresignedDownloadAsync(
        string key,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    public Task<S3ObjectMetadata?> GetMetadataAsync(string key, CancellationToken cancellationToken = default);
}
