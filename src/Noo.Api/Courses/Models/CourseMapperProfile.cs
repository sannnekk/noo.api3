using AutoMapper;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.Request;
using Noo.Api.Core.Utils.AutoMapper;
using Noo.Api.Courses.DTO;
using Noo.Api.Courses.Types;

namespace Noo.Api.Courses.Models;

[AutoMapperProfile]
public class CourseMapperProfile : Profile
{
    public CourseMapperProfile()
    {
        // Course
        CreateMap<CreateCourseDTO, CourseModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Editors, opt => opt.Ignore())
            .ForMember(dest => dest.Authors, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.IsArchived, opt => opt.Ignore())
            .ForMember(dest => dest.Memberships, opt => opt.Ignore())
            .ForMember(dest => dest.Thumbnail, opt => opt.Ignore())
            .ForMember(dest => dest.Subject, opt => opt.Ignore())
            .AfterMap((_, dest) => ApplyCourseHierarchy(dest));

        CreateMap<CourseModel, UpdateCourseDTO>()
            .ForMember(
                dest => dest.AuthorIds,
                opt => opt.MapFrom(src => src.Authors.Select(author => author.Id))
            )
            .ForMember(
                dest => dest.Chapters,
                opt =>
                    opt.MapFrom(
                        (src, _, _, context) =>
                            src.Chapters.MapCollectionToDictionary<
                                CourseChapterModel,
                                UpdateCourseChapterDTO
                            >(context)
                    )
            );

        // Chapters are handled in AfterMap so AutoMapper's default collection mapper
        // (which calls dest.Chapters.Clear() before re-adding) never touches the EF-tracked
        // collection — that Clear would orphan existing rows under cascade FKs and
        // delete them despite the merge re-adding them.
        //
        // The update chapter tree is flat: src.Chapters holds EVERY chapter keyed by Id
        // (roots and descendants alike), so it matches the tracked CourseModel.Chapters
        // collection one-to-one. The Id-keyed merge therefore reuses existing instances,
        // adds new ones, and only drops chapters the client genuinely removed — no
        // descendant is ever silently orphaned. Tree position comes from each chapter's
        // ParentChapterId (mapped as a plain scalar), not from collection nesting.
        CreateMap<UpdateCourseDTO, CourseModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            // TODO: implement editors update
            .ForMember(dest => dest.Editors, opt => opt.Ignore())
            // Authors are resolved to tracked references in CourseService (the mapper
            // has no DbContext), so EF can reconcile the join table on save.
            .ForMember(dest => dest.Authors, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Memberships, opt => opt.Ignore())
            .ForMember(dest => dest.Thumbnail, opt => opt.Ignore())
            .ForMember(dest => dest.Subject, opt => opt.Ignore())
            .ForMember(dest => dest.Chapters, opt => opt.Ignore())
            .AfterMap((src, dest, context) => MergeCourseGraph(src, dest, context.Mapper));

        CreateMap<CourseModel, CourseDTO>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.ThumbnailId, opt => opt.MapFrom(src => src.ThumbnailId))
            .ForMember(dest => dest.Thumbnail, opt => opt.MapFrom(src => src.Thumbnail))
            // TODO: implement
            //.ForMember(dest => dest.MemberCount, opt => opt.MapFrom<CourseMemberCountValueResolver>())
            // TODO: remove
            .ForMember(dest => dest.MemberCount, opt => opt.Ignore())
            .MaxDepth(CourseConfig.MaxChapterTreeDepth);

        // Chapter
        CreateMap<CreateCourseChapterDTO, CourseChapterModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Course, opt => opt.Ignore())
            .ForMember(dest => dest.CourseId, opt => opt.Ignore())
            .ForMember(dest => dest.ParentChapter, opt => opt.Ignore())
            .ForMember(dest => dest.ParentChapterId, opt => opt.Ignore());

        CreateMap<CourseChapterModel, UpdateCourseChapterDTO>()
            .ForMember(
                dest => dest.Materials,
                opt =>
                    opt.MapFrom(
                        (src, _, _, context) =>
                            src.Materials.MapCollectionToDictionary<
                                CourseMaterialModel,
                                UpdateCourseMaterialDTO
                            >(context)
                    )
            );

        // Only chapter scalars (including the ParentChapterId tree edge) are mapped here.
        // Materials are merged at the course level (see MergeCourseGraph) because a material
        // can move between chapters, so its identity must be resolved across the whole course
        // rather than per chapter — a per-chapter merge would re-create the moved material under
        // its new chapter and make EF track two instances with the same Id. SubChapters are not a
        // nested collection: the tree is flat and parentage is the ParentChapterId scalar.
        CreateMap<UpdateCourseChapterDTO, CourseChapterModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Course, opt => opt.Ignore())
            .ForMember(dest => dest.CourseId, opt => opt.Ignore())
            .ForMember(dest => dest.ParentChapter, opt => opt.Ignore())
            .ForMember(dest => dest.SubChapters, opt => opt.Ignore())
            .ForMember(dest => dest.Materials, opt => opt.Ignore());

        CreateMap<CourseChapterModel, CourseChapterDTO>()
            .MaxDepth(CourseConfig.MaxChapterTreeDepth);

        // Material
        CreateMap<CourseMaterialModel, UpdateCourseMaterialDTO>();

        CreateMap<UpdateCourseMaterialDTO, CourseMaterialModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Chapter, opt => opt.Ignore())
            .ForMember(dest => dest.Content, opt => opt.Ignore())
            .ForMember(dest => dest.Reactions, opt => opt.Ignore());

        CreateMap<CreateCourseMaterialDTO, CourseMaterialModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Chapter, opt => opt.Ignore())
            .ForMember(dest => dest.ChapterId, opt => opt.Ignore())
            .ForMember(dest => dest.Content, opt => opt.Ignore())
            .ForMember(dest => dest.ContentId, opt => opt.Ignore())
            .ForMember(dest => dest.Reactions, opt => opt.Ignore());

        CreateMap<CourseMaterialModel, CourseMaterialDTO>()
            .ForMember(
                dest => dest.MyReaction,
                opt =>
                    opt.MapFrom(src =>
                        src.Reactions == null
                            ? null
                            : src.Reactions
                                .Select(r => (CourseMaterialReactionTypes?)r.Reaction)
                                .FirstOrDefault()
                    )
            );

        // Course material content
        // Navigation collections (Medias, NooTubeVideos) and the Poll nav are
        // resolved in CourseService via IEntityReferenceFactory so EF tracks
        // them as Unchanged references rather than as new entities. PollId is a
        // plain FK column and maps automatically.
        CreateMap<CreateCourseMaterialContentDTO, CourseMaterialContentModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Material, opt => opt.Ignore())
            .ForMember(dest => dest.Poll, opt => opt.Ignore())
            .ForMember(dest => dest.NooTubeVideos, opt => opt.Ignore())
            .ForMember(dest => dest.Medias, opt => opt.Ignore());

        CreateMap<CourseMaterialContentModel, UpdateCourseMaterialContentDTO>()
            .ForMember(
                dest => dest.NooTubeVideos,
                opt => opt.MapFrom(src => MapEntitiesToIdReferenceDictionary(src.NooTubeVideos))
            )
            .ForMember(
                dest => dest.Medias,
                opt => opt.MapFrom(src => MapEntitiesToIdReferenceDictionary(src.Medias))
            )
            .ForMember(
                dest => dest.WorkAssignments,
                opt =>
                    opt.MapFrom(
                        (src, _, _, context) =>
                            src.WorkAssignments.MapCollectionToDictionary<
                                CourseWorkAssignmentModel,
                                UpdateCourseWorkAssignmentDTO
                            >(context)
                    )
            );

        // WorkAssignments are handled in AfterMap to avoid AutoMapper's Clear-based
        // collection mapping (see comment on UpdateCourseDTO -> CourseModel).
        CreateMap<UpdateCourseMaterialContentDTO, CourseMaterialContentModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.NooTubeVideos, opt => opt.Ignore())
            .ForMember(dest => dest.Medias, opt => opt.Ignore())
            .ForMember(dest => dest.Poll, opt => opt.Ignore())
            .ForMember(dest => dest.Material, opt => opt.Ignore())
            .ForMember(dest => dest.WorkAssignments, opt => opt.Ignore())
            .AfterMap(
                (src, dest, context) =>
                {
                    if (src.WorkAssignments != null)
                    {
                        dest.WorkAssignments = src.WorkAssignments.MapDictionaryToCollection<
                            UpdateCourseWorkAssignmentDTO,
                            CourseWorkAssignmentModel
                        >(dest.WorkAssignments, context.Mapper);
                    }
                }
            );

        CreateMap<CourseMaterialContentModel, CourseMaterialContentDTO>();

        // Course membership
        CreateMap<CourseMembershipModel, CourseMembershipDTO>();

        CreateMap<CreateCourseMembershipDTO, CourseMembershipModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Assigner, opt => opt.Ignore())
            .ForMember(dest => dest.Course, opt => opt.Ignore())
            .ForMember(dest => dest.Student, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.IsArchived, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.IsArchivedByStudent, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.IsPinnedByStudent, opt => opt.MapFrom(_ => false))
            .ForMember(
                dest => dest.Type,
                opt => opt.MapFrom(_ => CourseMembershipType.ManualAssigned)
            )
            .ForMember(dest => dest.AssignerId, opt => opt.Ignore());

        // Course work assignment
        CreateMap<CreateCourseWorkAssignmentDTO, CourseWorkAssignmentModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CourseMaterialContentId, opt => opt.Ignore())
            .ForMember(dest => dest.CourseMaterialContent, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedWorks, opt => opt.Ignore())
            .ForMember(dest => dest.Work, opt => opt.Ignore());

        CreateMap<CourseWorkAssignmentModel, CourseWorkAssignmentDTO>();

        CreateMap<UpdateCourseWorkAssignmentDTO, CourseWorkAssignmentModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CourseMaterialContentId, opt => opt.Ignore())
            .ForMember(dest => dest.CourseMaterialContent, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedWorks, opt => opt.Ignore())
            .ForMember(dest => dest.Work, opt => opt.Ignore());

        CreateMap<CourseWorkAssignmentModel, UpdateCourseWorkAssignmentDTO>();
    }

    // Rebuilds the tracked course graph from a fully-patched UpdateCourseDTO. Chapters are a flat
    // Id-keyed dictionary, so they merge one-to-one against the tracked CourseModel.Chapters and
    // their tree position comes from ParentChapterId. Materials stay nested per chapter in the DTO
    // but are merged against a course-wide reservoir so a material that moved to another chapter
    // resolves to its single tracked instance instead of being duplicated. Children absent from the
    // DTO are left out of every collection and EF orphans/cascades them.
    private static void MergeCourseGraph(UpdateCourseDTO src, CourseModel dest, IRuntimeMapper mapper)
    {
        if (src.Chapters == null)
        {
            return;
        }

        var materialReservoir = (dest.Chapters ?? [])
            .Where(chapter => chapter.Materials != null)
            .SelectMany(chapter => chapter.Materials!)
            .GroupBy(material => material.Id)
            .ToDictionary(group => group.Key, group => group.First());

        dest.Chapters = src.Chapters.MapDictionaryToCollection<
            UpdateCourseChapterDTO,
            CourseChapterModel
        >(dest.Chapters, mapper);

        var chaptersById = dest.Chapters.ToDictionary(chapter => chapter.Id);

        foreach (var (key, chapterDto) in src.Chapters)
        {
            if (!Ulid.TryParse(key, out var chapterId)
                || !chaptersById.TryGetValue(chapterId, out var chapter))
            {
                continue;
            }

            chapter.CourseId = dest.Id;
            chapter.Materials = chapterDto.Materials.MergeFromReservoir(materialReservoir, mapper);

            foreach (var material in chapter.Materials)
            {
                material.ChapterId = chapter.Id;
            }
        }
    }

    private static void ApplyCourseHierarchy(CourseModel course)
    {
        if (course?.Chapters == null || course.Chapters.Count == 0)
            return;

        foreach (var chapter in course.Chapters)
            ApplyChapterHierarchy(course, chapter, null);
    }

    private static void ApplyChapterHierarchy(
        CourseModel course,
        CourseChapterModel chapter,
        CourseChapterModel? parentChapter
    )
    {
        chapter.CourseId = course.Id;
        chapter.ParentChapterId = parentChapter?.Id;

        if (chapter.Materials != null && chapter.Materials.Count > 0)
        {
            foreach (var material in chapter.Materials)
            {
                material.ChapterId = chapter.Id;
            }
        }

        if (chapter.SubChapters == null || chapter.SubChapters.Count == 0)
            return;

        foreach (var subChapter in chapter.SubChapters)
            ApplyChapterHierarchy(course, subChapter, chapter);
    }

    private static IDictionary<string, IdReferenceDTO>? MapEntitiesToIdReferenceDictionary<TModel>(
        IEnumerable<TModel>? entities
    )
        where TModel : BaseModel
    {
        if (entities == null)
            return null;

        return entities.ToDictionary(
            entity => entity.Id.ToString(),
            entity => new IdReferenceDTO { Id = entity.Id },
            StringComparer.OrdinalIgnoreCase
        );
    }
}
