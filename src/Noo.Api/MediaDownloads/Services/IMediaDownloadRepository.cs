using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.MediaDownloads.Models;
using Noo.Api.MediaDownloads.Types;

namespace Noo.Api.MediaDownloads.Services;

public interface IMediaDownloadRepository : IRepository<MediaDownloadModel>
{
    /// <summary>
    /// Download totals per file, for the given files only. Files nobody downloaded are absent.
    /// </summary>
    public Task<IReadOnlyList<MediaDownloadCounts>> GetCountsByMediaAsync(
        IReadOnlyCollection<Ulid> mediaIds,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// One page of the per-user breakdown for a material, most recent downloader first.
    /// </summary>
    public Task<SearchResult<MediaDownloaderCounts>> GetDownloadersAsync(
        Ulid materialId,
        Ulid? mediaId,
        int page,
        int perPage,
        CancellationToken cancellationToken = default
    );
}
