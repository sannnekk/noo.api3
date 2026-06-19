using System.Text.Json.Serialization;

namespace Noo.Api.Core.ThirdPartyServices.Kinescope.Models;

public record KinescopeResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; init; }
}
