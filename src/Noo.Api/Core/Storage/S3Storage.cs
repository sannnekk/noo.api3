using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.Storage;

[RegisterScoped(typeof(IS3Storage))]
public class S3Storage : IS3Storage
{
    private static readonly TimeSpan _defaultUploadExpiration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan _defaultDownloadExpiration = TimeSpan.FromMinutes(10);
    private const string _taggingHeader = "x-amz-tagging";

    private readonly IAmazonS3 _client;
    private readonly S3Config _config;

    public S3Storage(IAmazonS3 client, IOptions<S3Config> config)
    {
        _client = client;
        _config = config.Value;
    }

    public Task<S3PresignedUpload> CreatePresignedUploadAsync(
        string key,
        string contentType,
        IReadOnlyDictionary<string, string>? tags = null,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default
    )
    {
        var ttl = expiration ?? _defaultUploadExpiration;
        var expiresAt = DateTime.UtcNow.Add(ttl);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _config.BucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = expiresAt,
            Protocol = ResolveProtocol(),
        };

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Content-Type"] = contentType,
        };

        if (tags is { Count: > 0 })
        {
            var tagging = EncodeTags(tags);
            request.Headers[_taggingHeader] = tagging;
            headers[_taggingHeader] = tagging;
        }

        var url = _client.GetPreSignedURL(request);
        return Task.FromResult(new S3PresignedUpload(url, headers, expiresAt));
    }

    public Task<string> CreatePresignedDownloadAsync(
        string key,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default
    )
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _config.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiration ?? _defaultDownloadExpiration),
            Protocol = ResolveProtocol(),
        };

        return Task.FromResult(_client.GetPreSignedURL(request));
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await _client.DeleteObjectAsync(
            new DeleteObjectRequest { BucketName = _config.BucketName, Key = key },
            cancellationToken
        );
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.GetObjectMetadataAsync(_config.BucketName, key, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex)
            when (ex.StatusCode == global::System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<S3ObjectMetadata?> GetMetadataAsync(
        string key,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await _client.GetObjectMetadataAsync(
                _config.BucketName,
                key,
                cancellationToken
            );
            return new S3ObjectMetadata(
                Key: key,
                Size: response.ContentLength,
                ETag: response.ETag?.Trim('"') ?? string.Empty,
                ContentType: response.Headers.ContentType ?? string.Empty,
                LastModified: response.LastModified ?? DateTime.MinValue
            );
        }
        catch (AmazonS3Exception ex)
            when (ex.StatusCode == global::System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private Protocol ResolveProtocol()
    {
        return _config.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            ? Protocol.HTTP
            : Protocol.HTTPS;
    }

    private static string EncodeTags(IReadOnlyDictionary<string, string> tags)
    {
        // S3 expects the x-amz-tagging value as a URL-encoded query string.
        return string.Join(
            "&",
            tags.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}")
        );
    }
}
