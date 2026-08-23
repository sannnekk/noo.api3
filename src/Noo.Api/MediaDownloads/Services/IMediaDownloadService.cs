using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.MediaDownloads.DTO;
using Noo.Api.MediaDownloads.Filters;

namespace Noo.Api.MediaDownloads.Services;

public interface IMediaDownloadService
{
    /// <summary>
    /// Records one download. The row is persisted by the ambient unit of work.
    /// </summary>
    public void Record(Ulid mediaId, Ulid userId, Ulid? courseMaterialId);

    /// <summary>
    /// Download totals for every file attached to a material, files nobody downloaded included.
    /// </summary>
    public Task<IEnumerable<MaterialFileDownloadSummaryDTO>> GetMaterialSummaryAsync(
        Ulid materialId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Who downloaded the material's files, how often, and when they last did.
    /// </summary>
    public Task<SearchResult<MaterialFileDownloaderDTO>> GetMaterialDownloadersAsync(
        Ulid materialId,
        MaterialFileDownloadsFilter filter,
        CancellationToken cancellationToken = default
    );
}
