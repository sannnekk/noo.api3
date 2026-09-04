using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Noo.Api.Core.Security.Authorization;

namespace Noo.UnitTests.Core.Security;

public class CurrentUserTests
{
    private static ClaimsPrincipal Principal(Ulid userId, UserRoles role)
        => new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role.ToString())
            ],
            authenticationType: "Test"));

    private static ClaimsPrincipalAccessor AccessorFor(HttpContext? httpContext)
        => new(new HttpContextAccessor { HttpContext = httpContext });

    [Fact]
    public void ReadsTheUserFromTheAmbientHttpRequest()
    {
        var userId = Ulid.NewUlid();
        var httpContext = new DefaultHttpContext { User = Principal(userId, UserRoles.Teacher) };

        var currentUser = new CurrentUser(AccessorFor(httpContext));

        Assert.Equal(userId, currentUser.UserId);
        Assert.Equal(UserRoles.Teacher, currentUser.UserRole);
        Assert.True(currentUser.IsAuthenticated);
        Assert.True(currentUser.IsInRole(UserRoles.Teacher, UserRoles.Admin));
        Assert.False(currentUser.IsInRole(UserRoles.Student));
    }

    [Fact]
    public void ReadsTheUserFromTheAccessorWhenThereIsNoHttpRequest()
    {
        var userId = Ulid.NewUlid();
        var accessor = AccessorFor(null);
        accessor.Principal = Principal(userId, UserRoles.Student);

        var currentUser = new CurrentUser(accessor);

        Assert.Equal(userId, currentUser.UserId);
        Assert.Equal(UserRoles.Student, currentUser.UserRole);
        Assert.True(currentUser.IsAuthenticated);
    }

    [Fact]
    public void PrefersTheExplicitPrincipalOverTheAmbientHttpRequest()
    {
        var hubUserId = Ulid.NewUlid();
        var httpContext = new DefaultHttpContext
        {
            User = Principal(Ulid.NewUlid(), UserRoles.Admin)
        };

        var accessor = AccessorFor(httpContext);
        accessor.Principal = Principal(hubUserId, UserRoles.Student);

        var currentUser = new CurrentUser(accessor);

        Assert.Equal(hubUserId, currentUser.UserId);
        Assert.Equal(UserRoles.Student, currentUser.UserRole);
    }

    // The case that matters for hubs: SignalR resolves the hub and its dependencies before the
    // filter supplying the principal runs, so anything captured at construction time would
    // report an anonymous user for every invocation.
    [Fact]
    public void ResolvesThePrincipalOnEveryReadRatherThanAtConstruction()
    {
        var accessor = AccessorFor(null);
        var currentUser = new CurrentUser(accessor);

        Assert.Null(currentUser.UserId);
        Assert.False(currentUser.IsAuthenticated);

        var userId = Ulid.NewUlid();
        accessor.Principal = Principal(userId, UserRoles.Mentor);

        Assert.Equal(userId, currentUser.UserId);
        Assert.Equal(UserRoles.Mentor, currentUser.UserRole);
        Assert.True(currentUser.IsAuthenticated);
    }

    [Fact]
    public void ReportsNoUserWhenThereIsNeitherARequestNorAPrincipal()
    {
        var currentUser = new CurrentUser(AccessorFor(null));

        Assert.Null(currentUser.UserId);
        Assert.Null(currentUser.UserRole);
        Assert.False(currentUser.IsAuthenticated);
        Assert.False(currentUser.IsInRole(UserRoles.Admin));
    }
}
