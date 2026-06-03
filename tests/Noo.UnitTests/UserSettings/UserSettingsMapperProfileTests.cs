using AutoMapper;
using Noo.Api.UserSettings.DTO;
using Noo.Api.UserSettings.Models;
using Noo.Api.UserSettings.Types;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.UserSettings;

public class UserSettingsMapperProfileTests
{
    private readonly IMapper _mapper;

    public UserSettingsMapperProfileTests()
    {
        var config = MapperTestUtils.CreateMapperConfig(cfg =>
        {
            cfg.AddProfile<UserSettingsMapperProfile>();
            cfg.AddProfile<Noo.Api.Media.Models.MediaMapperProfile>();
        });
        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Map_UserSettingsModel_To_UserSettingsDTO_Parses_Enums()
    {
        var model = new UserSettingsModel
        {
            Id = Ulid.NewUlid(),
            UserId = Ulid.NewUlid(),
            Theme = UserTheme.Dark,
            FontSize = "Large",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var dto = _mapper.Map<UserSettingsDTO>(model);

        Assert.Equal(UserTheme.Dark, dto.Theme);
        Assert.Equal(FontSize.Large, dto.FontSize);
    }

    [Fact]
    public void Map_UserSettingsUpdateDTO_To_UserSettingsModel_Respects_Ignores()
    {
        var originalId = Ulid.NewUlid();
        var originalUserId = Ulid.NewUlid();
        var originalCreated = DateTime.UtcNow.AddDays(-1);
        var originalUpdated = DateTime.UtcNow.AddHours(-1);

        var model = new UserSettingsModel
        {
            Id = originalId,
            UserId = originalUserId,
            Theme = UserTheme.Light,
            FontSize = "Small",
            CreatedAt = originalCreated,
            UpdatedAt = originalUpdated
        };

        var update = new UserSettingsUpdateDTO
        {
            Theme = UserTheme.Dark,
            FontSize = FontSize.Large
        };

        var mapped = _mapper.Map(update, model);

        Assert.Equal(UserTheme.Dark, mapped.Theme);
        Assert.Equal("Large", mapped.FontSize);
        Assert.Equal(originalId, mapped.Id);
        Assert.Equal(originalUserId, mapped.UserId);
        Assert.Equal(originalCreated, mapped.CreatedAt);
        Assert.Equal(originalUpdated, mapped.UpdatedAt);
    }
}
