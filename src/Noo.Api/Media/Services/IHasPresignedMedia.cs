using Noo.Api.Media.Models;

namespace Noo.Api.Media.Services;

/// <summary>
/// Marker for an entity that exposes one or more <see cref="MediaModel"/> instances
/// whose <see cref="MediaModel.Url"/> should be filled with a presigned download URL
/// before being mapped to a DTO. Single, nullable, and collection-shaped relations
/// are all flattened into the returned sequence; nulls are tolerated.
/// </summary>
public interface IHasPresignedMedia
{
    public IEnumerable<MediaModel?> GetMediaForPresigning();
}
