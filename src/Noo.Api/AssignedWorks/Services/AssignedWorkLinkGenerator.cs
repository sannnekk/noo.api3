using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.AssignedWorks.Services;

[RegisterTransient(typeof(IAssignedWorkLinkGenerator))]
public class AssignedWorkLinkGenerator : IAssignedWorkLinkGenerator
{
    public FrontendLink GenerateViewLink(Ulid assignedWorkId) =>
        new() { Name = "assigned-works.detail", Params = new { assignedWorkId } };
}
