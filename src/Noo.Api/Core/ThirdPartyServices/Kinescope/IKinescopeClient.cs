using Noo.Api.Core.ThirdPartyServices.Kinescope.Models;

namespace Noo.Api.Core.ThirdPartyServices.Kinescope;

/// <summary>
/// Thin wrapper around the Kinescope HTTP API (https://api.kinescope.io).
/// Exposes the operations the platform currently needs and is meant to grow
/// as more Kinescope features (folders, projects, analytics, ...) are adopted.
/// </summary>
public interface IKinescopeClient
{
    /// <summary>
    /// Initializes a resumable (tus) upload and returns the upload endpoint the
    /// client should stream the file to, together with the created video id.
    /// </summary>
    public Task<CreateUploadResult> CreateUploadAsync(
        CreateUploadRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Fetches a single video by its Kinescope id, or <c>null</c> if it does not exist.
    /// </summary>
    public Task<KinescopeVideo?> GetVideoAsync(
        string videoId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Permanently deletes a video on Kinescope.
    /// </summary>
    public Task DeleteVideoAsync(string videoId, CancellationToken cancellationToken = default);
}
