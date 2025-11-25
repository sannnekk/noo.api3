using AutoMapper;
using Noo.Api.Works.DTO;
using Noo.Api.Works.Models;
using Noo.Api.Works.Types;
using Noo.Api.Subjects.Models;
using Noo.Api.Core.Utils.Richtext.Delta;

namespace Noo.UnitTests.Works;

public class WorkMapperProfileTests
{
    private readonly IMapper _mapper;

    public WorkMapperProfileTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<WorkMapperProfile>();
            cfg.AddProfile<SubjectMapperProfile>();
        });
        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact(DisplayName = "Mapper: CreateWorkDTO -> WorkModel maps correctly")]
    public void Map_CreateWorkDTO_To_WorkModel()
    {
        var dto = new CreateWorkDTO
        {
            Title = "Algebra Test",
            Type = WorkType.Test,
            Description = "desc",
            SubjectId = Ulid.NewUlid(),
            Tasks =
            [
                new CreateWorkTaskDTO { Type = WorkTaskType.Text, Order = 1, MaxScore = 5, Content = DeltaRichText.FromString("abc") }
            ]
        };

        var model = _mapper.Map<WorkModel>(dto);

        Assert.Equal(dto.Title, model.Title);
        Assert.Equal(dto.Type, model.Type);
        Assert.Equal(dto.Description, model.Description);
        Assert.Equal(dto.SubjectId, model.SubjectId);
        Assert.Null(model.Subject);
        Assert.NotNull(model.CourseWorkAssignments);
    }

    [Fact(DisplayName = "Mapper: WorkModel -> UpdateWorkDTO and back updates title only")]
    public void Map_WorkModel_To_UpdateWorkDTO_And_Back()
    {
        var model = new WorkModel
        {
            Id = Ulid.NewUlid(),
            Title = "Geometry",
            Type = WorkType.MiniTest,
            Description = "d",
            SubjectId = Ulid.NewUlid()
        };

        var dto = _mapper.Map<UpdateWorkDTO>(model);
        Assert.Equal(model.Title, dto.Title);
        Assert.Equal(model.Type, dto.Type);
        Assert.Equal(model.Description, dto.Description);
        Assert.Equal(model.SubjectId, dto.SubjectId);

        // change fields via DTO and map back, ignoring non-updatable fields as configured
        dto.Title = "Geometry Updated";
        var updated = _mapper.Map(dto, model);
        Assert.Equal("Geometry Updated", updated.Title);
        Assert.Equal(model.Type, updated.Type);
    }
}
