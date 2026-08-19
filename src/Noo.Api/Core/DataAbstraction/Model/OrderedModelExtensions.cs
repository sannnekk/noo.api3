namespace Noo.Api.Core.DataAbstraction.Model;

public static class OrderedModelExtensions
{
    /// <summary>
    /// Renumbers a collection so its <see cref="OrderedModel.Order"/> runs 1..n, with no
    /// gaps and no repeats, keeping the sequence the items are already in.
    /// </summary>
    /// <remarks>
    /// A client sends the order it believes in, and after it has removed something from
    /// the middle that belief has holes in it — an order of 1, 3, 7 for three items. The
    /// number is read by people ("Задание №7"), so it has to be the position, which makes
    /// this the writer's job rather than something every reader sorts out for itself.
    /// <para>
    /// Items already sharing an order keep the sequence they arrived in, so a client that
    /// numbers two items the same gets a stable answer rather than an arbitrary one.
    /// </para>
    /// </remarks>
    public static void Renumber<T>(this IEnumerable<T>? items)
        where T : OrderedModel
    {
        if (items is null)
        {
            return;
        }

        var position = 1;

        // OrderBy is stable, so equal orders come out in the order they went in.
        foreach (var item in items.OrderBy(item => item.Order))
        {
            item.Order = position++;
        }
    }
}
