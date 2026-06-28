using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.NooTube.DTO;
using Noo.Api.NooTube.Engines;
using Noo.Api.NooTube.Exceptions;
using Noo.Api.NooTube.Filters;
using Noo.Api.NooTube.Models;
using Noo.Api.NooTube.Specifications;
using Noo.Api.NooTube.Types;
using SystemTextJsonPatch;

namespace Noo.Api.NooTube.Services;

[RegisterScoped(typeof(IVideoService))]
public class VideoService : IVideoService
{
    private readonly IVideoRepository _videoRepository;

    private readonly IVideoReactionRepository _videoReactionRepository;

    private readonly IJsonPatchUpdateService _updateService;

    private readonly IVideoEngineResolver _engineResolver;

    private readonly ICurrentUser _currentUser;

    public VideoService(
        IVideoRepository videoRepository,
        IVideoReactionRepository videoReactionRepository,
        IJsonPatchUpdateService updateService,
        IVideoEngineResolver engineResolver,
        ICurrentUser currentUser
    )
    {
        _videoRepository = videoRepository;
        _videoReactionRepository = videoReactionRepository;
        _updateService = updateService;
        _engineResolver = engineResolver;
        _currentUser = currentUser;
    }

    public async Task<NooTubeVideoUploadDTO> CreateAsync(CreateNooTubeVideoDTO createDto)
    {
        var userId = _currentUser.RequireUserId();

        var engine = _engineResolver.Resolve(createDto.ServiceType);

        var ticket = await engine.CreateUploadAsync(
            new VideoUploadRequest
            {
                Title = createDto.Title,
                Description = createDto.Description,
                FileSize = createDto.FileSize,
                FileName = createDto.FileName,
            }
        );

        var model = new NooTubeVideoModel
        {
            Title = createDto.Title,
            Description = createDto.Description,
            ThumbnailId = createDto.ThumbnailId,
            ServiceType = createDto.ServiceType,
            State = VideoState.Uploading,
            ExternalIdentifier = ticket.ExternalId,
            IsListed = createDto.IsListed,
            PublishedAt = createDto.PublishedAt,
            UploadedById = userId,
        };

        _videoRepository.Add(model);

        return new NooTubeVideoUploadDTO
        {
            VideoId = model.Id,
            UploadUrl = ticket.UploadUrl,
            ExternalId = ticket.ExternalId,
        };
    }

    public async Task FinishUploadAsync(Ulid videoId)
    {
        var model = await _videoRepository.GetByIdAsync(videoId);

        model.ThrowNotFoundIfNull();

        model.State = VideoState.Encoding;

        if (string.IsNullOrEmpty(model.ExternalIdentifier))
        {
            return;
        }

        var engine = _engineResolver.Resolve(model.ServiceType);
        var metadata = await engine.GetMetadataAsync(model.ExternalIdentifier);

        ApplyMetadata(model, metadata);
    }

    public async Task UpdateEngineStatusAsync(
        NooTubeServiceType serviceType,
        string externalId,
        string? rawStatus
    )
    {
        var model = await _videoRepository.GetByExternalIdAsync(serviceType, externalId);

        if (model is null)
        {
            return;
        }

        var engine = _engineResolver.Resolve(serviceType);
        var status = engine.MapStatus(rawStatus);

        if (status == VideoProcessingStatus.Ready)
        {
            var metadata = await engine.GetMetadataAsync(externalId);
            ApplyMetadata(model, metadata);
            return;
        }

        if (status is VideoProcessingStatus.Pending or VideoProcessingStatus.Processing)
        {
            model.State = VideoState.Encoding;
        }
    }

    private static void ApplyMetadata(NooTubeVideoModel model, VideoMetadata? metadata)
    {
        if (metadata is null)
        {
            return;
        }

        if (metadata.Status == VideoProcessingStatus.Ready)
        {
            model.State = VideoState.Uploaded;
        }

        model.ExternalUrl = metadata.Url ?? model.ExternalUrl;
        model.ExternalThumbnailUrl = metadata.ThumbnailUrl ?? model.ExternalThumbnailUrl;

        if (metadata.DurationSeconds is not null)
        {
            model.Duration = metadata.DurationSeconds;
        }
    }

    public async Task DeleteAsync(Ulid videoId)
    {
        var model = await _videoRepository.GetByIdAsync(videoId);

        if (model is null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(model.ExternalIdentifier))
        {
            var engine = _engineResolver.Resolve(model.ServiceType);
            await engine.DeleteAsync(model.ExternalIdentifier);
        }

        _videoRepository.Delete(model);
    }

    public Task<SearchResult<NooTubeVideoModel>> GetAsync(VideoFilter filter)
    {
        var userRole = _currentUser.RequireUserRole();
        var userId = _currentUser.RequireUserId();

        if (
            filter.Type == VideoFilterType.Own
            && !_currentUser.IsInRole(UserRoles.Teacher, UserRoles.Admin)
        )
        {
            throw new UnauthorizedException();
        }

        return _videoRepository.SearchAsync(
            filter,
            [new VideoSpecification(userRole, userId, filter.Type)]
        );
    }

    public async Task<NooTubeVideoModel?> GetByIdAsync(Ulid videoId)
    {
        var model = await _videoRepository.GetVideoAsync(videoId);

        if (model is null)
        {
            return null;
        }

        if (model.State is VideoState.Uploading or VideoState.Encoding)
        {
            throw new EncodingNotFinishedYetException();
        }

        return model;
    }

    public async Task ToggleReactionAsync(Ulid videoId, VideoReaction newReaction)
    {
        var userId = _currentUser.RequireUserId();

        var reaction = await _videoReactionRepository.GetAsync(videoId, userId);

        if (reaction?.Reaction == newReaction)
        {
            _videoReactionRepository.Delete(videoId, userId);
            return;
        }

        if (reaction == null)
        {
            reaction = new NooTubeVideoReactionModel
            {
                VideoId = videoId,
                UserId = userId,
                Reaction = newReaction,
            };

            _videoReactionRepository.Add(reaction);
            return;
        }

        reaction.Reaction = newReaction;
    }

    public async Task UpdateAsync(Ulid videoId, JsonPatchDocument<UpdateNooTubeVideoDTO> patch)
    {
        var model = await _videoRepository.GetByIdAsync(videoId);

        model.ThrowNotFoundIfNull();

        _updateService.ApplyPatch(model, patch);
    }
}
