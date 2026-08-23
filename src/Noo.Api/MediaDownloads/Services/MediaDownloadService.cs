using AutoMapper;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Courses.Services;
using Noo.Api.Media.DTO;
using Noo.Api.MediaDownloads.DTO;
using Noo.Api.MediaDownloads.Filters;
using Noo.Api.MediaDownloads.Models;
using Noo.Api.Users.DTO;
using Noo.Api.Users.Services;

namespace Noo.Api.MediaDownloads.Services;

[RegisterScoped(typeof(IMediaDownloadService))]
public class MediaDownloadService : IMediaDownloadService
{
    private readonly IMediaDownloadRepository _downloads;
    private readonly ICourseContentRepository _contents;
    private readonly IUserRepository _users;
    private readonly IMapper _mapper;

    public MediaDownloadService(
        IMediaDownloadRepository downloads,
        ICourseContentRepository contents,
        IUserRepository users,
        IMapper mapper
    )
    {
        _downloads = downloads;
        _contents = contents;
        _users = users;
        _mapper = mapper;
    }

    public void Record(Ulid mediaId, Ulid userId, Ulid? courseMaterialId)
    {
        _downloads.Add(
            new MediaDownloadModel
            {
                MediaId = mediaId,
                UserId = userId,
                CourseMaterialId = courseMaterialId,
            }
        );
    }

    public async Task<IEnumerable<MaterialFileDownloadSummaryDTO>> GetMaterialSummaryAsync(
        Ulid materialId,
        CancellationToken cancellationToken = default
    )
    {
        var media =
            await _contents.GetMaterialMediaAsync(materialId)
            ?? throw new NotFoundException("Material not found");

        var counts = await _downloads.GetCountsByMediaAsync(
            [.. media.Select(m => m.Id)],
            cancellationToken
        );

        var countsById = counts.ToDictionary(c => c.MediaId);

        // Stitched here rather than joined so a file nobody has downloaded still shows up, at zero.
        return media.Select(m =>
        {
            countsById.TryGetValue(m.Id, out var count);

            return new MaterialFileDownloadSummaryDTO
            {
                Media = _mapper.Map<MediaDTO>(m),
                TotalDownloads = count?.TotalDownloads ?? 0,
                UniqueUsers = count?.UniqueUsers ?? 0,
                LastDownloadAt = count?.LastDownloadAt,
            };
        });
    }

    public async Task<SearchResult<MaterialFileDownloaderDTO>> GetMaterialDownloadersAsync(
        Ulid materialId,
        MaterialFileDownloadsFilter filter,
        CancellationToken cancellationToken = default
    )
    {
        var rows = await _downloads.GetDownloadersAsync(
            materialId,
            filter.MediaId,
            filter.Page,
            filter.PerPage,
            cancellationToken
        );

        // The users of this page only, in one query — joining them into the grouping would
        // multiply the rows the aggregate has to scan.
        var users = await _users.GetManyWithAvatarAsync([.. rows.Items.Select(r => r.UserId)]);
        var usersById = users.ToDictionary(u => u.Id);

        var items = rows.Items.Select(r => new MaterialFileDownloaderDTO
        {
            UserId = r.UserId,
            User = usersById.TryGetValue(r.UserId, out var user)
                ? _mapper.Map<UserDTO>(user)
                : null,
            DownloadCount = r.DownloadCount,
            FirstDownloadAt = r.FirstDownloadAt,
            LastDownloadAt = r.LastDownloadAt,
        });

        return new SearchResult<MaterialFileDownloaderDTO>(items, rows.Total);
    }
}
