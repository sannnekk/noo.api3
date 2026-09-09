using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AutoMapper;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Collaboration;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Core.Utils.Json;
using Noo.Api.Works.DTO;
using Noo.Api.Works.Services;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace Noo.Api.Works.Collaboration;

/// <summary>
/// Everything collaborative editing needs to know about a work. Saving is the existing PATCH
/// endpoint's own pipeline: expressing the shared draft as JSON Patch operations means there is
/// no second way to write a work, so task renumbering, the recomputed total score and the
/// validation all keep happening exactly once, in one place.
/// </summary>
[RegisterScoped(typeof(ICollaborationRoomHandler))]
public class WorkCollaborationHandler : ICollaborationRoomHandler
{
    /// <summary>
    /// The same conventions the controller's patch endpoint reads with. Without them a task's
    /// <c>"word"</c> would not bind to the enum and a rich-text value's <c>$type</c> would not be
    /// honoured — an operation would apply through HTTP and fail here, for no visible reason.
    /// </summary>
    private static readonly JsonSerializerOptions _json = BuildOptions();

    private static JsonSerializerOptions BuildOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            AllowOutOfOrderMetadataProperties = true,
        };

        options.AddNooConverters();

        return options;
    }

    private readonly IWorkService _workService;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;

    public WorkCollaborationHandler(
        IWorkService workService,
        IMapper mapper,
        ICurrentUser currentUser
    )
    {
        _workService = workService;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public string RoomType => "work";

    /// <summary>
    /// The same roles the work's own PATCH endpoint admits, checked here rather than by a policy
    /// in Core so that the next handler can answer differently without touching anything shared.
    /// </summary>
    public async Task<bool> CanEditAsync(Ulid roomId, CancellationToken ct = default) =>
        _currentUser.IsInRole(UserRoles.Admin, UserRoles.Teacher)
        && await _workService.GetWorkAsync(roomId) is not null;

    /// <summary>
    /// Only what <see cref="UpdateWorkDTO"/> would accept. Checked when the operation enters the
    /// draft rather than when someone presses Save, so a malformed path fails for the client that
    /// sent it instead of for whoever saves an hour later.
    ///
    /// The field names are read off the DTOs rather than written out here, because a hand-kept
    /// list is a list that drifts: a property renamed on the patch endpoint would otherwise keep
    /// being accepted into drafts that can no longer be saved.
    /// </summary>
    public bool IsPathAllowed(string path) =>
        path.Split('/') switch
        {
            ["", var field] => _workFields.Contains(field),
            ["", "tasks", var id] => Ulid.TryParse(id, out _),
            ["", "tasks", var id, var field] => Ulid.TryParse(id, out _)
                && _taskFields.Contains(field),
            _ => false,
        };

    public async Task SaveAsync(
        Ulid roomId,
        IReadOnlyList<CollaborationOp> ops,
        CancellationToken ct = default
    ) => await _workService.UpdateWorkAsync(roomId, ToPatchDocument(ops));

    /// <summary>
    /// Replaces a session's worth of operations with the state they add up to: one operation per
    /// task, plus the work's own fields. Bounded by the size of the work rather than by how long
    /// anyone has been editing it, which is the point.
    /// </summary>
    public async Task<IReadOnlyList<CollaborationOp>> CompactAsync(
        Ulid roomId,
        IReadOnlyList<CollaborationOp> ops,
        CancellationToken ct = default
    )
    {
        var work = await _workService.GetWorkAsync(roomId);

        if (work is null)
        {
            return ops;
        }

        var saved = _mapper.Map<UpdateWorkDTO>(work);
        var draft = _mapper.Map<UpdateWorkDTO>(work);

        ToPatchDocument(ops).ApplyTo(draft);

        var compacted = new List<CollaborationOp>
        {
            Replace("/title", draft.Title),
            Replace("/description", draft.Description),
            Replace("/type", draft.Type),
            Replace("/subjectId", draft.SubjectId),
        };

        var draftTasks = draft.Tasks ?? new Dictionary<string, UpdateWorkTaskDTO>();
        var savedTasks = saved.Tasks ?? new Dictionary<string, UpdateWorkTaskDTO>();

        foreach (var (id, task) in draftTasks)
        {
            compacted.Add(Replace($"/tasks/{id}", task));
        }

        // Tasks the draft dropped: without these, replaying the compacted log onto the saved work
        // would quietly bring them back.
        foreach (var id in savedTasks.Keys.Except(draftTasks.Keys))
        {
            compacted.Add(new CollaborationOp { Op = "remove", Path = $"/tasks/{id}" });
        }

        return compacted;
    }

    private static CollaborationOp Replace<T>(string path, T value) =>
        new()
        {
            Op = "replace",
            Path = path,
            Value = JsonSerializer.SerializeToNode(value, _json),
        };

    private static JsonPatchDocument<UpdateWorkDTO> ToPatchDocument(
        IReadOnlyList<CollaborationOp> ops
    )
    {
        var document = new JsonPatchDocument<UpdateWorkDTO> { Options = _json };

        foreach (var op in ops)
        {
            document.Operations.Add(
                new Operation<UpdateWorkDTO>(op.Op, op.Path, from: null, value: ToValue(op.Value))
            );
        }

        return document;
    }

    /// <summary>
    /// The operation's value has to arrive as something the patch library will bind to the DTO
    /// property. A <see cref="JsonNode"/> would be assigned as a node; round-tripping it through
    /// its own JSON hands over a <see cref="JsonElement"/>, which is what the library expects.
    /// </summary>
    private static object? ToValue(JsonNode? value) =>
        value is null ? null : JsonSerializer.Deserialize<JsonElement>(value.ToJsonString());

    /// <summary>
    /// The work's own editable fields. <c>id</c> is not one of them, and <c>tasks</c> is reached
    /// a task at a time — replacing the whole collection in one operation would make every
    /// concurrent edit to a task inside it unmergeable.
    /// </summary>
    private static readonly FrozenSet<string> _workFields = JsonFieldsOf<UpdateWorkDTO>(
        except: ["id", "tasks"]
    );

    private static readonly FrozenSet<string> _taskFields = JsonFieldsOf<UpdateWorkTaskDTO>(
        except: ["id"]
    );

    /// <summary>
    /// The names the patch endpoint binds, taken from the DTO itself so the two cannot drift.
    /// </summary>
    private static FrozenSet<string> JsonFieldsOf<T>(string[] except) =>
        typeof(T)
            .GetProperties()
            .Select(property =>
                property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name)
            )
            .Except(except)
            .ToFrozenSet(StringComparer.Ordinal);
}
