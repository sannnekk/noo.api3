using Noo.Api.Core.Storage;
using Noo.Api.Media.Models;
using Noo.Api.Media.Types;

namespace Noo.Api.Media.Services;

public interface IMediaService
{
    /// <summary>
    /// Issues a presigned PUT URL for the current user. The frontend must PUT the
    /// file to <see cref="UploadTicket.Upload"/>.Url and include all returned
    /// headers verbatim, then call <see cref="CompleteUploadAsync"/>.
    /// </summary>
    public Task<UploadTicket> RequestUploadAsync(
        MediaCategory category,
        string fileName,
        string contentType,
        Ulid? entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms an upload finished and persists the final size + ETag.
    /// Returns the media entity with <see cref="MediaModel.Url"/> populated with a
    /// presigned GET URL for the freshly uploaded object. Owner-only (admins also).
    /// </summary>
    public Task<MediaModel> CompleteUploadAsync(
        Ulid mediaId,
        long size,
        string? etag = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a presigned GET URL after running every access rule registered for
    /// the media's category.
    /// </summary>
    public Task<string> GetDownloadUrlAsync(Ulid mediaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the object from S3 and the DB record. Owner-only (admins also).
    /// </summary>
    public Task DeleteAsync(Ulid mediaId, CancellationToken cancellationToken = default);
}

public sealed record UploadTicket(Ulid MediaId, S3PresignedUpload Upload);
