using AutoMapper;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.NooTube.DTO;

namespace Noo.Api.NooTube.Models;

[RegisterScoped(typeof(IValueResolver<NooTubeVideoModel, NooTubeVideoDTO, bool>))]
public class NooTubeVideoIsFavouriteValueResolver
    : IValueResolver<NooTubeVideoModel, NooTubeVideoDTO, bool>
{
    private readonly ICurrentUser _currentUser;

    public NooTubeVideoIsFavouriteValueResolver(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public bool Resolve(
        NooTubeVideoModel source,
        NooTubeVideoDTO destination,
        bool destMember,
        ResolutionContext context
    )
    {
        var userId = _currentUser.UserId;

        if (userId is null)
        {
            return false;
        }

        return source.Favourites.Any(favourite => favourite.UserId == userId);
    }
}
