namespace Noo.Api.Media.DTO;

public static class PresignedMedia
{
    public static IEnumerable<MediaDTO?> Collect(params object?[] nodes)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case MediaDTO media:
                    yield return media;
                    break;

                case IHasPresignedMedia owner:
                    foreach (var media in owner.GetMediaForPresigning())
                    {
                        yield return media;
                    }
                    break;

                case IEnumerable<IHasPresignedMedia> owners:
                    foreach (var owner in owners)
                    {
                        if (owner is null)
                        {
                            continue;
                        }

                        foreach (var media in owner.GetMediaForPresigning())
                        {
                            yield return media;
                        }
                    }
                    break;
            }
        }
    }
}
