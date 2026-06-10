using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.Richtext.Delta;
using Noo.Api.Support.DTO;
using Noo.Api.Support.Models;
using Noo.Api.Support.Services;
using Noo.Api.Support.Types;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.Support;

public class SupportServiceTests
{
    private static (SupportService svc, NooDbContext ctx) CreateService()
    {
        var dbOptions = new DbContextOptionsBuilder<NooDbContext>()
            .UseInMemoryDatabase(databaseName: $"support-tests-{Guid.NewGuid()}")
            .Options;

        var dbConfig = Options.Create(new DbConfig
        {
            User = "u",
            Password = "p",
            Host = "localhost",
            Port = "3306",
            Database = "noo_test",
            CommandTimeout = 30,
            DefaultCharset = "utf8mb4",
            DefaultCollation = "utf8mb4_general_ci"
        });

        var ctx = new NooDbContext(dbConfig, dbOptions);

        var mapperCfg = MapperTestUtils.CreateMapperConfig(cfg => cfg.AddProfile(new SupportMapperProfile()));
        var mapper = mapperCfg.CreateMapper();

        var articleRepo = new SupportArticleRepository(ctx);
        var jsonPatchService = new JsonPatchUpdateService(mapper);

        var svc = new SupportService(articleRepo, jsonPatchService, mapper);
        return (svc, ctx);
    }

    [Fact]
    public async Task CreateArticle_CreatesAndReturnsId()
    {
        var (svc, ctx) = CreateService();

        var dto = new CreateSupportArticleDTO
        {
            Title = "How to use Noo",
            Order = 1,
            Content = DeltaRichText.FromString("hello"),
            IsActive = true,
            Category = SupportCategory.Works
        };

        var id = svc.CreateArticle(dto);

        var saved = await ctx.GetDbSet<SupportArticleModel>().FindAsync(id);
        Assert.NotNull(saved);
        Assert.Equal("How to use Noo", saved!.Title);
        Assert.Equal(SupportCategory.Works, saved.Category);
        Assert.Equal(Slug.Generate("How to use Noo"), saved.Slug);
        Assert.True(saved.IsActive);
        Assert.Equal(1, saved.Order);
        Assert.False(saved.Content.IsEmpty());
    }

    [Fact]
    public async Task DeleteArticle_RemovesEntity()
    {
        var (svc, ctx) = CreateService();
        var art = new SupportArticleModel { Title = "TBD", Slug = "tbd", Order = 1, Category = SupportCategory.Courses };
        ctx.Add(art);
        await ctx.SaveChangesAsync();
        // Detach so Repository.DeleteById's "new T { Id = id }" does not conflict with tracking
        ctx.Entry(art).State = EntityState.Detached;

        svc.DeleteArticle(art.Id);
        await ctx.SaveChangesAsync();

        var exists = await ctx.GetDbSet<SupportArticleModel>().FindAsync(art.Id);
        Assert.Null(exists);
    }

    [Fact]
    public async Task GetArticleAsync_ReturnsArticle_WhenExists()
    {
        var (svc, ctx) = CreateService();
        var art = new SupportArticleModel { Title = "Welcome", Slug = "welcome", Order = 1, Category = SupportCategory.Courses };
        ctx.Add(art);
        await ctx.SaveChangesAsync();

        var result = await svc.GetArticleAsync(art.Slug);
        Assert.NotNull(result);
        Assert.Equal("Welcome", result!.Title);
    }

    [Fact]
    public async Task GetArticleAsync_ReturnsNull_WhenMissing()
    {
        var (svc, _) = CreateService();

        var result = await svc.GetArticleAsync("does-not-exist");

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateArticleAsync_UpdatesSuccessfully()
    {
        var (svc, ctx) = CreateService();
        var art = new SupportArticleModel { Title = "Old title", Slug = "old-title", Order = 1, Category = SupportCategory.Courses };
        ctx.Add(art);
        await ctx.SaveChangesAsync();

        var patch = new SystemTextJsonPatch.JsonPatchDocument<UpdateSupportArticleDTO>()
            .Replace(a => a.Title, "New title")
            .Replace(a => a.IsActive, false);
        await svc.UpdateArticleAsync(art.Id, patch);

        var updated = await ctx.GetDbSet<SupportArticleModel>().FindAsync(art.Id);
        Assert.Equal("New title", updated!.Title);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task UpdateArticleAsync_NotFound_Throws()
    {
        var (svc, _) = CreateService();
        await Assert.ThrowsAsync<Noo.Api.Core.Exceptions.Http.NotFoundException>(() => svc.UpdateArticleAsync(Ulid.NewUlid(), new SystemTextJsonPatch.JsonPatchDocument<UpdateSupportArticleDTO>()));
    }
}
