namespace Noo.Api.Core.Utils;

public static class NumberListExtensions
{
    public static double? AverageOrNull(this IEnumerable<int> source)
    {
        if (source == null || !source.Any())
            return null;

        return source.Average();
    }

    public static double? AveragePercentageOrNull(this IEnumerable<int> source, int maxScore)
    {
        return source.AverageOrNull() is double average ? (average / maxScore) * 100 : null;
    }

    public static double? MedianOrNull(this IEnumerable<int> source)
    {
        if (source == null || !source.Any())
            return null;

        var data = source.Order().ToArray();

        if (data.Length % 2 == 0)
            return (data[data.Length / 2 - 1] + data[data.Length / 2]) / 2.0;

        return data[data.Length / 2];
    }

    public static double? MedianPercentageOrNull(this IEnumerable<int> source, int maxScore)
    {
        return source.MedianOrNull() is double median ? (median / maxScore) * 100 : null;
    }
}
