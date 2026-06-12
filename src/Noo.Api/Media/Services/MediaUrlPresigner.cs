using Noo.Api.Core.Storage;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Media.DTO;
using Noo.Api.Media.Types;

namespace Noo.Api.Media.Services;

[RegisterScoped(typeof(IMediaUrlPresigner))]
public sealed class MediaUrlPresigner : IMediaUrlPresigner
{
    private readonly IS3Storage _s3;

    private readonly Dictionary<string, string> _cache = [];

    public MediaUrlPresigner(IS3Storage s3)
    {
        _s3 = s3;
    }

    public async Task SignAsync(IReadOnlyCollection<MediaDTO> media, CancellationToken cancellationToken = default)
    {
        if (media.Count == 0)
        {
            return;
        }

        var byPath = new Dictionary<string, List<MediaDTO>>();

        foreach (var m in media)
        {
            if (m.Status != MediaStatus.Completed || string.IsNullOrEmpty(m.Path))
            {
                continue;
            }

            if (!byPath.TryGetValue(m.Path, out var bucket))
            {
                byPath[m.Path] = bucket = new List<MediaDTO>(1);
            }

            bucket.Add(m);
        }

        var toSign = byPath.Keys.Where(path => !_cache.ContainsKey(path)).ToArray();

        if (toSign.Length > 0)
        {
            var signed = await Task.WhenAll(toSign.Select(async path =>
            {
                var url = await _s3.CreatePresignedDownloadAsync(path, cancellationToken: cancellationToken);
                return (path, url);
            }));

            foreach (var (path, url) in signed)
            {
                _cache[path] = url;
            }
        }

        foreach (var (path, bucket) in byPath)
        {
            if (!_cache.TryGetValue(path, out var url))
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
