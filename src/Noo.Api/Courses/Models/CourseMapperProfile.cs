using AutoMapper;
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
            .ForMember(dest => dest.Memberships, opt => opt.Ignore())
            .ForMember(dest => dest.Thumbnail, opt => opt.Ignore())
            .ForMember(dest => dest.Subject, opt => opt.Ignore())
            .AfterMap((_, dest) => ApplyCourseHierarchy(dest));

        CreateMap<CourseModel, UpdateCourseDTO>()
            .ForMember(dest => dest.Chapters, opt => opt.MapFrom((src, _, _, context) =>
                src.Chapters.MapCollectionToDictionary<CourseChapterModel, UpdateCourseChapterDTO>(context)));

        CreateMap<UpdateCourseDTO, CourseModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            // TODO: implement editors and authors update
            .ForMember(dest => dest.Editors, opt => opt.Ignore())
            .ForMember(dest => dest.Authors, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Memberships, opt => opt.Ignore())
            .ForMember(dest => dest.Thumbnail, opt => opt.Ignore())
            .ForMember(dest => dest.Subject, opt => opt.Ignore())
            .ForMember(dest => dest.Chapters, opt =>
            {
                opt.Condition(src => src.Chapters != null); opt.MapFrom((src, _, _, context) =>
                src.Chapters.MapDictionaryToCollection<UpdateCourseChapterDTO, CourseChapterModel>(context.Mapper));
            })
            .AfterMap((_, dest) => ApplyCourseHierarchy(dest));

        CreateMap<CourseModel, CourseDTO>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.ThumbnailId, opt => opt.MapFrom(src => src.ThumbnailId))
            // TODO: implement
            //.ForMember(dest => dest.MemberCount, opt => opt.MapFrom<CourseMemberCountValueResolver>())
            // TODO: remove
            .ForMember(dest => dest.MemberCount, opt => opt.Ignore())
            .ForMember(dest => dest.Thumbnail, opt => opt.Ignore())
            .ForMember(dest => dest.Subject, opt => opt.Ignore())
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
            .ForMember(dest => dest.SubChapters, opt => opt.MapFrom((src, _, _, context) =>
                src.SubChapters.MapCollectionToDictionary<CourseChapterModel, UpdateCourseChapterDTO>(context)))
            .ForMember(dest => dest.Materials, opt => opt.MapFrom((src, _, _, context) =>
                src.Materials.MapCollectionToDictionary<CourseMaterialModel, UpdateCourseMaterialDTO>(context)));

        CreateMap<UpdateCourseChapterDTO, CourseChapterModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Course, opt => opt.Ignore())
            .ForMember(dest => dest.CourseId, opt => opt.Ignore())
            .ForMember(dest => dest.ParentChapter, opt => opt.Ignore())
            .ForMember(dest => dest.SubChapters, opt =>
            {
                opt.Condition(src => src.SubChapters != null);
                opt.MapFrom((src, _, _, context) => src.SubChapters
                    .MapDictionaryToCollection<UpdateCourseChapterDTO, CourseChapterModel>(context.Mapper));
            })
            .ForMember(dest => dest.Materials, opt =>
            {
                opt.Condition(src => src.Materials != null);
                opt.MapFrom((src, _, _, context) => src.Materials
                    .MapDictionaryToCollection<UpdateCourseMaterialDTO, CourseMaterialModel>(context.Mapper));
            });

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

        CreateMap<CourseMaterialModel, CourseMaterialDTO>();

        // Course material content
        CreateMap<CreateCourseMaterialContentDTO, CourseMaterialContentModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Material, opt => opt.Ignore())
            // TODO: add mapping
            .ForMember(dest => dest.Poll, opt => opt.Ignore())
            // TODO: add mapping
            .ForMember(dest => dest.NooTubeVideos, opt => opt.Ignore())
            // TODO: add mapping
            .ForMember(dest => dest.Medias, opt => opt.Ignore())
            .ForMember(dest => dest.Reactions, opt => opt.Ignore());

        CreateMap<CourseMaterialContentModel, UpdateCourseMaterialContentDTO>()
            // TODO: add mapping
            .ForMember(dest => dest.NooTubeVideoIds, opt => opt.Ignore())
            // TODO: add mapping
            .ForMember(dest => dest.MediaIds, opt => opt.Ignore());

        CreateMap<UpdateCourseMaterialContentDTO, CourseMaterialContentModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            // TODO: add mapping
            .ForMember(dest => dest.NooTubeVideos, opt => opt.Ignore())
            // TODO: add mapping
            .ForMember(dest => dest.Medias, opt => opt.Ignore())
            // TODO: add mapping
            .ForMember(dest => dest.Poll, opt => opt.Ignore())
            .ForMember(dest => dest.Reactions, opt => opt.Ignore())
            .ForMember(dest => dest.Material, opt => opt.Ignore());

        CreateMap<CourseMaterialContentModel, CourseMaterialContentDTO>();

        // Course membership
        CreateMap<CourseMembershipModel, CourseMembershipDTO>()
                .ForMember(dest => dest.Course, opt => opt.Ignore())
                .ForMember(dest => dest.Student, opt => opt.Ignore())
                .ForMember(dest => dest.Assigner, opt => opt.Ignore());

        CreateMap<CreateCourseMembershipDTO, CourseMembershipModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Assigner, opt => opt.Ignore())
                .ForMember(dest => dest.Course, opt => opt.Ignore())
                .ForMember(dest => dest.Student, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
                .ForMember(dest => dest.IsArchived, opt => opt.MapFrom(_ => false))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(_ => CourseMembershipType.ManualAssigned))
                .ForMember(dest => dest.AssignerId, opt => opt.Ignore());

        // Course work assignment
        CreateMap<CreateCourseWorkAssignmentDTO, CourseWorkAssignmentModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CourseMaterialContentId, opt => opt.Ignore())
            .ForMember(dest => dest.CourseMaterialContent, opt => opt.Ignore())
            .ForMember(dest => dest.Work, opt => opt.Ignore());

        CreateMap<CourseWorkAssignmentModel, CourseWorkAssignmentDTO>();

        CreateMap<UpdateCourseWorkAssignmentDTO, CourseWorkAssignmentModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CourseMaterialContentId, opt => opt.Ignore())
            .ForMember(dest => dest.CourseMaterialContent, opt => opt.Ignore())
            .ForMember(dest => dest.Work, opt => opt.Ignore());

        CreateMap<CourseWorkAssignmentModel, UpdateCourseWorkAssignmentDTO>();
    }

    private static void ApplyCourseHierarchy(CourseModel course)
    {
        if (course?.Chapters == null || course.Chapters.Count == 0)
            return;

        foreach (var chapter in course.Chapters)
            ApplyChapterHierarchy(course, chapter, null);
    }

    private static void ApplyChapterHierarchy(CourseModel course, CourseChapterModel chapter, CourseChapterModel? parentChapter)
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
}
