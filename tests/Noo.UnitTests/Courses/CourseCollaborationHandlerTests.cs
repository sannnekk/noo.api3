using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AutoMapper;
using Moq;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Collaboration;
using Noo.Api.Courses.Collaboration;
using Noo.Api.Courses.DTO;
using Noo.Api.Courses.Services;
using Noo.Api.Subjects.Models;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.Courses;

/// <summary>
/// A course is the awkward case the room handler exists for: two entities on the wire with a
/// patch endpoint each, presented to everyone above as one flat draft.
/// </summary>
public class CourseCollaborationHandlerTests
{
    private sealed record Harness(
        CourseCollaborationHandler Handler,
        ICourseService Service,
        Func<Task> Commit,
        Ulid CourseId,
        Ulid ChapterId,
        Ulid MaterialId
    );

    private static ICurrentUser MakeUser(UserRoles role)
    {
        var mock = new Mock<ICurrentUser>();

        mock.SetupGet(m => m.UserId).Returns(Ulid.NewUlid());
        mock.SetupGet(m => m.UserRole).Returns(role);
        mock.SetupGet(m => m.IsAuthenticated).Returns(true);
        mock.Setup(m => m.IsInRole(It.IsAny<UserRoles[]>()))
            .Returns<UserRoles[]>(roles => roles.Contains(role));

        return mock.Object;
    }

    private static CourseService BuildService(
        NooDbContext context,
        IMapper mapper,
        ICurrentUser currentUser
    ) =>
        new(
            new CourseRepository(context),
            new CourseContentRepository(context),
            new CourseMaterialReactionRepository(context),
            currentUser,
            mapper,
            new JsonPatchUpdateService(mapper),
            new EntityReferenceFactory(context)
        );

    /// <summary>A course with one chapter holding one material, and no content anywhere.</summary>
    private static async Task<Harness> CreateAsync(UserRoles role = UserRoles.Teacher)
    {
        var context = TestHelpers.CreateInMemoryDb(Guid.NewGuid().ToString());
        var uow = TestHelpers.CreateUowMock(context).Object;
        var mapper = MapperTestUtils.CreateAppMapper();
        var currentUser = MakeUser(role);

        var subject = new SubjectModel { Name = "Математика", Color = "#00AAFF" };
        context.GetDbSet<SubjectModel>().Add(subject);
        await context.SaveChangesAsync();

        var service = BuildService(context, mapper, currentUser);

        var courseId = await service.CreateAsync(
            new CreateCourseDTO
            {
                Name = "Курс",
                SubjectId = subject.Id,
                Chapters =
                [
                    new CreateCourseChapterDTO
                    {
                        Order = 1,
                        Title = "Глава",
                        IsActive = true,
                        Materials =
                        [
                            new CreateCourseMaterialDTO
                            {
                                Order = 1,
                                Title = "Материал",
                                IsActive = true,
                            },
                        ],
                    },
                ],
            }
        );
        await uow.CommitAsync();

        var course = await service.GetByIdAsync(courseId, includeInactive: true);
        var chapter = course!.Chapters!.Single();
        var material = chapter.Materials!.Single();

        return new Harness(
            new CourseCollaborationHandler(service, currentUser),
            service,
            () => uow.CommitAsync(),
            courseId,
            chapter.Id,
            material.Id
        );
    }

    /// <summary>Rich text in the shape the wire converters bind, <c>$type</c> and all.</summary>
    private static JsonNode RichText(string text) =>
        JsonNode.Parse(
            $$"""
            {"$type":"tiptap","type":"doc","content":[
              {"type":"paragraph","content":[{"type":"text","text":"{{text}}"}]}]}
            """
        )!;

    private static CollaborationOp Replace(string path, JsonNode? value) =>
        new()
        {
            Op = "replace",
            Path = path,
            Value = value,
        };

    [Fact(DisplayName = "CourseCollaborationHandler: only paths the patch endpoints accept are editable")]
    public void IsPathAllowed_MatchesThePatchDocuments()
    {
        var handler = new CourseCollaborationHandler(null!, null!);
        var chapter = Ulid.NewUlid().ToString();
        var material = Ulid.NewUlid().ToString();

        Assert.True(handler.IsPathAllowed("/name"));
        Assert.True(handler.IsPathAllowed("/isPublic"));
        Assert.True(handler.IsPathAllowed($"/chapters/{chapter}"));
        Assert.True(handler.IsPathAllowed($"/chapters/{chapter}/title"));
        Assert.True(handler.IsPathAllowed($"/chapters/{chapter}/parentChapterId"));
        Assert.True(handler.IsPathAllowed($"/chapters/{chapter}/materials/{material}"));
        Assert.True(handler.IsPathAllowed($"/chapters/{chapter}/materials/{material}/title"));
        Assert.True(handler.IsPathAllowed($"/chapters/{chapter}/materials/{material}/content"));
        Assert.True(
            handler.IsPathAllowed($"/chapters/{chapter}/materials/{material}/content/content")
        );
        Assert.True(
            handler.IsPathAllowed($"/chapters/{chapter}/materials/{material}/content/pollId")
        );

        Assert.False(handler.IsPathAllowed("/id"));
        Assert.False(handler.IsPathAllowed("/chapters"));
        Assert.False(handler.IsPathAllowed("/notAField"));
        Assert.False(handler.IsPathAllowed("/chapters/0/title"));
        Assert.False(handler.IsPathAllowed($"/chapters/{chapter}/notAField"));
        Assert.False(handler.IsPathAllowed($"/chapters/{chapter}/materials/{material}/notAField"));
        Assert.False(
            handler.IsPathAllowed($"/chapters/{chapter}/materials/{material}/content/notAField")
        );
    }

    // Pointing one material at another's content would reach a row the caller may not be able to
    // open at all, so the server keeps that field for itself.
    [Fact(DisplayName = "CourseCollaborationHandler: a material's content id is not the client's to set")]
    public void IsPathAllowed_RefusesContentId()
    {
        var handler = new CourseCollaborationHandler(null!, null!);
        var chapter = Ulid.NewUlid().ToString();
        var material = Ulid.NewUlid().ToString();

        Assert.False(
            handler.IsPathAllowed($"/chapters/{chapter}/materials/{material}/contentId")
        );
    }

    [Fact(DisplayName = "CourseCollaborationHandler: the editable fields are the ones the DTOs declare")]
    public void IsPathAllowed_TracksTheDtos()
    {
        var handler = new CourseCollaborationHandler(null!, null!);
        var chapter = Ulid.NewUlid().ToString();
        var material = Ulid.NewUlid().ToString();

        foreach (var field in JsonFieldsOf<UpdateCourseDTO>().Except(["id", "chapters"]))
        {
            Assert.True(handler.IsPathAllowed($"/{field}"), field);
        }

        foreach (var field in JsonFieldsOf<UpdateCourseChapterDTO>().Except(["id", "materials"]))
        {
            Assert.True(handler.IsPathAllowed($"/chapters/{chapter}/{field}"), field);
        }

        foreach (var field in JsonFieldsOf<UpdateCourseMaterialDTO>().Except(["id", "contentId"]))
        {
            Assert.True(
                handler.IsPathAllowed($"/chapters/{chapter}/materials/{material}/{field}"),
                field
            );
        }

        foreach (var field in JsonFieldsOf<UpdateCourseMaterialContentDTO>())
        {
            Assert.True(
                handler.IsPathAllowed(
                    $"/chapters/{chapter}/materials/{material}/content/{field}"
                ),
                field
            );
        }
    }

    private static IEnumerable<string> JsonFieldsOf<T>() =>
        typeof(T)
            .GetProperties()
            .Select(property => property.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name);

    [Fact(DisplayName = "CourseCollaborationHandler: saving a draft goes through the course's own patch pipeline")]
    public async Task SaveAsync_AppliesTheTree()
    {
        var harness = await CreateAsync();

        await harness.Handler.SaveAsync(
            harness.CourseId,
            [
                Replace("/name", JsonValue.Create("Новое название")),
                Replace($"/chapters/{harness.ChapterId}/title", JsonValue.Create("Новая глава")),
                Replace(
                    $"/chapters/{harness.ChapterId}/materials/{harness.MaterialId}/title",
                    JsonValue.Create("Новый материал")
                ),
            ]
        );
        await harness.Commit();

        var saved = await harness.Service.GetByIdAsync(harness.CourseId, includeInactive: true);
        var chapter = saved!.Chapters!.Single();

        Assert.Equal("Новое название", saved.Name);
        Assert.Equal("Новая глава", chapter.Title);
        Assert.Equal("Новый материал", chapter.Materials!.Single().Title);
    }

    // The content row does not exist until somebody types into the material, so the save is what
    // brings it into being — the client never has to create it first and thread the id back.
    [Fact(DisplayName = "CourseCollaborationHandler: writing a material's text creates the content it needs")]
    public async Task SaveAsync_CreatesMissingContent()
    {
        var harness = await CreateAsync();

        await harness.Handler.SaveAsync(
            harness.CourseId,
            [
                Replace(
                    $"/chapters/{harness.ChapterId}/materials/{harness.MaterialId}/content/content",
                    RichText("Текст материала")
                ),
            ]
        );
        await harness.Commit();

        var saved = await harness.Service.GetByIdAsync(harness.CourseId, includeInactive: true);
        var material = saved!.Chapters!.Single().Materials!.Single();

        Assert.NotNull(material.ContentId);

        var content = await harness.Service.GetContentByIdAsync(material.ContentId!.Value);

        Assert.NotNull(content);
    }

    // Both halves of the document in one draft, applied in the order that lets a material drafted
    // in this same session own content by the end of it.
    [Fact(DisplayName = "CourseCollaborationHandler: a material drafted and written to in one session saves whole")]
    public async Task SaveAsync_CreatesAMaterialAndItsContentTogether()
    {
        var harness = await CreateAsync();
        var newMaterialId = Ulid.NewUlid();

        await harness.Handler.SaveAsync(
            harness.CourseId,
            [
                new CollaborationOp
                {
                    Op = "add",
                    Path = $"/chapters/{harness.ChapterId}/materials/{newMaterialId}",
                    Value = JsonNode.Parse(
                        $$"""
                        {"id":"{{newMaterialId}}","order":2,"title":"Второй","isActive":true,
                         "isPinned":false}
                        """
                    ),
                },
                Replace(
                    $"/chapters/{harness.ChapterId}/materials/{newMaterialId}/content/content",
                    RichText("Свежий текст")
                ),
            ]
        );
        await harness.Commit();

        var saved = await harness.Service.GetByIdAsync(harness.CourseId, includeInactive: true);
        var material = saved!.Chapters!.Single().Materials!.Single(m => m.Id == newMaterialId);

        Assert.Equal("Второй", material.Title);
        Assert.NotNull(material.ContentId);
    }

    // Room scoping is structural rather than a check somebody has to remember: the content half is
    // resolved through the course the room names, so a material of another course is simply not
    // there to be written.
    [Fact(DisplayName = "CourseCollaborationHandler: an operation cannot reach a material of another course")]
    public async Task SaveAsync_CannotReachAMaterialOutsideTheRoom()
    {
        var harness = await CreateAsync();
        var other = await CreateAsync();

        await harness.Handler.SaveAsync(
            harness.CourseId,
            [
                Replace(
                    $"/chapters/{other.ChapterId}/materials/{other.MaterialId}/content/content",
                    RichText("Чужое")
                ),
            ]
        );
        await harness.Commit();

        var untouched = await other.Service.GetByIdAsync(other.CourseId, includeInactive: true);

        Assert.Null(untouched!.Chapters!.Single().Materials!.Single().ContentId);
    }

    // The foreign key lives on the material, so EF has no cascade to follow: without this the row
    // would simply be left behind, which is what used to happen whenever a client forgot to ask.
    [Fact(DisplayName = "CourseCollaborationHandler: removing a material takes its content with it")]
    public async Task SaveAsync_RemovesTheContentOfARemovedMaterial()
    {
        var harness = await CreateAsync();
        var materialPath = $"/chapters/{harness.ChapterId}/materials/{harness.MaterialId}";

        await harness.Handler.SaveAsync(
            harness.CourseId,
            [Replace($"{materialPath}/content/content", RichText("Текст"))]
        );
        await harness.Commit();

        var withContent = await harness.Service.GetByIdAsync(
            harness.CourseId,
            includeInactive: true
        );
        var contentId = withContent!.Chapters!.Single().Materials!.Single().ContentId!.Value;

        Assert.NotNull(await harness.Service.GetContentByIdAsync(contentId));

        await harness.Handler.SaveAsync(
            harness.CourseId,
            [new CollaborationOp { Op = "remove", Path = materialPath }]
        );
        await harness.Commit();

        Assert.Null(await harness.Service.GetContentByIdAsync(contentId));
    }

    // A material can be removed by one editor while another is still typing into it, so the draft
    // outlives the material. That must cost the operation, never the whole save.
    [Fact(DisplayName = "CourseCollaborationHandler: content for a removed material does not block the save")]
    public async Task SaveAsync_SkipsContentForARemovedMaterial()
    {
        var harness = await CreateAsync();
        var materialPath = $"/chapters/{harness.ChapterId}/materials/{harness.MaterialId}";

        await harness.Handler.SaveAsync(
            harness.CourseId,
            [
                Replace($"{materialPath}/content/content", RichText("Напрасно")),
                new CollaborationOp { Op = "remove", Path = materialPath },
                Replace("/name", JsonValue.Create("Всё же сохранено")),
            ]
        );
        await harness.Commit();

        var saved = await harness.Service.GetByIdAsync(harness.CourseId, includeInactive: true);

        Assert.Equal("Всё же сохранено", saved!.Name);
        Assert.Empty(saved.Chapters!.Single().Materials ?? []);
    }

    [Fact(DisplayName = "CourseCollaborationHandler: compaction keeps the last write to each path")]
    public async Task CompactAsync_DropsWhatLaterOperationsSettle()
    {
        var harness = await CreateAsync();
        var materialPath = $"/chapters/{harness.ChapterId}/materials/{harness.MaterialId}";

        List<CollaborationOp> ops =
        [
            Replace("/name", JsonValue.Create("Первое")),
            Replace("/name", JsonValue.Create("Второе")),
            Replace($"{materialPath}/title", JsonValue.Create("A")),
            Replace($"{materialPath}/content/pollId", JsonValue.Create("x")),
            // Replacing the whole material settles the title above it.
            Replace($"{materialPath}", JsonValue.Create("whole")),
        ];

        var compacted = await harness.Handler.CompactAsync(harness.CourseId, ops);

        Assert.Equal(
            ["/name", materialPath, $"{materialPath}/content/pollId"],
            compacted.Select(op => op.Path)
        );
        Assert.Equal("Второе", compacted[0].Value!.GetValue<string>());
    }

    // The two entities a course is made of are separate documents: replacing a material rewrites
    // its title and its place in the tree, and says nothing about the content row it points at.
    [Fact(DisplayName = "CourseCollaborationHandler: replacing a material does not settle its content")]
    public async Task CompactAsync_KeepsContentAcrossAMaterialReplace()
    {
        var harness = await CreateAsync();
        var materialPath = $"/chapters/{harness.ChapterId}/materials/{harness.MaterialId}";

        var compacted = await harness.Handler.CompactAsync(
            harness.CourseId,
            [
                Replace($"{materialPath}/content/content", RichText("Текст")),
                Replace(materialPath, JsonValue.Create("whole")),
            ]
        );

        Assert.Contains(compacted, op => op.Path == $"{materialPath}/content/content");
    }

    [Theory(DisplayName = "CourseCollaborationHandler: only the roles the patch endpoint admits may edit")]
    [InlineData(UserRoles.Admin, true)]
    [InlineData(UserRoles.Teacher, true)]
    [InlineData(UserRoles.Mentor, false)]
    [InlineData(UserRoles.Student, false)]
    public async Task CanEditAsync_ChecksTheRole(UserRoles role, bool allowed)
    {
        var harness = await CreateAsync(role);

        Assert.Equal(allowed, await harness.Handler.CanEditAsync(harness.CourseId));
    }

    [Fact(DisplayName = "CourseCollaborationHandler: a course that does not exist cannot be opened")]
    public async Task CanEditAsync_RequiresTheCourseToExist()
    {
        var harness = await CreateAsync();

        Assert.True(await harness.Handler.CanEditAsync(harness.CourseId));
        Assert.False(await harness.Handler.CanEditAsync(Ulid.NewUlid()));
    }
}
