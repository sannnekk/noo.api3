using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Utils.Richtext.Delta;
using Noo.Api.Support.DTO;
using Noo.Api.Support.Models;
using Noo.Api.Support.Services;
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
        var categoryRepo = new SupportCategoryRepository(ctx);
        var jsonPatchService = new JsonPatchUpdateService(mapper);

        var svc = new SupportService(articleRepo, categoryRepo, jsonPatchService, mapper);
        return (svc, ctx);
    }

    [Fact]
    public async Task CreateCategory_CreatesAndReturnsId()
    {
        var (svc, ctx) = CreateService();

        var dto = new CreateSupportCategoryDTO
        {
            Name = "Getting Started",
            Order = 1,
            IsPinned = true,
            IsActive = true,
            ParentId = null
        };

        var id = svc.CreateCategory(dto);

        var saved = await ctx.GetDbSet<SupportCategoryModel>().FindAsync(id);
        Assert.NotNull(saved);
        Assert.Equal("Getting Started", saved!.Name);
        Assert.True(saved.IsPinned);
        Assert.True(saved.IsActive);
        Assert.Equal(1, saved.Order);
        Assert.Null(saved.ParentId);
    }

    [Fact]
    public async Task CreateArticle_CreatesAndReturnsId()
    {
        var (svc, ctx) = CreateService();

        var cat = new SupportCategoryModel { Name = "FAQ", Order = 1, IsActive = true };
        ctx.Add(cat);
        await ctx.SaveChangesAsync();

        var dto = new CreateSupportArticleDTO
        {
            Title = "How to use Noo",
            Order = 1,
            Content = DeltaRichText.FromString("hello"),
            IsActive = true,
            CategoryId = cat.Id
        };

        var id = svc.CreateArticle(dto);

        var saved = await ctx.GetDbSet<SupportArticleModel>().FindAsync(id);
        Assert.NotNull(saved);
        Assert.Equal("How to use Noo", saved!.Title);
        Assert.Equal(cat.Id, saved.CategoryId);
        Assert.True(saved.IsActive);
        Assert.Equal(1, saved.Order);
        Assert.False(saved.Content.IsEmpty());
    }

    [Fact]
    public async Task DeleteCategory_RemovesEntity()
    {
        var (svc, ctx) = CreateService();
        var cat = new SupportCategoryModel { Name = "Old", Order = 10 };
        ctx.Add(cat);
        await ctx.SaveChangesAsync();
        // Detach so Repository.DeleteById's "new T { Id = id }" does not conflict with tracking
        ctx.Entry(cat).State = EntityState.Detached;

        svc.DeleteCategory(cat.Id);
        await ctx.SaveChangesAsync();

        var exists = await ctx.GetDbSet<SupportCategoryModel>().FindAsync(cat.Id);
        Assert.Null(exists);
    }

    [Fact]
    public async Task DeleteArticle_RemovesEntity()
    {
        var (svc, ctx) = CreateService();
        var cat = new SupportCategoryModel { Name = "FAQ", Order = 1 };
        var art = new SupportArticleModel { Title = "TBD", Order = 1, Category = cat, CategoryId = cat.Id };
        ctx.AddRange(cat, art);
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
        var cat = new SupportCategoryModel { Name = "FAQ", Order = 1 };
        var art = new SupportArticleModel { Title = "Welcome", Order = 1, Category = cat, CategoryId = cat.Id };
        ctx.AddRange(cat, art);
        await ctx.SaveChangesAsync();

        var result = await svc.GetArticleAsync(art.Id);
        Assert.NotNull(result);
        Assert.Equal("Welcome", result!.Title);
    }

    [Fact]
    public async Task GetCategoryTreeAsync_ReturnsOnlyActiveRoots_WithChildren()
    {
        var (svc, ctx) = CreateService();
        var root1 = new SupportCategoryModel { Name = "Root A", Order = 1, IsActive = true };
        var child1 = new SupportCategoryModel { Name = "Child A1", Order = 1, IsActive = true, Parent = root1, ParentId = root1.Id };
        var root2 = new SupportCategoryModel { Name = "Root B", Order = 2, IsActive = false };
        ctx.AddRange(root1, child1, root2);
        await ctx.SaveChangesAsync();

        var tree = await svc.GetCategoryTreeAsync();
        var roots = tree.ToList();

        Assert.Single(roots);
        Assert.Equal("Root A", roots[0].Name);
        Assert.Single(roots[0].Children);
        Assert.Equal("Child A1", roots[0].Children.First().Name);
    }

    [Fact]
    public async Task UpdateArticleAsync_UpdatesSuccessfully()
    {
        var (svc, ctx) = CreateService();
        var cat = new SupportCategoryModel { Name = "Cat", Order = 1, IsActive = true };
        var art = new SupportArticleModel { Title = "Old title", Order = 1, Category = cat, CategoryId = cat.Id };
        ctx.AddRange(cat, art);
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
    public async Task UpdateCategoryAsync_UpdatesSuccessfully()
    {
        var (svc, ctx) = CreateService();
        var root = new SupportCategoryModel { Name = "Root", Order = 1, IsActive = true, IsPinned = false };
        ctx.Add(root);
        await ctx.SaveChangesAsync();

        var patch = new SystemTextJsonPatch.JsonPatchDocument<UpdateSupportCategoryDTO>()
            .Replace(c => c.Name, "Root Updated")
            .Replace(c => c.IsPinned, true);
        await svc.UpdateCategoryAsync(root.Id, patch);

        var updated = await ctx.GetDbSet<SupportCategoryModel>().FindAsync(root.Id);
        Assert.Equal("Root Updated", updated!.Name);
        Assert.True(updated.IsPinned);
    }

    [Fact]
    public async Task UpdateArticleAsync_NotFound_Throws()
    {
        var (svc, _) = CreateService();
        await Assert.ThrowsAsync<Noo.Api.Core.Exceptions.Http.NotFoundException>(() => svc.UpdateArticleAsync(Ulid.NewUlid(), new SystemTextJsonPatch.JsonPatchDocument<UpdateSupportArticleDTO>()));
    }

    [Fact]
    public async Task UpdateCategoryAsync_NotFound_Throws()
    {
        var (svc, _) = CreateService();
        await Assert.ThrowsAsync<Noo.Api.Core.Exceptions.Http.NotFoundException>(() => svc.UpdateCategoryAsync(Ulid.NewUlid(), new SystemTextJsonPatch.JsonPatchDocument<UpdateSupportCategoryDTO>()));
    }
}
