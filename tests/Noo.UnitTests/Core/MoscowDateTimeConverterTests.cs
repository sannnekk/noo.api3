using System.Text.Json;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.Json;

namespace Noo.UnitTests.Core;

public class MoscowDateTimeConverterTests
{
    private static JsonSerializerOptions Options()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new MoscowDateTimeConverter());
        options.Converters.Add(new MoscowNullableDateTimeConverter());
        return options;
    }

    private sealed record Holder(DateTime Value, DateTime? Nullable);

    [Fact]
    public void Write_Emits_MoscowOffset_For_WallClockValue()
    {
        // A Moscow wall-clock value (as read back from the DB: Unspecified kind).
        var value = new DateTime(2026, 6, 2, 15, 0, 0, DateTimeKind.Unspecified);

        var raw = JsonSerializer.Deserialize<string>(JsonSerializer.Serialize(value, Options()));

        Assert.StartsWith("2026-06-02T15:00:00", raw);
        Assert.EndsWith("+03:00", raw);
    }

    [Fact]
    public void Write_Converts_UtcValue_To_Moscow()
    {
        var value = new DateTime(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc);

        var raw = JsonSerializer.Deserialize<string>(JsonSerializer.Serialize(value, Options()));

        // 12:00 UTC == 15:00 Moscow.
        Assert.StartsWith("2026-06-02T15:00:00", raw);
        Assert.EndsWith("+03:00", raw);
    }

    [Fact]
    public void Read_Converts_Utc_To_MoscowWallClock()
    {
        var result = JsonSerializer.Deserialize<DateTime>("\"2026-06-02T12:00:00Z\"", Options());

        Assert.Equal(new DateTime(2026, 6, 2, 15, 0, 0), result);
    }

    [Fact]
    public void Read_Keeps_MoscowOffset_As_WallClock()
    {
        var result = JsonSerializer.Deserialize<DateTime>("\"2026-06-02T15:00:00+03:00\"", Options());

        Assert.Equal(new DateTime(2026, 6, 2, 15, 0, 0), result);
    }

    [Fact]
    public void Read_Treats_OffsetLess_As_Moscow()
    {
        var result = JsonSerializer.Deserialize<DateTime>("\"2026-06-02T15:00:00\"", Options());

        Assert.Equal(new DateTime(2026, 6, 2, 15, 0, 0), result);
    }

    [Fact]
    public void Nullable_Roundtrips_Null()
    {
        var holder = new Holder(new DateTime(2026, 6, 2, 15, 0, 0, DateTimeKind.Unspecified), null);

        var json = JsonSerializer.Serialize(holder, Options());
        var back = JsonSerializer.Deserialize<Holder>(json, Options());

        Assert.NotNull(back);
        Assert.Null(back!.Nullable);
        Assert.Equal(holder.Value, back.Value);
    }

    [Fact]
    public void Clock_Now_Is_MoscowWallClock()
    {
        var expected = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Clock.MoscowTimeZone);

        Assert.True(Math.Abs((Clock.Now - expected).TotalSeconds) < 5);
    }
}
