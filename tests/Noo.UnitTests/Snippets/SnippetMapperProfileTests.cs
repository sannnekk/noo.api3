using AutoMapper;
using Noo.Api.Core.Utils.Richtext.Delta;
using Noo.Api.Snippets.DTO;
using Noo.Api.Snippets.Models;

namespace Noo.UnitTests.Snippets;

public class SnippetMapperProfileTests
{
    private readonly IMapper _mapper;

    public SnippetMapperProfileTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SnippetMapperProfile>());
        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Map_CreateSnippetDTO_To_SnippetModel()
    {
        var dto = new CreateSnippetDTO
        {
            Name = "Title",
            Content = DeltaRichText.FromString("abc")
        };

        var model = _mapper.Map<SnippetModel>(dto);
        Assert.Equal(dto.Name, model.Name);
        Assert.Equal(dto.Content, model.Content);
        Assert.Equal(default, model.UserId);
        Assert.Null(model.User);
    }

    [Fact]
    public void Map_SnippetModel_To_UpdateSnippetDTO_And_Back()
    {
        var model = new SnippetModel
        {
            Id = Ulid.NewUlid(),
            Name = "Old",
            Content = DeltaRichText.FromString("abc"),
            UserId = Ulid.NewUlid()
        };

        var dto = _mapper.Map<UpdateSnippetDTO>(model);
        Assert.Equal(model.Name, dto.Name);
        Assert.Equal(model.Content, dto.Content);

        dto.Name = "New";
        var updated = _mapper.Map(dto, model);
        Assert.Equal("New", updated.Name);
        Assert.Equal(model.Content, updated.Content);
        Assert.Equal(model.UserId, updated.UserId);
    }

    [Fact]
    public void Map_SnippetModel_To_SnippetDTO()
    {
        var model = new SnippetModel
        {
            Id = Ulid.NewUlid(),
            Name = "N",
            Content = DeltaRichText.FromString("abc"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var dto = _mapper.Map<SnippetDTO>(model);
        Assert.Equal(model.Id, dto.Id);
        Assert.Equal(model.Name, dto.Name);
        Assert.Equal(model.Content, dto.Content);
        Assert.Equal(model.CreatedAt, dto.CreatedAt);
        Assert.Equal(model.UpdatedAt, dto.UpdatedAt);
    }
}
