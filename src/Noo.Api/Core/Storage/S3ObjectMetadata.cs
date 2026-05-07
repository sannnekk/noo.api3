namespace Noo.Api.Core.Storage;

public sealed record S3ObjectMetadata(
    string Key,
    long Size,
    string ETag,
    string ContentType,
    DateTime LastModified);
