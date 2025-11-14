using AutoMapper;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.UserSettings.DTO;
using Noo.Api.UserSettings.Models;

namespace Noo.Api.UserSettings.Services;

[RegisterScoped(typeof(IUserSettingsService))]
public class UserSettingsService : IUserSettingsService
{
    private readonly IUserSettingsRepository _userSettingsRepository;

    private readonly IUnitOfWork _unitOfWork;

    private readonly IMapper _mapper;

    public UserSettingsService(IUnitOfWork unitOfWork, IMapper mapper, IUserSettingsRepository userSettingsRepository)
    {
        _unitOfWork = unitOfWork;
        _userSettingsRepository = userSettingsRepository;
        _mapper = mapper;
    }

    public Task<UserSettingsModel> GetUserSettingsAsync(Ulid userId)
    {
        return _userSettingsRepository.GetOrCreateAsync(userId);
    }

    public async Task UpdateUserSettingsAsync(Ulid userId, UserSettingsUpdateDTO userSettings)
    {
        var userSettingsModel = await _userSettingsRepository.GetOrCreateAsync(userId);

        _mapper.Map(userSettings, userSettingsModel);
        _userSettingsRepository.Update(userSettingsModel);

        await _unitOfWork.CommitAsync();
    }
}
