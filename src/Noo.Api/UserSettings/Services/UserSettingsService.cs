using AutoMapper;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Media.Services;
using Noo.Api.UserSettings.DTO;
using Noo.Api.UserSettings.Models;

namespace Noo.Api.UserSettings.Services;

[RegisterScoped(typeof(IUserSettingsService))]
public class UserSettingsService : IUserSettingsService
{
    private readonly IUserSettingsRepository _userSettingsRepository;

    private readonly IMapper _mapper;

    private readonly IMediaUrlEnricher _mediaUrlEnricher;

    public UserSettingsService(
        IMapper mapper,
        IUserSettingsRepository userSettingsRepository,
        IMediaUrlEnricher mediaUrlEnricher
    )
    {
        _userSettingsRepository = userSettingsRepository;
        _mapper = mapper;
        _mediaUrlEnricher = mediaUrlEnricher;
    }

    public async Task<UserSettingsModel> GetUserSettingsAsync(Ulid userId)
    {
        var settings = await _userSettingsRepository.GetOrCreateAsync(userId);

        await _mediaUrlEnricher.EnrichAsync(settings);

        return settings;
    }

    public async Task UpdateUserSettingsAsync(Ulid userId, UserSettingsUpdateDTO userSettings)
    {
        var userSettingsModel = await _userSettingsRepository.GetOrCreateAsync(userId);

        _mapper.Map(userSettings, userSettingsModel);
    }
}
