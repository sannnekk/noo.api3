using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Users.DTO;

namespace Noo.Api.Users.AuthorizationRequirements;

[RegisterScoped(typeof(IAuthorizationHandler))]
public class MentorAssignRequirementHandler : AuthorizationHandler<MentorAssignRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, MentorAssignRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
        {
            context.Fail();
            return;
        }

        var currentRole = context.User.GetRole();
        var currentUserId = context.User.GetId();

        if (currentRole == null || currentUserId == Ulid.Empty)
        {
            context.Fail();
            return;
        }

        // Elevated roles can always assign
        if (requirement.ElevatedRoles.Any(role => role == currentRole))
        {
            context.Succeed(requirement);
            return;
        }

        if (currentRole == UserRoles.Mentor)
        {
            // Need to ensure the mentor is assigning themselves as MentorId
            try
            {
                httpContext.Request.EnableBuffering();
                using var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                httpContext.Request.Body.Position = 0;
                if (!string.IsNullOrWhiteSpace(body))
                {
                    var dto = JsonSerializer.Deserialize<CreateMentorAssignmentDTO>(body, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (dto != null && dto.MentorId == currentUserId)
                    {
                        context.Succeed(requirement);
                        return;
                    }
                }
            }
            catch
            {
                // Ignore and fail below
            }
        }

        context.Fail();
    }
}
