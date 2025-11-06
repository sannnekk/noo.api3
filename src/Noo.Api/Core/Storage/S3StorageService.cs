using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.Storage;

[RegisterScoped(typeof(IS3StorageService))]
public class S3StorageService : IS3StorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Config _options;

    public S3StorageService(IAmazonS3 s3Client, IOptions<S3Config> options)
    {
        _s3Client = s3Client;
        _options = options.Value;
    }

    public string GetPreSignedUploadUrl(string key, string contentType, bool isPublic, int expiresMinutes = 15)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = DateTime.UtcNow.AddMinutes(expiresMinutes),
        };
        if (isPublic)
            request.Headers["x-amz-acl"] = "public-read";
        return _s3Client.GetPreSignedURL(request);
    }

    public string GetPreSignedDownloadUrl(string key, int expiresMinutes = 10)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(expiresMinutes),
        };
        return _s3Client.GetPreSignedURL(request);
    }
}
