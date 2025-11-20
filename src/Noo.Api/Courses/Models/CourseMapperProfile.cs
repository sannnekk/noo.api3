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
        CreateMap<CreateCourseDTO, CourseModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Editors, opt => opt.Ignore())
            .ForMember(dest => dest.Authors, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Memberships, opt => opt.Ignore())
            .ForMember(dest => dest.Thumbnail, opt => opt.Ignore())
            .ForMember(dest => dest.Subject, opt => opt.Ignore());

        // Update mappings for patch operations
        // Map Model -> UpdateDTO (converts collections to dictionaries by ID)
        CreateMap<CourseModel, UpdateCourseDTO>()
            .ForMember(dest => dest.Chapters, opt => opt.MapFrom((src, _, _, context) =>
                src.Chapters.MapCollectionToDictionary<CourseChapterModel, UpdateCourseChapterDTO>(context)));

        CreateMap<UpdateCourseDTO, CourseModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Editors, opt => opt.Ignore())
            .ForMember(dest => dest.Authors, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Memberships, opt => opt.Ignore())
            .ForMember(dest => dest.Thumbnail, opt => opt.Ignore())
            .ForMember(dest => dest.Subject, opt => opt.Ignore())
            .ForMember(dest => dest.Chapters, opt => opt.MapFrom((src, _, _, context) =>
                src.Chapters.MapDictionaryToCollection<UpdateCourseChapterDTO, CourseChapterModel>(context.Mapper)));

        // Chapter mappings
        CreateMap<CourseChapterModel, UpdateCourseChapterDTO>()
            .ForMember(dest => dest.SubChapters, opt => opt.MapFrom((src, _, _, context) =>
                src.SubChapters.MapCollectionToDictionary<CourseChapterModel, UpdateCourseChapterDTO>(context)))
            .ForMember(dest => dest.Materials, opt => opt.MapFrom((src, _, _, context) =>
                src.Materials.MapCollectionToDictionary<CourseMaterialModel, UpdateCourseMaterialDTO>(context)));

        CreateMap<UpdateCourseChapterDTO, CourseChapterModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Order, opt => opt.Ignore())
            .ForMember(dest => dest.Course, opt => opt.Ignore())
            .ForMember(dest => dest.CourseId, opt => opt.Ignore())
            .ForMember(dest => dest.ParentChapter, opt => opt.Ignore())
            .ForMember(dest => dest.SubChapters, opt => opt.MapFrom((src, _, _, context) =>
                src.SubChapters.MapDictionaryToCollection<UpdateCourseChapterDTO, CourseChapterModel>(context.Mapper)))
            .ForMember(dest => dest.Materials, opt => opt.MapFrom((src, _, _, context) =>
                src.Materials.MapDictionaryToCollection<UpdateCourseMaterialDTO, CourseMaterialModel>(context.Mapper)));

        // Material mappings
        CreateMap<CourseMaterialModel, UpdateCourseMaterialDTO>();

        CreateMap<UpdateCourseMaterialDTO, CourseMaterialModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Order, opt => opt.Ignore())
            .ForMember(dest => dest.Chapter, opt => opt.Ignore())
            .ForMember(dest => dest.Content, opt => opt.Ignore())
            .ForMember(dest => dest.Reactions, opt => opt.Ignore());

        CreateMap<CreateCourseChapterDTO, CourseChapterModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Order, opt => opt.Ignore())
            .ForMember(dest => dest.Course, opt => opt.Ignore())
            .ForMember(dest => dest.CourseId, opt => opt.Ignore())
            .ForMember(dest => dest.ParentChapter, opt => opt.Ignore())
            .ForMember(dest => dest.ParentChapterId, opt => opt.Ignore());

        CreateMap<CreateCourseMaterialDTO, CourseMaterialModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Order, opt => opt.Ignore())
            .ForMember(dest => dest.Chapter, opt => opt.Ignore())
            .ForMember(dest => dest.ChapterId, opt => opt.Ignore())
            .ForMember(dest => dest.Content, opt => opt.Ignore())
            .ForMember(dest => dest.ContentId, opt => opt.Ignore())
            .ForMember(dest => dest.Reactions, opt => opt.Ignore());

        // Allow creating content entities from DTOs
        CreateMap<CreateCourseMaterialContentDTO, CourseMaterialContentModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Material, opt => opt.Ignore())
            .ForMember(dest => dest.Work, opt => opt.Ignore());

        // Update content mappings for patch operations
        CreateMap<CourseMaterialContentModel, UpdateCourseMaterialContentDTO>();
        CreateMap<UpdateCourseMaterialContentDTO, CourseMaterialContentModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Material, opt => opt.Ignore())
            .ForMember(dest => dest.Work, opt => opt.Ignore());

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

        CreateMap<CourseChapterModel, CourseChapterDTO>()
            .MaxDepth(CourseConfig.MaxChapterTreeDepth);
        CreateMap<CourseMaterialModel, CourseMaterialDTO>();

        CreateMap<CourseMaterialContentModel, CourseMaterialContentDTO>()
            .ForMember(dest => dest.Work, opt => opt.Ignore());

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
    }
}
