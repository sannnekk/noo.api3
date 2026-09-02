using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Support.DTO;
using Noo.Api.Support.Filters;
using Noo.Api.Support.Models;
using Noo.Api.Support.Services;
using Noo.Api.Support.Types;
using Noo.UnitTests.Common;
using SystemTextJsonPatch;

namespace Noo.UnitTests.Support;

public class SupportFaqServiceTests
{
    private static (SupportFaqService svc, NooDbContext ctx) CreateService()
    {
        var ctx = TestHelpers.CreateInMemoryDb();

        var mapper = MapperTestUtils
            .CreateMapperConfig(cfg => cfg.AddProfile(new SupportMapperProfile()))
            .CreateMapper();

        var svc = new SupportFaqService(
            new SupportFaqItemRepository(ctx),
            new JsonPatchUpdateService(mapper),
            mapper
        );

        return (svc, ctx);
    }

    private static SupportFaqItemModel Item(
        string question,
        SupportCategory? category = null,
        bool isActive = true,
        int order = 1
    )
    {
        return new SupportFaqItemModel
        {
            Question = question,
            Answer = RichTextFactory.Create("answer"),
            Category = category,
            IsActive = isActive,
            Order = order
        };
    }

    [Fact]
    public async Task CreateItem_StoresTheQuestionAndReturnsItsId()
    {
        var (svc, ctx) = CreateService();

        var id = svc.CreateItem(new CreateSupportFaqItemDTO
        {
            Question = "Забыл пароль. Как войти?",
            Order = 2,
            Answer = RichTextFactory.Create("Нажмите «Восстановить»"),
            IsActive = true,
            Category = SupportCategory.Payment
        });

        await ctx.SaveChangesAsync();

        var saved = await ctx.GetDbSet<SupportFaqItemModel>().FindAsync(id);

        Assert.NotNull(saved);
        Assert.Equal("Забыл пароль. Как войти?", saved!.Question);
        Assert.Equal(SupportCategory.Payment, saved.Category);
        Assert.Equal(2, saved.Order);
        Assert.True(saved.IsActive);
        Assert.False(saved.Answer.IsEmpty());
    }

    [Fact]
    public async Task CreateItem_AllowsAnItemWithNoCategory()
    {
        var (svc, ctx) = CreateService();

        var id = svc.CreateItem(new CreateSupportFaqItemDTO
        {
            Question = "Общий вопрос",
            Order = 1,
            Answer = RichTextFactory.Create("Ответ"),
            Category = null
        });

        await ctx.SaveChangesAsync();

        var saved = await ctx.GetDbSet<SupportFaqItemModel>().FindAsync(id);

        Assert.NotNull(saved);
        Assert.Null(saved!.Category);
    }

    [Fact]
    public async Task UpdateItem_ChangesOnlyThePatchedMembers()
    {
        var (svc, ctx) = CreateService();
        var item = Item("Старый вопрос", SupportCategory.Works, order: 5);
        ctx.Add(item);
        await ctx.SaveChangesAsync();

        var patch = new JsonPatchDocument<UpdateSupportFaqItemDTO>();
        patch.Replace(dto => dto.Question, "Новый вопрос");

        await svc.UpdateItemAsync(item.Id, patch);
        await ctx.SaveChangesAsync();

        Assert.Equal("Новый вопрос", item.Question);
        Assert.Equal(SupportCategory.Works, item.Category);
        Assert.Equal(5, item.Order);
        Assert.True(item.IsActive);
    }

    [Fact]
    public async Task UpdateItem_DetachesTheCategoryWhenPatchedToNull()
    {
        var (svc, ctx) = CreateService();
        var item = Item("Вопрос", SupportCategory.Courses);
        ctx.Add(item);
        await ctx.SaveChangesAsync();

        var patch = new JsonPatchDocument<UpdateSupportFaqItemDTO>();
        patch.Replace(dto => dto.Category, null);

        await svc.UpdateItemAsync(item.Id, patch);
        await ctx.SaveChangesAsync();

        Assert.Null(item.Category);
    }

    [Fact]
    public async Task UpdateItem_ThrowsForAnUnknownId()
    {
        var (svc, _) = CreateService();

        await Assert.ThrowsAnyAsync<Exception>(
            () => svc.UpdateItemAsync(Ulid.NewUlid(), new JsonPatchDocument<UpdateSupportFaqItemDTO>())
        );
    }

    [Fact]
    public async Task DeleteItem_RemovesTheEntity()
    {
        var (svc, ctx) = CreateService();
        var item = Item("Вопрос");
        ctx.Add(item);
        await ctx.SaveChangesAsync();

        svc.DeleteItem(item.Id);
        await ctx.SaveChangesAsync();

        Assert.Empty(ctx.GetDbSet<SupportFaqItemModel>());
    }

    [Fact]
    public async Task GetItems_ReturnsEveryCategoryWhenNoneIsAskedFor()
    {
        var (svc, ctx) = CreateService();
        ctx.AddRange(
            Item("Про курсы", SupportCategory.Courses),
            Item("Про оплату", SupportCategory.Payment),
            Item("Общий", null)
        );
        await ctx.SaveChangesAsync();

        var result = await svc.GetItemsAsync(new SupportFaqItemFilter());

        Assert.Equal(3, result.Items.Count());
    }

    [Fact]
    public async Task GetItems_FiltersByCategory()
    {
        var (svc, ctx) = CreateService();
        ctx.AddRange(
            Item("Про курсы", SupportCategory.Courses),
            Item("Про оплату", SupportCategory.Payment)
        );
        await ctx.SaveChangesAsync();

        var result = await svc.GetItemsAsync(
            new SupportFaqItemFilter { Category = SupportCategory.Payment }
        );

        Assert.Equal("Про оплату", Assert.Single(result.Items).Question);
    }

    [Fact]
    public async Task GetItems_FiltersByIsActive()
    {
        var (svc, ctx) = CreateService();
        ctx.AddRange(Item("Видимый"), Item("Скрытый", isActive: false));
        await ctx.SaveChangesAsync();

        var result = await svc.GetItemsAsync(new SupportFaqItemFilter { IsActive = true });

        Assert.Equal("Видимый", Assert.Single(result.Items).Question);
    }
}
