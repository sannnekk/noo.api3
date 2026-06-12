using Noo.Api.Media.DTO;

namespace Noo.Api.Media.Services;

public interface IMediaUrlPresigner
{
    public Task SignAsync(IReadOnlyCollection<MediaDTO> media, CancellationToken cancellationToken = default);
}
