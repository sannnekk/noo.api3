using System.Text.Json;

namespace Noo.Api.Core.Utils.Json;

public static class NooJsonSerializerOptionsExtensions
{
    /// <summary>
    /// The wire conventions shared by every transport: hyphen-lower-case enums and Moscow-time
    /// <see cref="DateTime"/>s. Realtime payloads go through here too, so a value looks the same
    /// whether it arrived over a controller or a hub.
    /// </summary>
    public static JsonSerializerOptions AddNooConverters(this JsonSerializerOptions options)
    {
        options.Converters.Add(new HyphenLowerCaseStringEnumConverterFactory());
        options.Converters.Add(new MoscowDateTimeConverter());
        options.Converters.Add(new MoscowNullableDateTimeConverter());

        return options;
    }
}
