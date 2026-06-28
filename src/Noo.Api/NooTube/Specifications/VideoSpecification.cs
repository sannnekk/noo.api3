using Ardalis.Specification;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.NooTube.Models;
using Noo.Api.NooTube.Types;

namespace Noo.Api.NooTube.Specifications;

public class VideoSpecification : Specification<NooTubeVideoModel>
{
    private static readonly IEnumerable<UserRoles> _canSeeUnlisted =
    [
        UserRoles.Admin,
        UserRoles.Teacher,
    ];

    public VideoSpecification(UserRoles role, Ulid userId, VideoFilterType type)
    {
        if (!_canSeeUnlisted.Contains(role))
        {
            Query.Where(video => video.IsListed);
        }

        if (type == VideoFilterType.Own)
        {
            Query.Where(video => video.UploadedById == userId);
        }

        if (type == VideoFilterType.Favourite)
        {
            Query.Where(video => video.Favourites.Any(favourite => favourite.UserId == userId));
        }

        Query.Include(v => v.UploadedBy);
        Query.Include(v => v.Thumbnail);
        Query.Include(v => v.Favourites.Where(favourite => favourite.UserId == userId));
    }
}
