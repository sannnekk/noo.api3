using Noo.Api.Core.Storage;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Media.Models;
using Noo.Api.Media.Types;

namespace Noo.Api.Media.Services;

[RegisterScoped(typeof(IMediaUrlEnricher))]
public sealed class MediaUrlEnricher : IMediaUrlEnricher
{
    private readonly IS3Storage _s3;

    // Per-request memo: signing the same key twice in one request is wasted work,
    // and the same media often appears many times in a list response.
    private readonly Dictionary<Ulid, string> _cache = [];

    public MediaUrlEnricher(IS3Storage s3)
    {
        _s3 = s3;
    }

    public Task EnrichAsync(MediaModel? media, CancellationToken cancellationToken = default)
    {
        if (media is null)
        {
            return Task.CompletedTask;
        }

        return EnrichManyAsync([media], cancellationToken);
    }

    public Task EnrichAsync(IEnumerable<MediaModel?>? media, CancellationToken cancellationToken = default)
    {
        if (media is null)
        {
            return Task.CompletedTask;
        }

        return EnrichManyAsync(media, cancellationToken);
    }

    public Task EnrichAsync<T>(T? entity, CancellationToken cancellationToken = default)
        where T : class, IHasPresignedMedia
    {
        if (entity is null)
        {
            return Task.CompletedTask;
        }

        return EnrichManyAsync(entity.GetMediaForPresigning(), cancellationToken);
    }

    public Task EnrichAsync<T>(IEnumerable<T>? entities, CancellationToken cancellationToken = default)
        where T : class, IHasPresignedMedia
    {
        if (entities is null)
        {
            return Task.CompletedTask;
        }

        return EnrichManyAsync(
            entities.SelectMany(e => e?.GetMediaForPresigning() ?? []),
            cancellationToken);
    }

    private async Task EnrichManyAsync(IEnumerable<MediaModel?> media, CancellationToken cancellationToken)
    {
        // Group by id so each unique media is signed once but every duplicate
        // reference (e.g. the same thumbnail across many courses) gets the URL.
        var grouped = new Dictionary<Ulid, List<MediaModel>>();

        foreach (var m in media)
        {
            if (m is null || m.Status != MediaStatus.Completed || string.IsNullOrEmpty(m.Path))
            {
                continue;
            }

            if (!grouped.TryGetValue(m.Id, out var bucket))
            {
                grouped[m.Id] = bucket = new List<MediaModel>(1);
            }

            bucket.Add(m);
        }

        if (grouped.Count == 0)
        {
            return;
        }

        var toSign = grouped
            .Where(kv => !_cache.ContainsKey(kv.Key))
            .Select(kv => kv.Value[0])
            .ToArray();

        if (toSign.Length > 0)
        {
            // Presigning is a local sigv4 computation, not a network call, but
            // running them concurrently still amortizes any per-call overhead.
            var signed = await Task.WhenAll(toSign.Select(async m =>
            {
                var url = await _s3.CreatePresignedDownloadAsync(m.Path, cancellationToken: cancellationToken);
                return (m.Id, url);
            }));

            foreach (var (id, url) in signed)
            {
                _cache[id] = url;
            }
        }

        foreach (var (id, bucket) in grouped)
        {
            if (!_cache.TryGetValue(id, out var url))
            {
                continue;
            }

            foreach (var m in bucket)
            {
                m.Url = url;
            }
        }
    }
}
