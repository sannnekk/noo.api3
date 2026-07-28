using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Platform.DTO;
using Noo.Api.Platform.Services;
using Noo.Api.Platform.Types;

namespace Noo.UnitTests.Platform;

public class PlatformServiceTests
{
    [Fact]
    public void GetPlatformVersion_ReturnsVersionOfMostRecentRelease()
    {
        var service = new PlatformService();
        var version = service.GetPlatformVersion();
        var newestRelease = service.GetChangelog().Items.First();

        Assert.Equal(newestRelease.Version, version);
    }

    [Fact]
    public void GetChangelog_ReturnsReleasesNewestFirst()
    {
        var service = new PlatformService();
        var releases = service.GetChangelog().Items.ToList();

        Assert.Equal(releases.OrderByDescending(release => release.Date), releases);
    }

    [Fact]
    public void GetChangelog_ReturnsAtLeastOneEntry_WithValidFields()
    {
        var service = new PlatformService();
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
}
