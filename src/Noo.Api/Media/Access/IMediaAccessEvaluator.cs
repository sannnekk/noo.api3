using Noo.Api.Media.Models;

namespace Noo.Api.Media.Access;

public interface IMediaAccessEvaluator
{
    /// <summary>
    /// Throws <see cref="Core.Exceptions.Http.UnauthorizedException"/> if the caller is anonymous,
    /// or <see cref="Core.Exceptions.Http.ForbiddenException"/> if any registered rule for this
    /// media's category denies the request.
    /// </summary>
    public Task EnsureCanAccessAsync(MediaModel media, CancellationToken cancellationToken = default);
}
