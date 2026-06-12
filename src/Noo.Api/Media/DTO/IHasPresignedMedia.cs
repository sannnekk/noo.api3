namespace Noo.Api.Media.DTO;

public interface IHasPresignedMedia
{
    public IEnumerable<MediaDTO?> GetMediaForPresigning();
}
