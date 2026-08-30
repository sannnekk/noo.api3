using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Noo.Api.Core.Response;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.UserAgent;
using Noo.Api.Statistics.DTO;

namespace Noo.IntegrationTests.Endpoints;

public class StatisticsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public StatisticsTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<StatisticsDTO> GetPlatformStatisticsAsync(HttpClient client)
    {
        var response = await client.GetAsync("/statistics/platform");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var payload = await response.Content.ReadFromJsonAsync<
            ApiResponseDTO<StatisticsDTO>
        >();
        payload.Should().NotBeNull();
        payload!.Data.Should().NotBeNull();

        return payload.Data!;
    }

    [Fact(DisplayName = "GET /statistics/platform breaks the users down by browser and device")]
    public async Task Platform_Statistics_Include_Device_Breakdown()
    {
        var userId = TestDataHelpers.GetUserId(_factory, "student");
        await TestDataHelpers.CreateSessionAsync(
            _factory,
            userId,
            deviceId: "statistics-yandex-mobile",
            browser: BrowserKind.Yandex,
            deviceType: DeviceType.Mobile,
            lastRequestAt: Clock.Now
        );

        using var client = _factory.CreateClient().AsAdmin();
        var statistics = await GetPlatformStatisticsAsync(client);

        var block = statistics.Blocks.Should().ContainSingle(b => b.Title == "Устройства").Subject;

        var browsers = block.Distributions.Should().ContainSingle(d => d.Title == "Браузеры").Subject;
        browsers
            .Entries.Should()
            .Contain(e => e.Label == "Яндекс Браузер" && e.Icon == "yandex" && e.Value >= 1);

        var deviceTypes = block
            .Distributions.Should()
            .ContainSingle(d => d.Title == "Типы устройств")
            .Subject;
        deviceTypes.Entries.Select(e => e.Icon).Should().BeEquivalentTo(
            ["desktop", "mobile", "tablet", "unknown"]
        );
        deviceTypes.Entries.Should().Contain(e => e.Icon == "mobile" && e.Value >= 1);
    }

    [Fact(DisplayName = "GET /statistics/platform is closed to students")]
    public async Task Platform_Statistics_Are_Closed_To_Students()
    {
        using var client = _factory.CreateClient().AsStudent();

        var response = await client.GetAsync("/statistics/platform");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
