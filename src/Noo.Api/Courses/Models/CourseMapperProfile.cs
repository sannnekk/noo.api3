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
            .ForMember(dest => dest.Chapters, opt => opt.Ignore())
            .ForMember(dest => dest.Editors, opt => opt.Ignore())
            .ForMember(dest => dest.Authors, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Memberships, opt => opt.Ignore())
            .ForMember(dest => dest.Thumbnail, opt => opt.Ignore())
            .ForMember(dest => dest.Subject, opt => opt.Ignore());

        // update mappings for patch operations
        CreateMap<CourseModel, UpdateCourseDTO>();
        CreateMap<UpdateCourseDTO, CourseModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Chapters, opt => opt.Ignore())
            .ForMember(dest => dest.Editors, opt => opt.Ignore())
            .ForMember(dest => dest.Authors, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Memberships, opt => opt.Ignore())
            .ForMember(dest => dest.Thumbnail, opt => opt.Ignore())
            .ForMember(dest => dest.Subject, opt => opt.Ignore());

        // Allow creating content entities from DTOs
        CreateMap<CreateCourseMaterialContentDTO, CourseMaterialContentModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Material, opt => opt.Ignore())
            .ForMember(dest => dest.Work, opt => opt.Ignore());

        // Update content mappings for patch operations
        CreateMap<CourseMaterialContentModel, Noo.Api.Courses.UpdateCourseMaterialContentDTO>();
        CreateMap<Noo.Api.Courses.UpdateCourseMaterialContentDTO, CourseMaterialContentModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Material, opt => opt.Ignore())
            .ForMember(dest => dest.Work, opt => opt.Ignore());

        CreateMap<CourseModel, CourseDTO>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.ThumbnailId, opt => opt.MapFrom(src => src.ThumbnailId))
            .ForMember(dest => dest.Chapters, opt => opt.MapFrom(src => src.Chapters))
            .ForMember(dest => dest.MemberCount, opt => opt.MapFrom<CourseMemberCountValueResolver>())
            .ForMember(dest => dest.Thumbnail, opt => opt.Ignore())
            .ForMember(dest => dest.Subject, opt => opt.Ignore());

        CreateMap<CourseChapterModel, CourseChapterDTO>();
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
