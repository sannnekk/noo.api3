using AutoMapper;
using Noo.Api.Core.Utils.DI;
using Noo.Api.UserSettings.DTO;
using Noo.Api.UserSettings.Models;

namespace Noo.Api.UserSettings.Services;

[RegisterScoped(typeof(IUserSettingsService))]
public class UserSettingsService : IUserSettingsService
{
    private readonly IUserSettingsRepository _userSettingsRepository;

    private readonly IMapper _mapper;

    public UserSettingsService(
        IMapper mapper,
        IUserSettingsRepository userSettingsRepository
    )
    {
        _userSettingsRepository = userSettingsRepository;
        _mapper = mapper;
    }

    public async Task<UserSettingsModel> GetUserSettingsAsync(Ulid userId)
    {
        var settings = await _userSettingsRepository.GetOrCreateAsync(userId);

        return settings;
    }

    public async Task UpdateUserSettingsAsync(Ulid userId, UserSettingsUpdateDTO userSettings)
    {
        var userSettingsModel = await _userSettingsRepository.GetOrCreateAsync(userId);

        _mapper.Map(userSettings, userSettingsModel);
    }
}
