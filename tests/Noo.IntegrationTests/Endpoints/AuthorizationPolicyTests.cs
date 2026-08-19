using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Noo.IntegrationTests.Endpoints;

/// <summary>
/// A policy named on an endpoint but never registered is not a quiet mistake: ASP.NET throws
/// when the request arrives, so the endpoint answers 500 to everyone. This walks the whole
/// API rather than any one module, because the mistake is invisible until someone calls.
/// </summary>
public class AuthorizationPolicyTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthorizationPolicyTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact(DisplayName = "every policy named by a controller action is registered")]
    public async Task Every_Named_Policy_Is_Registered()
    {
        using var scope = _factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();

        var names = typeof(Noo.Api.Program).Assembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(type =>
                type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Concat([(MemberInfo)type])
            )
            .SelectMany(member => member.GetCustomAttributes<AuthorizeAttribute>(inherit: true))
            .Select(attribute => attribute.Policy)
            .Where(policy => !string.IsNullOrEmpty(policy))
            .Distinct()
            .ToList();

        names.Should().NotBeEmpty("the scan must actually find the [Authorize(Policy = ...)] attributes");

        var missing = new List<string>();

        foreach (var name in names)
        {
            if (await provider.GetPolicyAsync(name!) is null)
            {
                missing.Add(name!);
            }
        }

        missing.Should().BeEmpty();
    }
}
