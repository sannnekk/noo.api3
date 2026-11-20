using Noo.Api.Core.Utils.DI;
using Noo.Api.UserHistory.Services;
using Noo.Api.UserHistory.Types;

namespace Noo.Api.AssignedWorks.Services;

public interface IAssignedWorkUserHistoryRecorder
{
    public Task RecordAsync(Ulid assignedWorkId, Ulid? actorId, UserHistoryType type);
}

[RegisterScoped(typeof(IAssignedWorkUserHistoryRecorder))]
public class AssignedWorkUserHistoryRecorder : IAssignedWorkUserHistoryRecorder
{
    private readonly IAssignedWorkRepository _assignedWorkRepository;
    private readonly IUserHistoryService _userHistoryService;

    public AssignedWorkUserHistoryRecorder(
        IAssignedWorkRepository assignedWorkRepository,
        IUserHistoryService userHistoryService
    )
    {
        _assignedWorkRepository = assignedWorkRepository;
        _userHistoryService = userHistoryService;
    }

    public async Task RecordAsync(Ulid assignedWorkId, Ulid? actorId, UserHistoryType type)
    {
        var assignedWork = await _assignedWorkRepository.GetByIdAsync(assignedWorkId);

        if (assignedWork is null)
        {
            return;
        }

        _userHistoryService.Record(
            assignedWork.StudentId,
            actorId,
            type,
            new Dictionary<string, string>
            {
                ["assignedWorkId"] = assignedWork.Id.ToString(),
                ["workTitle"] = assignedWork.Title,
            }
        );
    }
}
