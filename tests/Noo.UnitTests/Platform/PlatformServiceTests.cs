using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Platform.DTO;
using Noo.Api.Platform.Models;
using Noo.Api.Platform.Services;
using Noo.Api.Platform.Types;
using Noo.UnitTests.Common;
using SystemTextJsonPatch;

namespace Noo.UnitTests.Platform;

public class PlatformServiceTests
{
    private static (PlatformService svc, NooDbContext ctx) CreateService()
    {
        var ctx = TestHelpers.CreateInMemoryDb();

        var mapper = MapperTestUtils
            .CreateMapperConfig(cfg => cfg.AddProfile(new PlatformMapperProfile()))
            .CreateMapper();

        var svc = new PlatformService(
            new PlatformSettingsRepository(ctx),
            new JsonPatchUpdateService(mapper)
        );

        return (svc, ctx);
    }

    [Fact]
    public void GetPlatformVersion_ReturnsVersionOfMostRecentRelease()
    {
        var (service, _) = CreateService();
        var version = service.GetPlatformVersion();
        var newestRelease = service.GetChangelog().Items.First();

        Assert.Equal(newestRelease.Version, version);
    }

    [Fact]
    public void GetChangelog_ReturnsReleasesNewestFirst()
    {
        var (service, _) = CreateService();
        var releases = service.GetChangelog().Items.ToList();

        Assert.Equal(releases.OrderByDescending(release => release.Date), releases);
    }

    [Fact]
    public void GetChangelog_ReturnsAtLeastOneEntry_WithValidFields()
    {
        var (service, _) = CreateService();
        var result = service.GetChangelog();

        Assert.NotNull(result);
        Assert.True(result.Total >= 1);
        var first = Assert.IsType<SearchResult<ChangeLogDTO>>(result).Items.First();
        Assert.False(string.IsNullOrWhiteSpace(first.Version));
        Assert.NotEmpty(first.Changes);
        foreach (var change in first.Changes)
        {
            // Ensure the enum value is defined instead of relying on numeric range ordering
            Assert.True(Enum.IsDefined(typeof(ChangeType), change.Type));
            Assert.False(string.IsNullOrWhiteSpace(change.Author));
            Assert.False(string.IsNullOrWhiteSpace(change.Description));
        }
    }

    [Fact]
    public async Task GetSettings_ReturnsDefaults_WithoutWritingARow()
    {
        var (service, ctx) = CreateService();

        var settings = await service.GetSettingsAsync();

        Assert.Equal("https://no-os.ru", settings.ShopLink);
        Assert.Equal("noohelp@mail.ru", settings.SupportEmail);
        Assert.Empty(ctx.GetDbSet<PlatformSettingsModel>());
    }

    [Fact]
    public async Task UpdateSettings_CreatesTheSingleton_OnFirstSave()
    {
        var (service, ctx) = CreateService();

        var patch = new JsonPatchDocument<UpdatePlatformSettingsDTO>();
        patch.Replace(dto => dto.ShopLink, "https://example.com/shop");

        await service.UpdateSettingsAsync(patch);
        await ctx.SaveChangesAsync();

        var saved = Assert.Single(ctx.GetDbSet<PlatformSettingsModel>());
        Assert.Equal(PlatformSettingsModel.SingletonId, saved.Id);
        Assert.Equal("https://example.com/shop", saved.ShopLink);
    }

    [Fact]
    public async Task UpdateSettings_LeavesUnpatchedValuesAlone()
    {
        var (service, ctx) = CreateService();

        var patch = new JsonPatchDocument<UpdatePlatformSettingsDTO>();
        patch.Replace(dto => dto.SupportChatName, "@noo_help");

        await service.UpdateSettingsAsync(patch);
        await ctx.SaveChangesAsync();

        var saved = Assert.Single(ctx.GetDbSet<PlatformSettingsModel>());
        Assert.Equal("@noo_help", saved.SupportChatName);
        Assert.Equal("https://no-os.ru/oferta", saved.TermsLink);
    }

    [Fact]
    public async Task UpdateSettings_PatchesTheSameRowTwice()
    {
        var (service, ctx) = CreateService();

        var first = new JsonPatchDocument<UpdatePlatformSettingsDTO>();
        first.Replace(dto => dto.ShopLink, "https://example.com/one");
        await service.UpdateSettingsAsync(first);
        await ctx.SaveChangesAsync();

        var second = new JsonPatchDocument<UpdatePlatformSettingsDTO>();
        second.Replace(dto => dto.ShopLink, "https://example.com/two");
        await service.UpdateSettingsAsync(second);
        await ctx.SaveChangesAsync();

        var saved = Assert.Single(ctx.GetDbSet<PlatformSettingsModel>());
        Assert.Equal("https://example.com/two", saved.ShopLink);
    }
}
