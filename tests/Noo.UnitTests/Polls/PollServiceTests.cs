using AutoMapper;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Media.Models;
using Noo.Api.Media.Services;
using Noo.Api.Media.Types;
using Noo.Api.Polls.DTO;
using Noo.Api.Polls.Exceptions;
using Noo.Api.Polls.Filters;
using Noo.Api.Polls.Models;
using Noo.Api.Polls.Services;
using Noo.Api.Polls.Types;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Users.Models;
using Noo.UnitTests.Common;
using SystemTextJsonPatch;
using Noo.Api.Core.Request.Patching;

namespace Noo.UnitTests.Polls;

public class PollServiceTests
{
    private sealed class TestCurrentUser : ICurrentUser
    {
        public TestCurrentUser(Ulid? userId, UserRoles? role = null, bool isAuthenticated = true)
        {
            UserId = userId;
            UserRole = role;
            IsAuthenticated = isAuthenticated;
        }

        public Ulid? UserId { get; }
        public UserRoles? UserRole { get; }
        public bool IsAuthenticated { get; }
        public bool IsInRole(params UserRoles[] role) => UserRole.HasValue && role.Contains(UserRole.Value);
    }
    private static IMapper CreateMapper()
    {
        var config = MapperTestUtils.CreateMapperConfig(cfg => cfg.AddProfile<Noo.Api.Polls.Models.PollMapperProfile>());
        // config.AssertConfigurationIsValid(); // Commented out for tests
        return config.CreateMapper();
    }

    [Fact]
    public async Task Create_Get_Search_Update_Delete_Poll_Flow()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(dbName);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var pollRepo = new PollRepository(context);
        var pollParticipationRepo = new PollParticipationRepository(context);
        var pollAnswerRepo = new PollAnswerRepository(context);
        var mediaRepo = new MediaRepository(context);
        var currentUser = new TestCurrentUser(null, UserRoles.Admin);
        var jsonPatch = new JsonPatchUpdateService(mapper);
        var service = new PollService(mapper, pollRepo, pollParticipationRepo, pollAnswerRepo, mediaRepo, currentUser, jsonPatch);

        // Create poll with one question
        var create = new CreatePollDTO
        {
            Title = "Satisfaction",
            Description = "Quick survey",
            IsActive = true,
            IsAuthRequired = false,
            Questions = new[]
            {
                new CreatePollQuestionDTO
                {
                    Title = "Rate our app",
                    Description = "1-5",
                    IsRequired = true,
                    Type = PollQuestionType.Rating,
                    Config = new PollQuestionConfig { Type = PollQuestionType.Rating, MinRating = 1, MaxRating = 5 }
                }
            }
        };

        var pollId = service.CreatePoll(create);
        await uow.CommitAsync();
        Assert.NotEqual(default, pollId);

        // Get
        var fetched = await service.GetPollAsync(pollId);
        Assert.NotNull(fetched);
        Assert.Equal("Satisfaction", fetched!.Title);
        Assert.Single(fetched.Questions);

        // Search
        var search = await service.GetPollsAsync(new PollFilter { Page = 1, PerPage = 10, Search = "satis" });
        Assert.Equal(1, search.Total);
        Assert.Single(search.Items);

        // Update title via patch
        var patch = new JsonPatchDocument<UpdatePollDTO>();
        patch.Replace(x => x.Title, "Updated Title");
        await service.UpdatePollAsync(pollId, patch);
        await uow.CommitAsync();

        var updated = await service.GetPollAsync(pollId);
        Assert.Equal("Updated Title", updated!.Title);

        // Delete in a fresh context to avoid tracking issues
        using var deleteContext = TestHelpers.CreateInMemoryDb(dbName);
        var deleteUow = TestHelpers.CreateUowMock(deleteContext).Object;
        var deletePollRepo = new PollRepository(deleteContext);
        var deletePollParticipationRepo = new PollParticipationRepository(deleteContext);
        var deletePollAnswerRepo = new PollAnswerRepository(deleteContext);
        var deleteCurrentUser = new TestCurrentUser(null, UserRoles.Admin);
        var deleteJsonPatch = new JsonPatchUpdateService(mapper);
        var deleteMediaRepo = new MediaRepository(deleteContext);
        var deleteService = new PollService(mapper, deletePollRepo, deletePollParticipationRepo, deletePollAnswerRepo, deleteMediaRepo, deleteCurrentUser, deleteJsonPatch);
        deleteService.DeletePoll(pollId);
        await deleteUow.CommitAsync();

        using var verifyContext = TestHelpers.CreateInMemoryDb(dbName);
        var verifyUow = TestHelpers.CreateUowMock(verifyContext).Object;
        var verifyPollRepo = new PollRepository(verifyContext);
        var verifyPollParticipationRepo = new PollParticipationRepository(verifyContext);
        var verifyPollAnswerRepo = new PollAnswerRepository(verifyContext);
        var verifyCurrentUser = new TestCurrentUser(null, UserRoles.Admin);
        var verifyJsonPatch = new JsonPatchUpdateService(mapper);
        var verifyMediaRepo = new MediaRepository(verifyContext);
        var verifyService = new PollService(mapper, verifyPollRepo, verifyPollParticipationRepo, verifyPollAnswerRepo, verifyMediaRepo, verifyCurrentUser, verifyJsonPatch);
        await Assert.ThrowsAsync<NotFoundException>(() => verifyService.GetPollAsync(pollId));
    }

    [Fact]
    public async Task Participate_Prevents_Duplicate_By_UserId_Or_ExternalId()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(dbName);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var pollRepo = new PollRepository(context);
        var pollParticipationRepo = new PollParticipationRepository(context);
        var pollAnswerRepo = new PollAnswerRepository(context);
        var mediaRepo = new MediaRepository(context);
        var currentUser = new TestCurrentUser(null, UserRoles.Admin);
        var jsonPatch = new JsonPatchUpdateService(mapper);
        var service = new PollService(mapper, pollRepo, pollParticipationRepo, pollAnswerRepo, mediaRepo, currentUser, jsonPatch);

        // Seed poll
        var poll = new PollModel { Title = "P", IsActive = true, IsAuthRequired = false };
        context.Add(poll);
        await context.SaveChangesAsync();

        var userId = Ulid.NewUlid();
        const string extId = "ext-42";

        // 1) By userId: create with a current user, then attempt duplicate with the same user
        var withUser = new PollService(mapper, pollRepo, pollParticipationRepo, pollAnswerRepo, mediaRepo, new TestCurrentUser(userId), jsonPatch);
        await withUser.ParticipateAsync(poll.Id, new CreatePollParticipationDTO
        {
            UserType = ParticipatingUserType.AuthenticatedUser,
            UserExternalIdentifier = null
        });
        await uow.CommitAsync();
        await Assert.ThrowsAsync<Noo.Api.Polls.Exceptions.UserAlreadyVotedException>(async () =>
        {
            await withUser.ParticipateAsync(poll.Id, new CreatePollParticipationDTO
            {
                UserType = ParticipatingUserType.AuthenticatedUser,
                UserExternalIdentifier = null
            });
        });

        // 2) By external id: create with ext id, then attempt duplicate with the same ext id
        await service.ParticipateAsync(poll.Id, new CreatePollParticipationDTO
        {
            UserType = ParticipatingUserType.TelegramUser,
            UserExternalIdentifier = extId
        });
        await uow.CommitAsync();
        await Assert.ThrowsAsync<Noo.Api.Polls.Exceptions.UserAlreadyVotedException>(async () =>
        {
            await service.ParticipateAsync(poll.Id, new CreatePollParticipationDTO
            {
                UserType = ParticipatingUserType.TelegramUser,
                UserExternalIdentifier = extId
            });
        });
    }

    [Fact]
    public async Task GetUserParticipations_Returns_Only_Own_Participations_With_Polls()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(dbName);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var pollRepo = new PollRepository(context);
        var pollParticipationRepo = new PollParticipationRepository(context);
        var pollAnswerRepo = new PollAnswerRepository(context);
        var mediaRepo = new MediaRepository(context);
        var jsonPatch = new JsonPatchUpdateService(mapper);

        var userId = Ulid.NewUlid();
        var otherUserId = Ulid.NewUlid();

        // Poll A: target user + another user, Poll B: another user only, Poll C: target user only
        var pollA = new PollModel { Title = "A", IsActive = true, IsAuthRequired = false };
        var pollB = new PollModel { Title = "B", IsActive = true, IsAuthRequired = false };
        var pollC = new PollModel { Title = "C", IsActive = true, IsAuthRequired = false };
        context.AddRange(pollA, pollB, pollC);
        await context.SaveChangesAsync();

        context.AddRange(
            new PollParticipationModel { PollId = pollA.Id, UserId = userId, UserType = ParticipatingUserType.AuthenticatedUser },
            new PollParticipationModel { PollId = pollA.Id, UserId = otherUserId, UserType = ParticipatingUserType.AuthenticatedUser },
            new PollParticipationModel { PollId = pollB.Id, UserId = otherUserId, UserType = ParticipatingUserType.AuthenticatedUser },
            new PollParticipationModel { PollId = pollC.Id, UserId = userId, UserType = ParticipatingUserType.AuthenticatedUser }
        );
        await context.SaveChangesAsync();

        var service = new PollService(mapper, pollRepo, pollParticipationRepo, pollAnswerRepo, mediaRepo, new TestCurrentUser(userId, UserRoles.Student), jsonPatch);

        var result = await service.GetUserParticipationsAsync(userId, new PollParticipationFilter { Page = 1, PerPage = 10 });

        Assert.Equal(2, result.Total);
        Assert.All(result.Items, participation => Assert.Equal(userId, participation.UserId));
        // Ulids minted in the same millisecond are ordered by their random tail, so the
        // expected ids are sorted alongside the actual ones rather than listed as created.
        Assert.Equal(
            new Ulid?[] { pollA.Id, pollC.Id }.Order(),
            result.Items.Select(participation => participation.PollId).Order()
        );
        Assert.All(result.Items, participation => Assert.NotNull(participation.Poll));
    }

    [Fact]
    public async Task GetUserParticipations_Respects_Pagination()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(dbName);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var pollRepo = new PollRepository(context);
        var pollParticipationRepo = new PollParticipationRepository(context);
        var pollAnswerRepo = new PollAnswerRepository(context);
        var mediaRepo = new MediaRepository(context);
        var jsonPatch = new JsonPatchUpdateService(mapper);

        var userId = Ulid.NewUlid();

        for (var i = 0; i < 3; i++)
        {
            var poll = new PollModel { Title = $"P{i}", IsActive = true, IsAuthRequired = false };
            context.Add(poll);
            await context.SaveChangesAsync();
            context.Add(new PollParticipationModel
            {
                PollId = poll.Id,
                UserId = userId,
                UserType = ParticipatingUserType.AuthenticatedUser
            });
            await context.SaveChangesAsync();
        }

        var service = new PollService(mapper, pollRepo, pollParticipationRepo, pollAnswerRepo, mediaRepo, new TestCurrentUser(userId, UserRoles.Student), jsonPatch);

        var page = await service.GetUserParticipationsAsync(userId, new PollParticipationFilter { Page = 1, PerPage = 2 });

        Assert.Equal(3, page.Total);
        Assert.Equal(2, page.Items.Count());
    }

    [Fact]
    public async Task GetUserParticipations_Forbids_Reading_Another_Users_Participations()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(dbName);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var pollRepo = new PollRepository(context);
        var pollParticipationRepo = new PollParticipationRepository(context);
        var pollAnswerRepo = new PollAnswerRepository(context);
        var mediaRepo = new MediaRepository(context);
        var jsonPatch = new JsonPatchUpdateService(mapper);

        var userId = Ulid.NewUlid();
        var otherUserId = Ulid.NewUlid();
        var filter = new PollParticipationFilter { Page = 1, PerPage = 10 };

        var student = new PollService(mapper, pollRepo, pollParticipationRepo, pollAnswerRepo, mediaRepo, new TestCurrentUser(userId, UserRoles.Student), jsonPatch);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => student.GetUserParticipationsAsync(otherUserId, filter)
        );

        var teacher = new PollService(mapper, pollRepo, pollParticipationRepo, pollAnswerRepo, mediaRepo, new TestCurrentUser(userId, UserRoles.Teacher), jsonPatch);

        var result = await teacher.GetUserParticipationsAsync(otherUserId, filter);

        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task UpdatePollAnswer_Patches_Value()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(dbName);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var pollRepo = new PollRepository(context);
        var pollParticipationRepo = new PollParticipationRepository(context);
        var pollAnswerRepo = new PollAnswerRepository(context);
        var mediaRepo = new MediaRepository(context);
        var currentUser = new TestCurrentUser(null, UserRoles.Admin);
        var jsonPatch = new JsonPatchUpdateService(mapper);
        var service = new PollService(mapper, pollRepo, pollParticipationRepo, pollAnswerRepo, mediaRepo, currentUser, jsonPatch);

        // Seed question + answer
        var poll = new PollModel { Title = "P", IsActive = true, IsAuthRequired = false };
        var q = new PollQuestionModel { Poll = poll, Title = "Q", IsRequired = true, Type = PollQuestionType.Text, Order = 0 };
        var a = new PollAnswerModel { PollQuestion = q, Value = new PollAnswerValue { Type = PollQuestionType.Text, Value = "old" } };
        context.Add(a);
        await context.SaveChangesAsync();

        var patch = new JsonPatchDocument<UpdatePollAnswerDTO>();
        patch.Replace(x => x.Value, new PollAnswerValue { Type = PollQuestionType.Text, Value = "new" });

        await service.UpdatePollAnswerAsync(a.Id, patch);

        var again = await context.Set<PollAnswerModel>().FindAsync(a.Id);
        Assert.Equal("new", again!.Value.Value as string);
    }

    [Fact]
    public async Task GetPollParticipations_Loads_User_And_Searches_By_Participant()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(dbName);
        var mapper = CreateMapper();
        var pollRepo = new PollRepository(context);
        var pollParticipationRepo = new PollParticipationRepository(context);
        var pollAnswerRepo = new PollAnswerRepository(context);
        var mediaRepo = new MediaRepository(context);
        var jsonPatch = new JsonPatchUpdateService(mapper);

        var poll = new PollModel { Title = "P", IsActive = true, IsAuthRequired = false };
        var alice = new UserModel { Name = "Alice", Username = "alice", Email = "alice@noo.ru", PasswordHash = "x", Role = UserRoles.Student };
        var bob = new UserModel { Name = "Bob", Username = "bob", Email = "bob@noo.ru", PasswordHash = "x", Role = UserRoles.Student };
        context.AddRange(poll, alice, bob);
        await context.SaveChangesAsync();

        context.AddRange(
            new PollParticipationModel { PollId = poll.Id, UserId = alice.Id, UserType = ParticipatingUserType.AuthenticatedUser },
            new PollParticipationModel { PollId = poll.Id, UserId = bob.Id, UserType = ParticipatingUserType.AuthenticatedUser },
            new PollParticipationModel { PollId = poll.Id, UserType = ParticipatingUserType.TelegramUser, UserExternalIdentifier = "@carol" }
        );
        await context.SaveChangesAsync();

        var service = new PollService(mapper, pollRepo, pollParticipationRepo, pollAnswerRepo, mediaRepo, new TestCurrentUser(null, UserRoles.Admin), jsonPatch);

        var all = await service.GetPollParticipationsAsync(poll.Id, new PollParticipationFilter { Page = 1, PerPage = 10 });
        Assert.Equal(3, all.Total);
        Assert.Contains(all.Items, p => p.User?.Name == "Alice");

        var byName = await service.GetPollParticipationsAsync(poll.Id, new PollParticipationFilter { Page = 1, PerPage = 10, Search = "alic" });
        Assert.Equal(1, byName.Total);
        Assert.Equal(alice.Id, byName.Items.Single().UserId);

        var byEmail = await service.GetPollParticipationsAsync(poll.Id, new PollParticipationFilter { Page = 1, PerPage = 10, Search = "bob@noo" });
        Assert.Equal(1, byEmail.Total);
        Assert.Equal(bob.Id, byEmail.Items.Single().UserId);

        var byExternalIdentifier = await service.GetPollParticipationsAsync(poll.Id, new PollParticipationFilter { Page = 1, PerPage = 10, Search = "carol" });
        Assert.Equal(1, byExternalIdentifier.Total);
        Assert.Equal("@carol", byExternalIdentifier.Items.Single().UserExternalIdentifier);
    }

    [Fact]
    public async Task GetPollParticipation_Loads_Answers_And_User()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(dbName);
        var mapper = CreateMapper();
        var pollRepo = new PollRepository(context);
        var pollParticipationRepo = new PollParticipationRepository(context);
        var pollAnswerRepo = new PollAnswerRepository(context);
        var mediaRepo = new MediaRepository(context);
        var jsonPatch = new JsonPatchUpdateService(mapper);

        var poll = new PollModel { Title = "P", IsActive = true, IsAuthRequired = false };
        var question = new PollQuestionModel { Poll = poll, Title = "Q", IsRequired = true, Type = PollQuestionType.Text, Order = 0 };
        var user = new UserModel { Name = "Alice", Username = "alice", Email = "alice@noo.ru", PasswordHash = "x", Role = UserRoles.Student };
        context.AddRange(question, user);
        await context.SaveChangesAsync();

        var participation = new PollParticipationModel
        {
            PollId = poll.Id,
            UserId = user.Id,
            UserType = ParticipatingUserType.AuthenticatedUser,
            Answers =
            [
                new PollAnswerModel
                {
                    PollQuestionId = question.Id,
                    Value = new PollAnswerValue { Type = PollQuestionType.Text, Value = "hello" }
                }
            ]
        };
        context.Add(participation);
        await context.SaveChangesAsync();

        var service = new PollService(mapper, pollRepo, pollParticipationRepo, pollAnswerRepo, mediaRepo, new TestCurrentUser(null, UserRoles.Admin), jsonPatch);

        var result = await service.GetPollParticipationAsync(participation.Id);

        Assert.NotNull(result);
        Assert.Equal("Alice", result!.User?.Name);

        var answer = Assert.Single(result.Answers);
        Assert.Equal(question.Id, answer.PollQuestionId);
        // Round-tripped through the JSON converter, so the value comes back as a
        // JsonElement rather than the string it was stored as.
        Assert.Equal("hello", answer.Value.Value?.ToString());
    }

    [Fact]
    public async Task Participate_Stores_Answers_And_Attaches_Files()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(dbName);
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = CreateMapper();
        var pollRepo = new PollRepository(context);
        var pollParticipationRepo = new PollParticipationRepository(context);
        var pollAnswerRepo = new PollAnswerRepository(context);
        var mediaRepo = new MediaRepository(context);
        var jsonPatch = new JsonPatchUpdateService(mapper);

        var userId = Ulid.NewUlid();
        var (poll, textQuestion, filesQuestion) = SeedPollWithFileQuestion(context);
        var media = SeedAnswerFile(context, userId);
        await context.SaveChangesAsync();

        var service = new PollService(mapper, pollRepo, pollParticipationRepo, pollAnswerRepo, mediaRepo, new TestCurrentUser(userId), jsonPatch);

        await service.ParticipateAsync(poll.Id, new CreatePollParticipationDTO
        {
            UserType = ParticipatingUserType.AuthenticatedUser,
            Answers =
            [
                new CreatePollAnswerDTO
                {
                    PollQuestionId = textQuestion.Id,
                    Value = new PollAnswerValue { Type = PollQuestionType.Text, Value = "hello" }
                },
                new CreatePollAnswerDTO
                {
                    PollQuestionId = filesQuestion.Id,
                    Value = new PollAnswerValue { Type = PollQuestionType.Files, Value = null },
                    MediaIds = [media.Id]
                }
            ]
        });
        await uow.CommitAsync();

        var participation = await service.GetPollParticipationsAsync(poll.Id, new PollParticipationFilter { Page = 1, PerPage = 10 });
        var stored = await service.GetPollParticipationAsync(participation.Items.Single().Id);

        Assert.NotNull(stored);
        Assert.Equal(2, stored!.Answers.Count);

        var fileAnswer = stored.Answers.Single(answer => answer.PollQuestionId == filesQuestion.Id);
        Assert.Equal(PollQuestionType.Files, fileAnswer.Value.Type);
        Assert.Equal(media.Id, Assert.Single(fileAnswer.Medias!).Id);
    }

    [Fact]
    public async Task Participate_Rejects_Files_Of_Another_Owner()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(dbName);
        var mapper = CreateMapper();
        var pollRepo = new PollRepository(context);
        var pollParticipationRepo = new PollParticipationRepository(context);
        var pollAnswerRepo = new PollAnswerRepository(context);
        var mediaRepo = new MediaRepository(context);
        var jsonPatch = new JsonPatchUpdateService(mapper);

        var (poll, _, filesQuestion) = SeedPollWithFileQuestion(context);
        var media = SeedAnswerFile(context, Ulid.NewUlid());
        await context.SaveChangesAsync();

        var service = new PollService(mapper, pollRepo, pollParticipationRepo, pollAnswerRepo, mediaRepo, new TestCurrentUser(Ulid.NewUlid()), jsonPatch);

        await Assert.ThrowsAsync<InvalidPollAnswerException>(async () =>
        {
            await service.ParticipateAsync(poll.Id, new CreatePollParticipationDTO
            {
                UserType = ParticipatingUserType.AuthenticatedUser,
                Answers =
                [
                    new CreatePollAnswerDTO
                    {
                        PollQuestionId = filesQuestion.Id,
                        Value = new PollAnswerValue { Type = PollQuestionType.Files, Value = null },
                        MediaIds = [media.Id]
                    }
                ]
            });
        });
    }

    [Fact]
    public async Task Participate_Rejects_More_Files_Than_The_Question_Allows()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = TestHelpers.CreateInMemoryDb(dbName);
        var mapper = CreateMapper();
        var pollRepo = new PollRepository(context);
        var pollParticipationRepo = new PollParticipationRepository(context);
        var pollAnswerRepo = new PollAnswerRepository(context);
        var mediaRepo = new MediaRepository(context);
        var jsonPatch = new JsonPatchUpdateService(mapper);

        var userId = Ulid.NewUlid();
        var (poll, _, filesQuestion) = SeedPollWithFileQuestion(context);
        var first = SeedAnswerFile(context, userId);
        var second = SeedAnswerFile(context, userId);
        await context.SaveChangesAsync();

        var service = new PollService(mapper, pollRepo, pollParticipationRepo, pollAnswerRepo, mediaRepo, new TestCurrentUser(userId), jsonPatch);

        await Assert.ThrowsAsync<InvalidPollAnswerException>(async () =>
        {
            await service.ParticipateAsync(poll.Id, new CreatePollParticipationDTO
            {
                UserType = ParticipatingUserType.AuthenticatedUser,
                Answers =
                [
                    new CreatePollAnswerDTO
                    {
                        PollQuestionId = filesQuestion.Id,
                        Value = new PollAnswerValue { Type = PollQuestionType.Files, Value = null },
                        MediaIds = [first.Id, second.Id]
                    }
                ]
            });
        });
    }

    private static (PollModel Poll, PollQuestionModel TextQuestion, PollQuestionModel FilesQuestion) SeedPollWithFileQuestion(NooDbContext context)
    {
        var poll = new PollModel { Title = "P", IsActive = true, IsAuthRequired = false };
        var textQuestion = new PollQuestionModel { Poll = poll, Title = "Q1", Type = PollQuestionType.Text, Order = 0 };
        var filesQuestion = new PollQuestionModel
        {
            Poll = poll,
            Title = "Q2",
            Type = PollQuestionType.Files,
            Order = 1,
            Config = new PollQuestionConfig
            {
                Type = PollQuestionType.Files,
                MaxFileCount = 1,
                MaxFileSize = 1024,
                AllowedFileTypes = ["application/pdf"]
            }
        };

        context.AddRange(textQuestion, filesQuestion);

        return (poll, textQuestion, filesQuestion);
    }

    private static MediaModel SeedAnswerFile(NooDbContext context, Ulid ownerId)
    {
        var media = new MediaModel
        {
            Path = $"poll-answer-file/{ownerId}/{Ulid.NewUlid()}.pdf",
            Name = "answer.pdf",
            ActualName = "answer.pdf",
            Extension = "pdf",
            Size = 512,
            Category = MediaCategory.PollAnswerFile,
            Status = MediaStatus.Completed,
            OwnerId = ownerId
        };

        context.Add(media);

        return media;
    }
}
