using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Users.AuthorizationRequirements;
using Noo.Api.Users.Services;

namespace Noo.UnitTests.Users.Authorization;

public class UserAccessRequirementHandlerTests
{
    private static UserAccessRequirementHandler MakeHandler()
    {
        var mentorAssignments = new Mock<IMentorAssignmentRepository>(MockBehavior.Strict);
        return new UserAccessRequirementHandler(mentorAssignments.Object);
    }

    private static AuthorizationHandlerContext MakeContext(
        UserRoles role,
        Ulid currentUserId,
        string routeKey,
        Ulid targetUserId
    )
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, currentUserId.ToString()),
            new(ClaimTypes.Role, role.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues[routeKey] = targetUserId.ToString();

        var requirement = new UserAccessRequirement();
        return new AuthorizationHandlerContext([requirement], principal, httpContext);
    }

    [Theory]
    [InlineData(UserRoles.Admin)]
    [InlineData(UserRoles.Teacher)]
    [InlineData(UserRoles.Assistant)]
    [InlineData(UserRoles.Mentor)]
    public async Task AlwaysAllowedRoles_Succeed(UserRoles role)
    {
        var handler = MakeHandler();

        var ctx = MakeContext(role, Ulid.NewUlid(), "userId", Ulid.NewUlid());
        await handler.HandleAsync(ctx);

        Assert.True(ctx.HasSucceeded);
    }

    [Fact]
    public async Task Student_Accessing_Self_Succeeds()
    {
        var handler = MakeHandler();
        var studentId = Ulid.NewUlid();

        var ctx = MakeContext(UserRoles.Student, studentId, "userId", studentId);
        await handler.HandleAsync(ctx);

        Assert.True(ctx.HasSucceeded);
    }

    [Fact]
    public async Task Student_Accessing_Other_User_Fails()
    {
        var handler = MakeHandler();

        var ctx = MakeContext(UserRoles.Student, Ulid.NewUlid(), "userId", Ulid.NewUlid());
        await handler.HandleAsync(ctx);

        Assert.False(ctx.HasSucceeded);
    }

    // Regression: the handler only recognizes the "userId" route key. Endpoints that
    // exposed the target id under a different name (studentId/mentorId) silently broke
    // self access for students.
    [Theory]
    [InlineData("studentId")]
    [InlineData("mentorId")]
    [InlineData("id")]
    public async Task Student_Accessing_Self_Under_Wrong_Route_Key_Fails(string routeKey)
    {
        var handler = MakeHandler();
        var studentId = Ulid.NewUlid();

        var ctx = MakeContext(UserRoles.Student, studentId, routeKey, studentId);
        await handler.HandleAsync(ctx);

        Assert.False(ctx.HasSucceeded);
    }

    [Fact]
    public async Task Missing_Route_Value_Fails()
    {
        var handler = MakeHandler();
        var studentId = Ulid.NewUlid();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, studentId.ToString()),
            new(ClaimTypes.Role, UserRoles.Student.ToString())
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var httpContext = new DefaultHttpContext();

        var requirement = new UserAccessRequirement();
        var ctx = new AuthorizationHandlerContext([requirement], principal, httpContext);
        await handler.HandleAsync(ctx);

        Assert.False(ctx.HasSucceeded);
    }

    [Fact]
    public async Task Non_HttpContext_Resource_Fails()
    {
        var handler = MakeHandler();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Ulid.NewUlid().ToString()),
            new(ClaimTypes.Role, UserRoles.Student.ToString())
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var requirement = new UserAccessRequirement();
        var ctx = new AuthorizationHandlerContext([requirement], principal, resource: null);
        await handler.HandleAsync(ctx);

        Assert.False(ctx.HasSucceeded);
    }
}
