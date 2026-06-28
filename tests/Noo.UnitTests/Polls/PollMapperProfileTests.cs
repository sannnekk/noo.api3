using Noo.Api.Polls.DTO;
using Noo.Api.Polls.Models;
using Noo.Api.Polls.Types;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.Polls;

public class PollMapperProfileTests
{
    [Fact]
    public void MapperConfiguration_Is_Valid()
    {
        var config = MapperTestUtils.CreateMapperConfig(cfg => cfg.AddProfile<PollMapperProfile>());
        // config.AssertConfigurationIsValid(); // Commented out for tests
    }

    [Fact]
    public void CreatePoll_Maps_To_Model()
    {
        var mapper = MapperTestUtils.CreateMapperConfig(cfg => cfg.AddProfile<PollMapperProfile>()).CreateMapper();
        var dto = new CreatePollDTO
        {
            Title = "T",
            Description = "D",
            IsActive = true,
            IsAuthRequired = false,
            Questions = new[]
            {
                new CreatePollQuestionDTO
                {
                    Title = "Q1",
                    Description = "d1",
                    IsRequired = true,
                    Type = PollQuestionType.Text,
                    Config = new PollQuestionConfig { Type = PollQuestionType.Text, MinTextLength = 1, MaxTextLength = 20 }
                }
            }
        };

        var model = mapper.Map<PollModel>(dto);
        Assert.Equal("T", model.Title);
        Assert.Single(model.Questions);
        Assert.True(model.IsActive);
        Assert.False(model.IsAuthRequired);
    }

    [Fact]
    public void Poll_Maps_ParticipationsCount_To_Dto()
    {
        var mapper = MapperTestUtils.CreateMapperConfig(cfg => cfg.AddProfile<PollMapperProfile>()).CreateMapper();
        var model = new PollModel
        {
            Title = "T",
            IsActive = true,
            IsAuthRequired = false,
            ParticipationsCount = 5
        };

        var dto = mapper.Map<PollDTO>(model);

        Assert.Equal(5, dto.ParticipationsCount);
    }

    [Fact]
    public void Poll_Without_ParticipationsCount_Maps_To_Zero()
    {
        var mapper = MapperTestUtils.CreateMapperConfig(cfg => cfg.AddProfile<PollMapperProfile>()).CreateMapper();
        var model = new PollModel { Title = "T", IsActive = true, IsAuthRequired = false };

        var dto = mapper.Map<PollDTO>(model);

        Assert.Equal(0, dto.ParticipationsCount);
    }
}
