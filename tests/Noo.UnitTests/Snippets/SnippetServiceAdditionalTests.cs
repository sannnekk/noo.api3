using AutoMapper;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Utils.Richtext.Delta;
using Noo.Api.Snippets;
using Noo.Api.Snippets.DTO;
using Noo.Api.Snippets.Models;
using Noo.Api.Snippets.Services;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.Snippets;

public class SnippetServiceAdditionalTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SnippetMapperProfile>());
        config.AssertConfigurationIsValid();
        return config.CreateMapper();
    }

    [Fact]
    public async Task GetSnippets_Capped_At_MaxSnippetsPerUser()
    {
        var db = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(db);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var service = new SnippetService(uow, CreateMapper());
        var userId = Ulid.NewUlid();

        for (int i = 0; i < SnippetConfig.MaxSnippetsPerUser + 5; i++)
        {
            await service.CreateSnippetAsync(userId, new CreateSnippetDTO
            {
                Name = $"S{i}",
                Content = new DeltaRichText("{}")
            });
        }

        var list = await service.GetSnippetsAsync(userId);
        Assert.Equal(SnippetConfig.MaxSnippetsPerUser, list.Items.Count());
        Assert.True(list.Total >= SnippetConfig.MaxSnippetsPerUser);
    }

    [Fact]
    public async Task Update_NonExistent_Snippet_Should_Throw_NotFound()
    {
        var db = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(db);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var service = new SnippetService(uow, CreateMapper());
        var patch = new SystemTextJsonPatch.JsonPatchDocument<UpdateSnippetDTO>();
        patch.Replace(x => x.Name, "X");
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateSnippetAsync(Ulid.NewUlid(), Ulid.NewUlid(), patch, new ModelStateDictionary()));
    }

    [Fact]
    public async Task Delete_NonExistent_Snippet_Should_Throw_NotFound()
    {
        var db = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(db);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var service = new SnippetService(uow, CreateMapper());
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.DeleteSnippetAsync(Ulid.NewUlid(), Ulid.NewUlid()));
    }

    [Fact]
    public async Task Update_With_Empty_Patch_No_Change()
    {
        var db = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(db);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var service = new SnippetService(uow, mapper);
        var userId = Ulid.NewUlid();
        await service.CreateSnippetAsync(userId, new CreateSnippetDTO { Name = "Initial", Content = new DeltaRichText("{}") });
        var list = await service.GetSnippetsAsync(userId);
        var snippet = list.Items.First();

        var emptyPatch = new SystemTextJsonPatch.JsonPatchDocument<UpdateSnippetDTO>();
        await service.UpdateSnippetAsync(userId, snippet.Id, emptyPatch, new ModelStateDictionary());

        using var verifyContext = TestHelpers.CreateInMemoryDb(db);
        var verifyUow = TestHelpers.CreateUowMock(verifyContext).Object;
        var verifyService = new SnippetService(verifyUow, mapper);
        var after = await verifyService.GetSnippetsAsync(userId);
        var afterSnippet = after.Items.Single();
        Assert.Equal("Initial", afterSnippet.Name);
    }

    [Fact]
    public async Task Update_Content_Field_Works()
    {
        var db = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(db);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var service = new SnippetService(uow, mapper);
        var userId = Ulid.NewUlid();
        await service.CreateSnippetAsync(userId, new CreateSnippetDTO { Name = "Initial", Content = new DeltaRichText("{}") });
        var list = await service.GetSnippetsAsync(userId);
        var snippet = list.Items.First();

        var patch = new SystemTextJsonPatch.JsonPatchDocument<UpdateSnippetDTO>();
        var newContent = DeltaRichText.FromString("Updated content");
        patch.Replace(x => x.Content, newContent);
        await service.UpdateSnippetAsync(userId, snippet.Id, patch, new ModelStateDictionary());

        using var verifyContext = TestHelpers.CreateInMemoryDb(db);
        var verifyUow = TestHelpers.CreateUowMock(verifyContext).Object;
        var verifyService = new SnippetService(verifyUow, mapper);
        var after = await verifyService.GetSnippetsAsync(userId);
        var afterSnippet = after.Items.Single();
        Assert.Contains("Updated content", afterSnippet.Content?.ToString());
    }
}
