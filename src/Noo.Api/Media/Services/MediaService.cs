using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Storage;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Media.Access;
using Noo.Api.Media.Models;
using Noo.Api.Media.Types;
using Noo.Api.Users.Services;

namespace Noo.Api.Media.Services;

[RegisterScoped(typeof(IMediaService))]
public class MediaService : IMediaService
{
    private readonly IS3Storage _s3;
    private readonly IMediaRepository _media;
    private readonly IUserRepository _users;
    private readonly IMediaAccessEvaluator _access;
    private readonly ICurrentUser _currentUser;

    public MediaService(
        IS3Storage s3,
        IMediaRepository media,
        IUserRepository users,
        IMediaAccessEvaluator access,
        ICurrentUser currentUser)
    {
        _s3 = s3;
        _media = media;
        _users = users;
        _access = access;
        _currentUser = currentUser;
    }

    public async Task<UploadTicket> RequestUploadAsync(
        MediaCategory category,
        string fileName,
        string contentType,
        Ulid? entityId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new BadRequestException("File name is required");
        }

        if (!MediaConfig.AllowedContentTypes.Contains(contentType))
        {
            throw new UnsupportedMediaTypeException();
        }

        if (_currentUser.UserId is not { } ownerId)
        {
            throw new UnauthorizedException();
        }

        var owner = await _users.GetByIdAsync(ownerId)
            ?? throw new UnauthorizedException();

        var mediaId = Ulid.NewUlid();
        var extension = ExtractExtension(fileName);
        var key = BuildKey(category, ownerId, mediaId, extension);

        var tags = new Dictionary<string, string>
        {
            ["uploader-id"] = ownerId.ToString(),
            ["uploader-username"] = owner.Username,
            ["category"] = category.ToSlug(),
        };

        var upload = await _s3.CreatePresignedUploadAsync(key, contentType, tags, cancellationToken: cancellationToken);

        _media.Add(new MediaModel
        {
            Id = mediaId,
            Path = key,
            Name = mediaId.ToString() + (extension.Length > 0 ? "." + extension : string.Empty),
            ActualName = fileName,
            Extension = extension,
            Size = 0,
            Category = category,
            Status = MediaStatus.Pending,
            EntityId = entityId,
            OwnerId = ownerId,
        });

        return new UploadTicket(mediaId, upload);
    }

    public async Task<MediaModel> CompleteUploadAsync(
        Ulid mediaId,
        long size,
        string? etag = null,
        CancellationToken cancellationToken = default)
    {
        var media = await _media.GetByIdAsync(mediaId)
            ?? throw new NotFoundException("Media not found");

        EnsureOwnerOrAdmin(media);

        if (size < 0 || size > MediaConfig.MaxFileSize)
        {
            throw new BadRequestException("Reported size is invalid");
        }

        media.Size = size;
        media.Status = MediaStatus.Completed;
        if (!string.IsNullOrEmpty(etag))
        {
            media.Hash = etag.Trim('"');
        }

        _media.Update(media);

        media.Url = await _s3.CreatePresignedDownloadAsync(media.Path, cancellationToken: cancellationToken);
        return media;
    }

    public async Task<string> GetDownloadUrlAsync(Ulid mediaId, CancellationToken cancellationToken = default)
    {
        var media = await _media.GetByIdAsync(mediaId)
            ?? throw new NotFoundException("Media not found");

        await _access.EnsureCanAccessAsync(media, cancellationToken);

        return await _s3.CreatePresignedDownloadAsync(media.Path, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Ulid mediaId, CancellationToken cancellationToken = default)
    {
        var media = await _media.GetByIdAsync(mediaId)
            ?? throw new NotFoundException("Media not found");

        EnsureOwnerOrAdmin(media);

        await _s3.DeleteAsync(media.Path, cancellationToken);
        _media.Delete(media);
    }

    private void EnsureOwnerOrAdmin(MediaModel media)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException();
        }

        var isOwner = _currentUser.UserId == media.OwnerId;
        var isAdmin = _currentUser.IsInRole(UserRoles.Admin);

        if (!isOwner && !isAdmin)
        {
            throw new ForbiddenException();
        }
    }

    private static string BuildKey(MediaCategory category, Ulid ownerId, Ulid mediaId, string extension)
    {
        var suffix = extension.Length > 0 ? "." + extension : string.Empty;
        return $"{category.ToSlug()}/{ownerId}/{mediaId}{suffix}";
    }

    private static string ExtractExtension(string fileName)
    {
        var raw = Path.GetExtension(fileName);
        return string.IsNullOrEmpty(raw)
            ? string.Empty
            : raw.TrimStart('.').ToLowerInvariant();
    }
}
