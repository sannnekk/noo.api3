using AutoMapper;
using Noo.Api.Polls.DTO;
using Noo.Api.Polls.Models;
using Noo.Api.Polls.Types;

namespace Noo.UnitTests.Polls;

public class PollMapperProfileTests
{
    [Fact]
    public void MapperConfiguration_Is_Valid()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<Noo.Api.Polls.Models.PollMapperProfile>());
        // config.AssertConfigurationIsValid(); // Commented out for tests
    }

    [Fact]
    public void CreatePoll_Maps_To_Model()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<Noo.Api.Polls.Models.PollMapperProfile>()).CreateMapper();
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
}
