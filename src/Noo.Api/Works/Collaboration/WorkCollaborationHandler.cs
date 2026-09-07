using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AutoMapper;
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
public partial class WorkCollaborationHandler : ICollaborationRoomHandler
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

    public WorkCollaborationHandler(IWorkService workService, IMapper mapper)
    {
        _workService = workService;
        _mapper = mapper;
    }

    public string RoomType => "work";

    public async Task<bool> CanEditAsync(Ulid roomId, CancellationToken ct = default) =>
        await _workService.GetWorkAsync(roomId) is not null;

    /// <summary>
    /// Only what <see cref="UpdateWorkDTO"/> would accept. Checked when the operation enters the
    /// draft rather than when someone presses Save, so a malformed path fails for the client that
    /// sent it instead of for whoever saves an hour later.
    /// </summary>
    public bool IsPathAllowed(string path) => EditablePath().IsMatch(path);

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
    /// The work's own fields, a whole task, or one field of a task. Task keys are ULIDs because
    /// the patch exposes the collection as a dictionary keyed by id — which is also what makes an
    /// operation survive another editor reordering the tasks under it.
    /// </summary>
    [GeneratedRegex(
        @"^/(title|description|type|subjectId|tasks/[0-9A-HJKMNP-TV-Za-hjkmnp-tv-z]{26}(/[a-zA-Z]+)?)$"
    )]
    private static partial Regex EditablePath();
}
