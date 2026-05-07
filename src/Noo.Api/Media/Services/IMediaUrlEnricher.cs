using Noo.Api.Media.Models;

namespace Noo.Api.Media.Services;

/// <summary>
/// Fills <see cref="MediaModel.Url"/> with a presigned download URL on every
/// completed media reachable from the supplied entity (or entities). Calls dedupe
/// by media id and reuse already-signed URLs across the request scope, so
/// repeatedly enriching the same media is free.
/// </summary>
public interface IMediaUrlEnricher
{
    public Task EnrichAsync(MediaModel? media, CancellationToken cancellationToken = default);

    public Task EnrichAsync(IEnumerable<MediaModel?>? media, CancellationToken cancellationToken = default);

    public Task EnrichAsync<T>(T? entity, CancellationToken cancellationToken = default)
        where T : class, IHasPresignedMedia;

    public Task EnrichAsync<T>(IEnumerable<T>? entities, CancellationToken cancellationToken = default)
        where T : class, IHasPresignedMedia;
}
