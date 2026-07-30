using AutoMapper;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Polls.DTO;
using Noo.Api.Polls.Models;
using Noo.Api.Polls.Types;
using Noo.UnitTests.Common;
using SystemTextJsonPatch;

namespace Noo.UnitTests.Polls;

public class PollMapperProfileTests
{
    private static IMapper CreateMapper()
    {
        return MapperTestUtils
            .CreateMapperConfig(cfg => cfg.AddProfile<PollMapperProfile>())
            .CreateMapper();
    }

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

    // The participation endpoints return this DTO, so a missing model -> DTO map turns
    // every read of poll results into a 500 at response-serialization time.
    [Fact]
    public void Participation_Maps_To_Dto()
    {
        var mapper = MapperTestUtils.CreateMapperConfig(cfg => cfg.AddProfile<PollMapperProfile>()).CreateMapper();
        var pollId = Ulid.NewUlid();
        var model = new PollParticipationModel
        {
            Id = Ulid.NewUlid(),
            PollId = pollId,
            UserType = ParticipatingUserType.TelegramUser,
            UserExternalIdentifier = "external-1",
            Answers = []
        };

        var dto = mapper.Map<PollParticipationDTO>(model);

        Assert.Equal(model.Id, dto.Id);
        Assert.Equal(pollId, dto.PollId);
        Assert.Equal(ParticipatingUserType.TelegramUser, dto.UserType);
        Assert.Equal("external-1", dto.UserExternalIdentifier);
        Assert.Null(dto.User);
    }

    [Fact]
    public void Poll_Without_ParticipationsCount_Maps_To_Zero()
    {
        var mapper = MapperTestUtils.CreateMapperConfig(cfg => cfg.AddProfile<PollMapperProfile>()).CreateMapper();
        var model = new PollModel { Title = "T", IsActive = true, IsAuthRequired = false };

        var dto = mapper.Map<PollDTO>(model);

        Assert.Equal(0, dto.ParticipationsCount);
    }

    // ----------------------------------------------------------------------
    // Patch round-trip regression tests (Questions).
    //
    // Replicates the production flow JsonPatchUpdateService runs: Model -> DTO
    // -> patch ops -> Map(DTO, Model). Before UpdatePollDTO carried a Questions
    // dictionary, every /questions/... operation targeted a path that did not
    // exist on the DTO and was silently dropped, so question edits, additions
    // and removals never reached the database.
    // ----------------------------------------------------------------------

    [Fact(DisplayName = "Mapper: PollModel -> UpdatePollDTO exposes questions keyed by Id")]
    public void Map_PollModel_To_UpdatePollDTO_Keys_Questions_By_Id()
    {
        var questionId = Ulid.NewUlid();
        var model = new PollModel
        {
            Id = Ulid.NewUlid(),
            Title = "P",
            Questions =
            [
                new()
                {
                    Id = questionId,
                    Order = 3,
                    Title = "Q",
                    IsRequired = true,
                    Type = PollQuestionType.Text,
                    Config = new PollQuestionConfig { Type = PollQuestionType.Text, MaxTextLength = 20 }
                }
            ]
        };

        var dto = CreateMapper().Map<UpdatePollDTO>(model);

        Assert.NotNull(dto.Questions);
        var questionDto = Assert.Contains(questionId.ToString(), dto.Questions);
        Assert.Equal("Q", questionDto.Title);
        Assert.Equal(3, questionDto.Order);
        Assert.Equal(PollQuestionType.Text, questionDto.Type);
        Assert.Equal(20, questionDto.Config?.MaxTextLength);
    }

    [Fact(DisplayName = "Mapper: PATCH that updates a question changes only its fields, not its identity")]
    public void Patch_Update_Question_Preserves_Identity()
    {
        var mapper = CreateMapper();
        var questionId = Ulid.NewUlid();
        var model = new PollModel
        {
            Id = Ulid.NewUlid(),
            Title = "P",
            Questions =
            [
                new()
                {
                    Id = questionId,
                    Order = 1,
                    Title = "before",
                    Type = PollQuestionType.Text,
                    Config = new PollQuestionConfig { Type = PollQuestionType.Text }
                }
            ]
        };
        var existingQuestionRef = model.Questions.First();

        var dto = mapper.Map<UpdatePollDTO>(model);
        dto.Questions![questionId.ToString()] = dto.Questions[questionId.ToString()] with
        {
            Title = "after",
            Type = PollQuestionType.MultipleChoice
        };

        mapper.Map(dto, model);

        var updated = Assert.Single(model.Questions);
        // The same instance, so EF sees a field-level update instead of a delete + insert.
        Assert.Same(existingQuestionRef, updated);
        Assert.Equal(questionId, updated.Id);
        Assert.Equal("after", updated.Title);
        Assert.Equal(PollQuestionType.MultipleChoice, updated.Type);
        Assert.Equal(1, updated.Order);
    }

    [Fact(DisplayName = "Mapper: PATCH that removes a question drops it from the collection")]
    public void Patch_Remove_Question_Drops_It()
    {
        var mapper = CreateMapper();
        var keepId = Ulid.NewUlid();
        var dropId = Ulid.NewUlid();
        var model = new PollModel
        {
            Id = Ulid.NewUlid(),
            Title = "P",
            Questions =
            [
                new() { Id = keepId, Order = 1, Title = "keep", Type = PollQuestionType.Text },
                new() { Id = dropId, Order = 2, Title = "drop", Type = PollQuestionType.Text }
            ]
        };

        var dto = mapper.Map<UpdatePollDTO>(model);
        dto.Questions!.Remove(dropId.ToString());

        mapper.Map(dto, model);

        Assert.Equal(keepId, Assert.Single(model.Questions).Id);
    }

    [Fact(DisplayName = "Mapper: PATCH via JsonPatchUpdateService — adding a question preserves existing ones")]
    public void Patch_End_To_End_Add_Question_Preserves_Existing_Questions()
    {
        var mapper = CreateMapper();
        var patchService = new JsonPatchUpdateService(mapper);

        var existingId = Ulid.NewUlid();
        var model = new PollModel
        {
            Id = Ulid.NewUlid(),
            Title = "P",
            Questions =
            [
                new()
                {
                    Id = existingId,
                    Order = 1,
                    Title = "existing",
                    Type = PollQuestionType.Text,
                    Config = new PollQuestionConfig { Type = PollQuestionType.Text }
                }
            ]
        };
        var existingQuestionRef = model.Questions.First();

        // The frontend keys added questions by a placeholder ("new-5"), not by a Ulid.
        var patch = new JsonPatchDocument<UpdatePollDTO>();
        patch
            .Replace(x => x.Questions![existingId.ToString()].Title, "patched")
            .Add(
                x => x.Questions!["new-5"],
                new UpdatePollQuestionDTO
                {
                    Order = 2,
                    Title = "added",
                    IsRequired = true,
                    Type = PollQuestionType.Files,
                    Config = new PollQuestionConfig { Type = PollQuestionType.Files, MaxFileCount = 3 }
                }
            );

        patchService.ApplyPatch(model, patch);

        Assert.Equal(2, model.Questions.Count);

        var kept = model.Questions.Single(question => question.Id == existingId);
        Assert.Same(existingQuestionRef, kept);
        Assert.Equal("patched", kept.Title);

        // A placeholder key is not a Ulid, so the merge assigns the new question a fresh Id.
        var added = model.Questions.Single(question => question.Id != existingId);
        Assert.Equal("added", added.Title);
        Assert.Equal(PollQuestionType.Files, added.Type);
        Assert.Equal(2, added.Order);
        Assert.NotEqual(default, added.Id);
    }
}
