using System.ComponentModel.DataAnnotations;

namespace Noo.Api.Courses.DTO;

public record CreateCourseMembershipDTO
{
    [Required]
    public Ulid StudentId { get; init; }

    [Required]
    public Ulid CourseId { get; init; }

    public bool NotifyStudent { get; init; } = true;
}
