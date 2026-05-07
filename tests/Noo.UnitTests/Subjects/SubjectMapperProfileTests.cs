using AutoMapper;
using Noo.Api.Subjects.DTO;
using Noo.Api.Subjects.Models;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.Subjects;

public class SubjectMapperProfileTests
{
    private readonly IMapper _mapper;

    public SubjectMapperProfileTests()
    {
        var config = MapperTestUtils.CreateMapperConfig(cfg => cfg.AddProfile<SubjectMapperProfile>());
        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Map_SubjectCreationDTO_To_SubjectModel()
    {
        var dto = new SubjectCreationDTO
        {
            Name = "Math",
            Color = "#FF0000"
        };

        var model = _mapper.Map<SubjectModel>(dto);

        Assert.Equal(dto.Name, model.Name);
        Assert.Equal(dto.Color, model.Color);
        Assert.Empty(model.Works);
        Assert.Empty(model.Courses);
        Assert.Empty(model.MentorAssignments);
    }

    [Fact]
    public void Map_SubjectModel_To_SubjectUpdateDTO_And_Back()
    {
        var model = new SubjectModel
        {
            Id = Ulid.NewUlid(),
            Name = "Physics",
            Color = "#00FF00"
        };

        var dto = _mapper.Map<SubjectUpdateDTO>(model);
        Assert.Equal(model.Name, dto.Name);
        Assert.Equal(model.Color, dto.Color);

        dto.Name = "Physics 2";
        var updated = _mapper.Map(dto, model);
        Assert.Equal("Physics 2", updated.Name);
        Assert.Equal(model.Color, updated.Color);
    }

    [Fact]
    public void Map_SubjectModel_To_SubjectDTO()
    {
        var model = new SubjectModel
        {
            Id = Ulid.NewUlid(),
            Name = "Chemistry",
            Color = "#0000FF",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var dto = _mapper.Map<SubjectDTO>(model);

        Assert.Equal(model.Id, dto.Id);
        Assert.Equal(model.Name, dto.Name);
        Assert.Equal(model.Color, dto.Color);
    }
}
