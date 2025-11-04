using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Statistics;

public static class StatisticsHelpers
{

    public static Dictionary<string, double?> NormalizeDictionary(Dictionary<UserRoles, int> dict)
    {
        return dict.ToDictionary(
            kvp => kvp.Key.ToString(),
            kvp => (double?)kvp.Value);
    }

    public static Dictionary<string, double?> NormalizeDictionary(Dictionary<DateTime, int> dict)
    {
        return dict.ToDictionary(
            kvp => kvp.Key.ToString("yyyy-MM-dd"),
            kvp => (double?)kvp.Value);
    }

    public static Dictionary<string, double?> NormalizeDictionary(Dictionary<DateTime, double?> dict)
    {
        return dict.ToDictionary(
            kvp => kvp.Key.ToString("yyyy-MM-dd"),
            kvp => kvp.Value);
    }
}
