using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Courses.AuthorizationRequirements;
using Noo.Api.Courses.Services;

namespace Noo.UnitTests.Courses.Authorization;

public class CourseAccessRequirementHandlerTests
{
    private static AuthorizationHandlerContext MakeContext(UserRoles role, Ulid userId, Ulid courseId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues["courseId"] = courseId.ToString();

        var requirement = new CourseAccessRequirement();
        return new AuthorizationHandlerContext([requirement], principal, httpContext);
    }

    [Theory]
    [InlineData(UserRoles.Admin)]
    [InlineData(UserRoles.Teacher)]
    [InlineData(UserRoles.Assistant)]
    [InlineData(UserRoles.Mentor)]
    public async Task AlwaysAllowedRoles_Succeed(UserRoles role)
    {
        var membership = new Mock<ICourseAccessService>(MockBehavior.Strict);
        var handler = new CourseAccessRequirementHandler(membership.Object);

        var ctx = MakeContext(role, Ulid.NewUlid(), Ulid.NewUlid());
        var requirement = ctx.Requirements.OfType<CourseAccessRequirement>().Single();
        await handler.HandleAsync(ctx);

        Assert.True(ctx.HasSucceeded);
    }

    [Fact]
    public async Task Student_With_Access_Succeeds_Without_Fails()
    {
        var studentId = Ulid.NewUlid();
        var courseId = Ulid.NewUlid();
        var membership = new Mock<ICourseAccessService>();
        membership.Setup(m => m.HasAccessAsync(courseId, studentId)).ReturnsAsync(true);
        var handler = new CourseAccessRequirementHandler(membership.Object);
        var ctx = MakeContext(UserRoles.Student, studentId, courseId);
        var requirement = ctx.Requirements.OfType<CourseAccessRequirement>().Single();
        await handler.HandleAsync(ctx);
        Assert.True(ctx.HasSucceeded);

        // Now fail
        var membershipFail = new Mock<ICourseAccessService>();
        membershipFail.Setup(m => m.HasAccessAsync(courseId, studentId)).ReturnsAsync(false);
        var handlerFail = new CourseAccessRequirementHandler(membershipFail.Object);
        var ctxFail = MakeContext(UserRoles.Student, studentId, courseId);
        await handlerFail.HandleAsync(ctxFail);
        Assert.False(ctxFail.HasSucceeded);
    }

    [Fact]
    public async Task Invalid_CourseId_Fails()
    {
        var membership = new Mock<ICourseAccessService>(MockBehavior.Strict);
        var handler = new CourseAccessRequirementHandler(membership.Object);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Ulid.NewUlid().ToString()),
            new(ClaimTypes.Role, UserRoles.Student.ToString())
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues["courseId"] = "not-a-ulid";

        var requirement = new CourseAccessRequirement();
        var ctx = new AuthorizationHandlerContext([requirement], principal, httpContext);
        await handler.HandleAsync(ctx);
        Assert.False(ctx.HasSucceeded);
    }
}
