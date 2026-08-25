using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Courses.AuthorizationRequirements;

namespace Noo.Api.Courses;

public class CoursePolicies : IPolicyRegistrar
{
    public const string CanSearchCourses = nameof(CanSearchCourses);
    public const string CanSearchOwnCourses = nameof(CanSearchOwnCourses);
    public const string CanGetCourse = nameof(CanGetCourse);
    public const string CanCreateCourse = nameof(CanCreateCourse);
    public const string CanSearchCourseMemberships = nameof(CanSearchCourseMemberships);
    public const string CanCreateCourseMembership = nameof(CanCreateCourseMembership);
    public const string CanDeleteCourseMembership = nameof(CanDeleteCourseMembership);
    public const string CanManageOwnCourseState = nameof(CanManageOwnCourseState);
    public const string CanReactToCourseMaterial = nameof(CanReactToCourseMaterial);
    public const string CanEditCourse = nameof(CanEditCourse);
    public const string CanDeleteCourse = nameof(CanDeleteCourse);

    public void RegisterPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(CanGetCourse, policy =>
            policy.AddRequirements(new CourseAccessRequirement()).RequireNotBlocked());

        options.AddPolicy(CanCreateCourse, policy =>
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked());

        options.AddPolicy(CanSearchCourses, policy =>
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher),
                nameof(UserRoles.Assistant),
                nameof(UserRoles.Mentor),
                nameof(UserRoles.Student)
            ).RequireNotBlocked());

        options.AddPolicy(CanCreateCourseMembership, policy =>
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked());

        options.AddPolicy(CanDeleteCourseMembership, policy =>
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked());

        options.AddPolicy(CanSearchOwnCourses, policy =>
            policy.RequireRole(
                nameof(UserRoles.Student)
            ).RequireNotBlocked());

        // The access requirement keeps a student from pinning a course they cannot open — and it
        // covers publicly open courses without a membership row for free.
        options.AddPolicy(CanManageOwnCourseState, policy =>
            policy.RequireRole(
                nameof(UserRoles.Student)
            ).AddRequirements(new CourseAccessRequirement()).RequireNotBlocked());

        options.AddPolicy(CanReactToCourseMaterial, policy =>
            policy.RequireRole(
                nameof(UserRoles.Student)
            ).AddRequirements(new CourseAccessRequirement()).RequireNotBlocked());

        options.AddPolicy(CanEditCourse, policy =>
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked());

        options.AddPolicy(CanDeleteCourse, policy =>
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher)
            ).RequireNotBlocked());

        // Memberships are a staff-side roster; students read their own list from GET /course/my.
        options.AddPolicy(CanSearchCourseMemberships, policy =>
            policy.RequireRole(
                nameof(UserRoles.Admin),
                nameof(UserRoles.Teacher),
                nameof(UserRoles.Assistant),
                nameof(UserRoles.Mentor)
            ).RequireNotBlocked());
    }
}
