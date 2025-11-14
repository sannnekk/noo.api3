namespace Noo.Api.Core.Request;

public static class RouteValueExtensions
{
    public static Ulid? GetUlidValue(this RouteValueDictionary routeValues, string key)
    {
        if (routeValues.TryGetValue(key, out var valueObj) && valueObj != null && Ulid.TryParse(valueObj.ToString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }
}
