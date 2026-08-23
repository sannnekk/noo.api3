using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.MediaDownloads.Models;
using Noo.Api.MediaDownloads.Types;

namespace Noo.Api.MediaDownloads.Services;

[RegisterScoped(typeof(IMediaDownloadRepository))]
public class MediaDownloadRepository : Repository<MediaDownloadModel>, IMediaDownloadRepository
{
    public MediaDownloadRepository(NooDbContext dbContext)
        : base(dbContext) { }

    public async Task<IReadOnlyList<MediaDownloadCounts>> GetCountsByMediaAsync(
        IReadOnlyCollection<Ulid> mediaIds,
        CancellationToken cancellationToken = default
    )
    {
        if (mediaIds.Count == 0)
        {
            return [];
        }

        return await CountsByMediaQuery(mediaIds).ToListAsync(cancellationToken);
    }

    public async Task<SearchResult<MediaDownloaderCounts>> GetDownloadersAsync(
        Ulid materialId,
        Ulid? mediaId,
        int page,
        int perPage,
        CancellationToken cancellationToken = default
    )
    {
        var total = await DownloadersQuery(materialId, mediaId).CountAsync(cancellationToken);

        var items = await DownloadersPageQuery(materialId, mediaId, page, perPage)
            .ToListAsync(cancellationToken);

        return new SearchResult<MediaDownloaderCounts>(items, total);
    }

    // The aggregate queries are shaped separately from their materialization so a test can compile
    // them to SQL without a database — the InMemory provider the other tests run on evaluates
    // anything on the client and so cannot tell a translatable grouping from an untranslatable one.

    internal IQueryable<MediaDownloadCounts> CountsByMediaQuery(IReadOnlyCollection<Ulid> mediaIds)
    {
        return Context.GetDbSet<MediaDownloadModel>()
            .Where(d => mediaIds.Contains(d.MediaId))
            .GroupBy(d => d.MediaId)
            .Select(g => new MediaDownloadCounts
            {
                MediaId = g.Key,
                TotalDownloads = g.Count(),
                UniqueUsers = g.Select(d => d.UserId).Distinct().Count(),
                LastDownloadAt = g.Max(d => d.CreatedAt),
            });
    }

    internal IQueryable<MediaDownloaderCounts> DownloadersQuery(Ulid materialId, Ulid? mediaId)
    {
        var query = Context.GetDbSet<MediaDownloadModel>()
            .Where(d => d.CourseMaterialId == materialId);

        if (mediaId is { } id)
        {
            query = query.Where(d => d.MediaId == id);
        }

        return query
            .GroupBy(d => d.UserId)
            .Select(g => new MediaDownloaderCounts
            {
                UserId = g.Key,
                DownloadCount = g.Count(),
                FirstDownloadAt = g.Min(d => d.CreatedAt),
                LastDownloadAt = g.Max(d => d.CreatedAt),
            });
    }

    internal IQueryable<MediaDownloaderCounts> DownloadersPageQuery(
        Ulid materialId,
        Ulid? mediaId,
        int page,
        int perPage
    )
    {
        return DownloadersQuery(materialId, mediaId)
            .OrderByDescending(r => r.LastDownloadAt)
            .Skip(Math.Max(page - 1, 0) * perPage)
            .Take(perPage);
    }
}
