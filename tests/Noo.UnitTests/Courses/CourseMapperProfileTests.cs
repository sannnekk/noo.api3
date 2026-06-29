using AutoMapper;
using Noo.Api.Courses.DTO;
using Noo.Api.Courses.Models;
using Noo.Api.Media.Models;
using Noo.Api.NooTube.Models;
using Noo.Api.Polls.Models;
using Noo.Api.Subjects.Models;
using Noo.Api.Users.Models;
using Noo.Api.Works.Models;
using Noo.UnitTests.Common;
using Noo.Api.Core.Request.Patching;
using SystemTextJsonPatch;

namespace Noo.UnitTests.Courses;

public class CourseMapperProfileTests
{
    private static MapperConfiguration CreateConfiguration()
        => MapperTestUtils.CreateMapperConfig(cfg =>
        {
            cfg.AddProfile(new CourseMapperProfile());
            cfg.AddProfile(new NooTubeMapperProfile());
            cfg.AddProfile(new MediaMapperProfile());
            cfg.AddProfile(new PollMapperProfile());
            cfg.AddProfile(new SubjectMapperProfile());
            cfg.AddProfile(new UserMapperProfile());
            cfg.AddProfile(new WorkMapperProfile());
        });

    [Fact]
    public void CourseProfile_Config_Valid()
    {
        var cfg = CreateConfiguration();
        cfg.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_CreateCourse_To_Model_Maps_Fields()
    {
        var mapper = CreateConfiguration().CreateMapper();
        var dto = new CreateCourseDTO
        {
            Name = "Test",
            SubjectId = Ulid.NewUlid(),
            Description = "desc"
        };
        var model = mapper.Map<CourseModel>(dto);
        Assert.Equal(dto.Name, model.Name);
        Assert.Equal(dto.SubjectId, model.SubjectId);
        Assert.Equal(dto.Description, model.Description);
    }

    [Fact]
    public void Map_CreateMembership_To_Model_Maps_Fields()
    {
        var mapper = CreateConfiguration().CreateMapper();
        var dto = new CreateCourseMembershipDTO
        {
            CourseId = Ulid.NewUlid(),
            StudentId = Ulid.NewUlid()
        };
        var model = mapper.Map<CourseMembershipModel>(dto);
        Assert.Equal(dto.CourseId, model.CourseId);
        Assert.Equal(dto.StudentId, model.StudentId);
    }

    [Fact]
    public void Map_Course_To_Dto_Maps_Subject()
    {
        var mapper = CreateConfiguration().CreateMapper();
        var subject = new SubjectModel
        {
            Name = "Math",
            Color = "#ffffff"
        };
        var model = new CourseModel
        {
            Name = "Test",
            SubjectId = subject.Id,
            Subject = subject
        };

        var dto = mapper.Map<CourseDTO>(model);

        Assert.NotNull(dto.Subject);
        Assert.Equal(subject.Id, dto.Subject!.Id);
        Assert.Equal(subject.Name, dto.Subject.Name);
    }

    // ----------------------------------------------------------------------
    // Patch round-trip regression tests for every nested dictionary->collection
    // mapping in this profile. Each test seeds a parent with one existing child
    // (a known reference), runs the same Model -> DTO -> patch -> Map(DTO, Model)
    // flow as JsonPatchUpdateService, then asserts the existing child:
    //   1) is the SAME instance (so EF still tracks it as Unchanged),
    //   2) kept its original Id, and
    //   3) kept its content.
    // The new child must also land with the Id from the dict key.
    //
    // Bug being guarded against: assigning a fresh list of fresh children would
    // make EF orphan the originals (and, with cascade FKs, corrupt related rows
    // such as assigned_work_answer).
    // ----------------------------------------------------------------------

    private static CourseModel BuildCourse(Ulid existingChapterId, string chapterTitle = "Existing Chapter")
        => new()
        {
            Id = Ulid.NewUlid(),
            Name = "C",
            SubjectId = Ulid.NewUlid(),
            Chapters = new List<CourseChapterModel>
            {
                new()
                {
                    Id = existingChapterId,
                    Title = chapterTitle,
                    Order = 0,
                    IsActive = true,
                }
            }
        };

    [Fact(DisplayName = "Course mapper: PATCH adds a chapter, preserves existing chapter identity")]
    public void Patch_Add_Chapter_Preserves_Existing()
    {
        var mapper = CreateConfiguration().CreateMapper();
        var existingChapterId = Ulid.NewUlid();
        var model = BuildCourse(existingChapterId);
        var existingChapterRef = model.Chapters!.First();

        var dto = mapper.Map<UpdateCourseDTO>(model);

        var newChapterId = Ulid.NewUlid();
        dto.Chapters![newChapterId.ToString()] = new UpdateCourseChapterDTO
        {
            Id = newChapterId,
            Title = "New Chapter",
            Order = 1,
            IsActive = true,
        };

        mapper.Map(dto, model);

        Assert.Equal(2, model.Chapters!.Count);
        var kept = model.Chapters!.Single(c => c.Id == existingChapterId);
        Assert.Same(existingChapterRef, kept);
        Assert.Equal("Existing Chapter", kept.Title);

        Assert.Contains(model.Chapters!, c => c.Id == newChapterId);
    }

    [Fact(DisplayName = "Course mapper: PATCH adds a sub-chapter (flat), preserves existing sub-chapter identity")]
    public void Patch_Add_SubChapter_Preserves_Existing()
    {
        var mapper = CreateConfiguration().CreateMapper();
        var parentId = Ulid.NewUlid();
        var existingSubChapterId = Ulid.NewUlid();

        // The update load is flat: every chapter (root or nested) is a top-level entry in
        // CourseModel.Chapters, with parentage expressed via ParentChapterId.
        var model = new CourseModel
        {
            Id = Ulid.NewUlid(),
            Name = "C",
            SubjectId = Ulid.NewUlid(),
            Chapters = new List<CourseChapterModel>
            {
                new()
                {
                    Id = parentId,
                    Title = "Parent",
                    Order = 0,
                    IsActive = true,
                },
                new()
                {
                    Id = existingSubChapterId,
                    Title = "Existing Sub",
                    Order = 0,
                    IsActive = true,
                    ParentChapterId = parentId,
                },
            }
        };
        var existingSubRef = model.Chapters!.Single(c => c.Id == existingSubChapterId);

        var dto = mapper.Map<UpdateCourseDTO>(model);
        Assert.Equal(2, dto.Chapters!.Count);

        var newSubId = Ulid.NewUlid();
        dto.Chapters![newSubId.ToString()] = new UpdateCourseChapterDTO
        {
            Id = newSubId,
            Title = "New Sub",
            Order = 1,
            IsActive = true,
            ParentChapterId = parentId,
        };

        mapper.Map(dto, model);

        Assert.Equal(3, model.Chapters!.Count);
        var keptSub = model.Chapters!.Single(c => c.Id == existingSubChapterId);
        Assert.Same(existingSubRef, keptSub);
        Assert.Equal("Existing Sub", keptSub.Title);
        Assert.Equal(parentId, keptSub.ParentChapterId);

        var newSub = model.Chapters!.Single(c => c.Id == newSubId);
        Assert.Equal(parentId, newSub.ParentChapterId);
    }

    [Fact(DisplayName = "Course mapper: PATCH adds a material, preserves existing material identity")]
    public void Patch_Add_Material_Preserves_Existing()
    {
        var mapper = CreateConfiguration().CreateMapper();
        var existingMaterialId = Ulid.NewUlid();

        var chapter = new CourseChapterModel
        {
            Id = Ulid.NewUlid(),
            Title = "Parent",
            Order = 0,
            IsActive = true,
            Materials = new List<CourseMaterialModel>
            {
                new()
                {
                    Id = existingMaterialId,
                    Title = "Existing Material",
                    Order = 0,
                    IsActive = true,
                }
            }
        };
        var model = new CourseModel
        {
            Id = Ulid.NewUlid(),
            Name = "C",
            SubjectId = Ulid.NewUlid(),
            Chapters = new List<CourseChapterModel> { chapter },
        };
        var existingMaterialRef = chapter.Materials!.First();

        var dto = mapper.Map<UpdateCourseDTO>(model);
        var parentDto = dto.Chapters!.Values.Single();

        var newMaterialId = Ulid.NewUlid();
        parentDto.Materials![newMaterialId.ToString()] = new UpdateCourseMaterialDTO
        {
            Id = newMaterialId,
            Title = "New Material",
            Order = 1,
            IsActive = true,
        };

        mapper.Map(dto, model);

        var chapterAfter = model.Chapters!.Single();
        Assert.Equal(2, chapterAfter.Materials!.Count);
        var keptMaterial = chapterAfter.Materials!.Single(m => m.Id == existingMaterialId);
        Assert.Same(existingMaterialRef, keptMaterial);
        Assert.Equal("Existing Material", keptMaterial.Title);
        Assert.Contains(chapterAfter.Materials!, m => m.Id == newMaterialId);
    }

    [Fact(DisplayName = "Course mapper: PATCH moving a material to another chapter reuses the same instance")]
    public void Patch_Move_Material_Between_Chapters_Reuses_Instance()
    {
        var mapper = CreateConfiguration().CreateMapper();
        var chapterAId = Ulid.NewUlid();
        var chapterBId = Ulid.NewUlid();
        var materialId = Ulid.NewUlid();

        var movable = new CourseMaterialModel
        {
            Id = materialId,
            Title = "Movable",
            Order = 0,
            IsActive = true,
            ChapterId = chapterAId,
        };
        var model = new CourseModel
        {
            Id = Ulid.NewUlid(),
            Name = "C",
            SubjectId = Ulid.NewUlid(),
            Chapters = new List<CourseChapterModel>
            {
                new()
                {
                    Id = chapterAId,
                    Title = "A",
                    Order = 0,
                    IsActive = true,
                    Materials = new List<CourseMaterialModel> { movable },
                },
                new()
                {
                    Id = chapterBId,
                    Title = "B",
                    Order = 1,
                    IsActive = true,
                    Materials = new List<CourseMaterialModel>(),
                },
            }
        };

        var dto = mapper.Map<UpdateCourseDTO>(model);
        var materialDto = dto.Chapters![chapterAId.ToString()].Materials![materialId.ToString()];
        dto.Chapters![chapterAId.ToString()].Materials!.Remove(materialId.ToString());
        dto.Chapters![chapterBId.ToString()].Materials![materialId.ToString()] = materialDto;

        mapper.Map(dto, model);

        var chapterA = model.Chapters!.Single(c => c.Id == chapterAId);
        var chapterB = model.Chapters!.Single(c => c.Id == chapterBId);
        Assert.Empty(chapterA.Materials!);
        var moved = Assert.Single(chapterB.Materials!);
        // The same tracked instance must move (identity resolved course-wide), otherwise EF
        // would track two CourseMaterialModel rows with the same Id and reject SaveChanges.
        Assert.Same(movable, moved);
        Assert.Equal(chapterBId, moved.ChapterId);
    }

    [Fact(DisplayName = "Course mapper: PATCH adds a work-assignment, preserves existing work-assignment identity")]
    public void Patch_Add_WorkAssignment_Preserves_Existing()
    {
        var mapper = CreateConfiguration().CreateMapper();
        var existingAssignmentId = Ulid.NewUlid();

        var model = new CourseMaterialContentModel
        {
            Id = Ulid.NewUlid(),
            WorkAssignments = new List<CourseWorkAssignmentModel>
            {
                new()
                {
                    Id = existingAssignmentId,
                    Order = 0,
                    IsActive = true,
                    WorkId = Ulid.NewUlid(),
                    Note = "Existing",
                }
            }
        };
        var existingAssignmentRef = model.WorkAssignments!.First();

        var dto = mapper.Map<UpdateCourseMaterialContentDTO>(model);

        var newAssignmentId = Ulid.NewUlid();
        dto.WorkAssignments![newAssignmentId.ToString()] = new UpdateCourseWorkAssignmentDTO
        {
            Order = 1,
            IsActive = true,
            WorkId = Ulid.NewUlid(),
            Note = "New",
        };

        mapper.Map(dto, model);

        Assert.Equal(2, model.WorkAssignments!.Count);
        var keptAssignment = model.WorkAssignments!.Single(a => a.Id == existingAssignmentId);
        Assert.Same(existingAssignmentRef, keptAssignment);
        Assert.Equal("Existing", keptAssignment.Note);
        Assert.Contains(model.WorkAssignments!, a => a.Id == newAssignmentId);
    }

    [Fact(DisplayName = "Course mapper: PATCH that removes a chapter drops it from the collection")]
    public void Patch_Remove_Chapter_Drops_It()
    {
        var mapper = CreateConfiguration().CreateMapper();
        var keepId = Ulid.NewUlid();
        var dropId = Ulid.NewUlid();

        var model = new CourseModel
        {
            Id = Ulid.NewUlid(),
            Name = "C",
            SubjectId = Ulid.NewUlid(),
            Chapters = new List<CourseChapterModel>
            {
                new() { Id = keepId, Title = "K", Order = 0, IsActive = true },
                new() { Id = dropId, Title = "D", Order = 1, IsActive = true },
            }
        };

        var dto = mapper.Map<UpdateCourseDTO>(model);
        dto.Chapters!.Remove(dropId.ToString());

        mapper.Map(dto, model);

        Assert.Single(model.Chapters!);
        Assert.Equal(keepId, model.Chapters!.Single().Id);
    }

    [Fact(DisplayName = "Course mapper: PATCH via JsonPatchUpdateService — adding a chapter preserves existing rows")]
    public void Patch_End_To_End_Add_Chapter_Preserves_Existing()
    {
        var mapper = CreateConfiguration().CreateMapper();
        var patchService = new JsonPatchUpdateService(mapper);

        var existingChapterId = Ulid.NewUlid();
        var model = BuildCourse(existingChapterId);
        var existingRef = model.Chapters!.First();

        var newChapterId = Ulid.NewUlid();
        var patch = new JsonPatchDocument<UpdateCourseDTO>();
        patch.Add(
            x => x.Chapters![newChapterId.ToString()],
            new UpdateCourseChapterDTO
            {
                Id = newChapterId,
                Title = "Brand New",
                Order = 1,
                IsActive = true,
            });

        patchService.ApplyPatch(model, patch);

        Assert.Equal(2, model.Chapters!.Count);
        var kept = model.Chapters!.Single(c => c.Id == existingChapterId);
        Assert.Same(existingRef, kept);
        Assert.Equal("Existing Chapter", kept.Title);
    }
}
