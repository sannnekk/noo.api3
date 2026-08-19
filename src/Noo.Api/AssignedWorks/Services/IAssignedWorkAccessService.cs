using Noo.Api.AssignedWorks.Models;

namespace Noo.Api.AssignedWorks.Services;

/// <summary>
/// Who may do what to a work that has already been loaded. The role gate lives in
/// <see cref="AssignedWorkPolicies"/> and runs first; this decides the part that depends on
/// the work itself — whether the caller takes part in it, and what state it is in.
/// </summary>
public interface IAssignedWorkAccessService
{
    public bool CanRead(AssignedWorkModel assignedWork);
    public bool CanDelete(AssignedWorkModel assignedWork);
    public bool CanArchive(AssignedWorkModel assignedWork);
    public bool CanAssignMainMentor(AssignedWorkModel assignedWork);
    public bool CanAssignHelperMentor(AssignedWorkModel assignedWork);
}
