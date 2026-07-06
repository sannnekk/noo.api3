using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Noo.Api.Users;

namespace Noo.UnitTests.Users.Authorization;

/// <summary>
/// UserAccessRequirementHandler resolves the target user from the "userId" route value.
/// Every action guarded by a policy backed by that requirement must therefore name its
/// route parameter "userId" — a differently named parameter (e.g. studentId/mentorId)
/// compiles and routes fine but silently breaks self access for students.
/// </summary>
public class UserAccessRouteContractTests
{
    private static readonly string[] UserAccessPolicies = [UserPolicies.CanGetUser];

    public static TheoryData<string, string> GuardedActions()
    {
        var data = new TheoryData<string, string>();

        var actions = typeof(UserPolicies).Assembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
            .SelectMany(type =>
                type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            )
            .Where(method =>
                method
                    .GetCustomAttributes<AuthorizeAttribute>()
                    .Any(attribute => UserAccessPolicies.Contains(attribute.Policy))
            );

        foreach (var action in actions)
        {
            var template = action
                .GetCustomAttributes<HttpMethodAttribute>()
                .Select(attribute => attribute.Template)
                .FirstOrDefault(t => t != null);

            data.Add($"{action.DeclaringType!.Name}.{action.Name}", template ?? string.Empty);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(GuardedActions))]
    public void Actions_Guarded_By_UserAccessRequirement_Use_UserId_Route_Parameter(
        string action,
        string routeTemplate
    )
    {
        Assert.True(
            routeTemplate.Contains("{userId", StringComparison.Ordinal),
            $"{action} is guarded by a user-access policy but its route template "
                + $"\"{routeTemplate}\" has no {{userId}} parameter. "
                + "UserAccessRequirementHandler reads the target user id from the "
                + "\"userId\" route value, so self access would always be denied."
        );
    }

    [Fact]
    public void GuardedActions_Are_Discovered()
    {
        // Guards the reflection query itself: if attribute usage changes and nothing
        // is discovered anymore, the contract test above would silently pass.
        Assert.NotEmpty(GuardedActions());
    }
}
