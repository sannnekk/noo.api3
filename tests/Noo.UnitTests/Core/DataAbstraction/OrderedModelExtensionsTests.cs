using Noo.Api.Core.DataAbstraction.Model;

namespace Noo.UnitTests.Core.DataAbstraction;

public class OrderedModelExtensionsTests
{
    private sealed class Item : OrderedModel
    {
        public string Name { get; init; } = string.Empty;
    }

    private static List<Item> Items(params int[] orders)
    {
        return orders
            .Select((order, index) => new Item { Order = order, Name = $"item-{index}" })
            .ToList();
    }

    [Fact]
    public void Closes_The_Gaps_A_Removal_Left_Behind()
    {
        var items = Items(1, 3, 7);

        items.Renumber();

        Assert.Equal([1, 2, 3], items.Select(item => item.Order));
    }

    [Fact]
    public void Keeps_The_Sequence_The_Items_Were_In()
    {
        var items = Items(50, 10, 30);

        items.Renumber();

        Assert.Equal(
            ["item-1", "item-2", "item-0"],
            items.OrderBy(item => item.Order).Select(item => item.Name)
        );
    }

    [Fact]
    public void Settles_A_Repeated_Order_Without_Shuffling_The_Rest()
    {
        var items = Items(1, 2, 2, 3);

        items.Renumber();

        Assert.Equal([1, 2, 3, 4], items.Select(item => item.Order));
        // The two that shared an order keep the sequence they arrived in.
        Assert.Equal(
            ["item-0", "item-1", "item-2", "item-3"],
            items.OrderBy(item => item.Order).Select(item => item.Name)
        );
    }

    [Fact]
    public void Numbers_From_One_Even_When_Nothing_Did()
    {
        var items = Items(0, 4, 5);

        items.Renumber();

        Assert.Equal([1, 2, 3], items.Select(item => item.Order));
    }

    [Fact]
    public void Leaves_An_Already_Ordered_Collection_Alone()
    {
        var items = Items(1, 2, 3);

        items.Renumber();

        Assert.Equal([1, 2, 3], items.Select(item => item.Order));
    }

    [Fact]
    public void Has_Nothing_To_Do_For_An_Empty_Or_Missing_Collection()
    {
        List<Item>? missing = null;

        missing.Renumber();
        Items().Renumber();
    }
}
