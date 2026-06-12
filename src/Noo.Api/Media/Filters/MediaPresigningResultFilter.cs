using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Media.DTO;
using Noo.Api.Media.Services;

namespace Noo.Api.Media.Filters;

[RegisterScoped(typeof(MediaPresigningResultFilter))]
public sealed class MediaPresigningResultFilter : IAsyncResultFilter
{
    private readonly IMediaUrlPresigner _presigner;

    public MediaPresigningResultFilter(IMediaUrlPresigner presigner)
    {
        _presigner = presigner;
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { Value: IHasPresignedMedia owner })
        {
            var media = owner.GetMediaForPresigning().OfType<MediaDTO>().ToArray();

            await _presigner.SignAsync(media, context.HttpContext.RequestAborted);
        }

        await next();
    }
}
