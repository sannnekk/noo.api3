using System.Text.Json;
using System.Text.Json.Serialization;

namespace Noo.Api.Core.ThirdPartyServices.Kinescope;

internal static class KinescopeSerialization
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower),
        },
    };
}
