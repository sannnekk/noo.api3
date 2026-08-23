using Noo.Api.Core.System.Events;
using Noo.Api.Courses.Services;
using Noo.Api.Media.Events;
using Noo.Api.Media.Types;
using Noo.Api.MediaDownloads.Services;

namespace Noo.Api.MediaDownloads.Events;

/// <summary>
/// Turns a download of a course attachment into a statistics row.
/// </summary>
public sealed class MediaDownloadedHandler : IEventHandler<MediaDownloadedEvent>
{
    private readonly IMediaDownloadService _downloads;
    private readonly ICourseContentRepository _contents;

    public MediaDownloadedHandler(
        IMediaDownloadService downloads,
        ICourseContentRepository contents
    )
    {
        _downloads = downloads;
        _contents = contents;
    }

    public async Task HandleAsync(MediaDownloadedEvent @event, CancellationToken ct = default)
    {
        // Only attachments are counted. Inline richtext images resolve their URL through the same
        // endpoint on every render, which says nothing about anyone taking the file.
        if (@event.Category != MediaCategory.CourseAttachment)
        {
            return;
        }

        var materialId = await _contents.GetMaterialIdByMediaIdAsync(@event.MediaId);

        _downloads.Record(@event.MediaId, @event.UserId, materialId);
    }
}
