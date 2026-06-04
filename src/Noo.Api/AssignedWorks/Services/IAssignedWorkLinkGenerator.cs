using Noo.Api.Core.Utils;

namespace Noo.Api.AssignedWorks.Services;

public interface IAssignedWorkLinkGenerator
{
    public FrontendLink GenerateViewLink(Ulid assignedWorkId);
}
