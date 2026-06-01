using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Noo.Api.Core.Utils.Json;

/// <summary>
/// Serializes every <see cref="DateTime"/> as a Moscow-time instant carrying an
/// explicit <c>+03:00</c> offset, so clients always receive an unambiguous point
/// in time. On read, any value (with <c>Z</c>, an explicit offset, or none) is
/// normalized to Moscow wall-clock time.
/// </summary>
public class MoscowDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();

        if (string.IsNullOrEmpty(raw))
        {
            return default;
        }

        // A value with an explicit offset (Z or +hh:mm) is a real instant: convert it to Moscow.
        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var offset)
            && HasExplicitOffset(raw))
        {
            return Clock.ToMoscow(offset);
        }

        // An offset-less value is assumed to already be Moscow wall-clock time.
        return DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(Clock.WithMoscowOffset(value).ToString("o", CultureInfo.InvariantCulture));
    }

    private static bool HasExplicitOffset(string raw)
    {
        if (raw.EndsWith('Z') || raw.EndsWith('z'))
        {
            return true;
        }

        // Look for a +hh:mm / -hh:mm offset after the time part, ignoring the date's dashes.
        var timeSeparator = raw.IndexOf('T');
        if (timeSeparator < 0)
        {
            return false;
        }

        var timePart = raw.AsSpan(timeSeparator);
        return timePart.Contains('+') || timePart.Contains('-');
    }
}

/// <summary>
/// Nullable counterpart of <see cref="MoscowDateTimeConverter"/>.
/// </summary>
public class MoscowNullableDateTimeConverter : JsonConverter<DateTime?>
{
    private static readonly MoscowDateTimeConverter _inner = new();

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        return _inner.Read(ref reader, typeof(DateTime), options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        _inner.Write(writer, value.Value, options);
    }
}
