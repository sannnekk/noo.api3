using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Collaboration;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Core.Utils.Json;
using Noo.Api.Courses.DTO;
using Noo.Api.Courses.Services;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace Noo.Api.Courses.Collaboration;

/// <summary>
/// Everything collaborative editing needs to know about a course.
///
/// A course is two entities on the wire — the chapter tree, and a separate content row per
/// material — with a patch endpoint each. That difference lives here and nowhere else: the draft
/// is one flat list of operations like any other document's, and this splits it back into the two
/// pipelines on the way to MySQL, so neither the hub nor the client knows a course is unusual.
/// </summary>
[RegisterScoped(typeof(ICollaborationRoomHandler))]
public class CourseCollaborationHandler : ICollaborationRoomHandler
{
    /// <summary>
    /// The segment that turns a material's path into its content's. A material's own fields live
    /// on the course patch; everything past this belongs to the content row it points at.
    /// </summary>
    private const string _contentSegment = "content";

    /// <summary>
    /// The same conventions the controller's patch endpoints read with, so an operation that would
    /// apply over HTTP applies here too — rich text needs its <c>$type</c> honoured, and the
    /// enums are hyphen-lower-case on the wire.
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

    /// <summary>
    /// <c>chapters</c> is reached a chapter at a time, so replacing the whole collection in one
    /// operation cannot happen — that would make every concurrent edit inside it unmergeable.
    /// </summary>
    private static readonly FrozenSet<string> _courseFields = JsonFieldsOf<UpdateCourseDTO>(
        except: ["id", "chapters"]
    );

    private static readonly FrozenSet<string> _chapterFields = JsonFieldsOf<UpdateCourseChapterDTO>(
        except: ["id", "materials"]
    );

    /// <summary>
    /// <c>contentId</c> is the server's to set: it is written when a material's content is first
    /// created, and letting a client assign it would let one material be pointed at another's
    /// content — including a content row belonging to a course the caller cannot open.
    /// </summary>
    private static readonly FrozenSet<string> _materialFields =
        JsonFieldsOf<UpdateCourseMaterialDTO>(except: ["id", "contentId"]);

    private static readonly FrozenSet<string> _contentFields =
        JsonFieldsOf<UpdateCourseMaterialContentDTO>(except: []);

    /// <summary>
    /// The collections inside a material's content that are keyed by id, so an entry can be added
    /// or removed on its own rather than the whole collection being replaced — which is what keeps
    /// two people attaching different files from undoing each other.
    /// </summary>
    private static readonly FrozenSet<string> _contentCollections = new[]
    {
        "medias",
        "nooTubeVideos",
        "workAssignments",
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    /// A work assignment's own fields. The other two collections hold bare id references, so there
    /// is nothing inside one of their entries to address.
    /// </summary>
    private static readonly FrozenSet<string> _workAssignmentFields =
        JsonFieldsOf<UpdateCourseWorkAssignmentDTO>(except: []);

    private readonly ICourseService _courseService;
    private readonly ICurrentUser _currentUser;

    public CourseCollaborationHandler(ICourseService courseService, ICurrentUser currentUser)
    {
        _courseService = courseService;
        _currentUser = currentUser;
    }

    public string RoomType => "course";

    /// <summary>
    /// The same roles the course's own PATCH endpoint admits. Answered here rather than by a
    /// policy in Core so a document type can differ without anything shared being edited.
    /// </summary>
    public async Task<bool> CanEditAsync(Ulid roomId, CancellationToken ct = default) =>
        _currentUser.IsInRole(UserRoles.Admin, UserRoles.Teacher)
        && await _courseService.GetByIdAsync(roomId, includeInactive: true) is not null;

    /// <summary>
    /// A course's own fields, a chapter, a material inside one, or a field of that material's
    /// content. Everything is addressed by id rather than by position, which is what makes an
    /// operation survive somebody else reordering the tree underneath it.
    /// </summary>
    public bool IsPathAllowed(string path) =>
        path.Split('/') switch
        {
            ["", var field] => _courseFields.Contains(field),
            ["", "chapters", var chapter] => IsId(chapter),
            ["", "chapters", var chapter, var field] => IsId(chapter)
                && _chapterFields.Contains(field),
            ["", "chapters", var chapter, "materials", var material] => IsId(chapter)
                && IsId(material),
            // Before the material-field arm below, which would otherwise swallow "content".
            ["", "chapters", var chapter, "materials", var material, _contentSegment] => IsId(
                chapter
            )
                && IsId(material),
            ["", "chapters", var chapter, "materials", var material, _contentSegment, var field] =>
                IsId(chapter) && IsId(material) && _contentFields.Contains(field),
            // One entry of a keyed collection inside the content, and a work assignment's own
            // fields inside that.
            [
                "",
                "chapters",
                var chapter,
                "materials",
                var material,
                _contentSegment,
                var collection,
                var entry
            ] => IsId(chapter)
                && IsId(material)
                && _contentCollections.Contains(collection)
                && IsId(entry),
            [
                "",
                "chapters",
                var chapter,
                "materials",
                var material,
                _contentSegment,
                "workAssignments",
                var entry,
                var field
            ] => IsId(chapter)
                && IsId(material)
                && IsId(entry)
                && _workAssignmentFields.Contains(field),
            ["", "chapters", var chapter, "materials", var material, var field] => IsId(chapter)
                && IsId(material)
                && _materialFields.Contains(field),
            _ => false,
        };

    /// <summary>
    /// Applies the draft: the tree first, then each touched material's content.
    ///
    /// The order is not incidental. A material drafted in this same session does not exist until
    /// the tree patch has run, and its content has to be attached to it afterwards.
    ///
    /// Content for a material the course no longer has is skipped rather than failed on: a draft
    /// holds the edits somebody made before somebody else removed that material, and one of those
    /// must not be able to make the whole draft unsaveable for everyone.
    /// </summary>
    public async Task SaveAsync(
        Ulid roomId,
        IReadOnlyList<CollaborationOp> ops,
        CancellationToken ct = default
    )
    {
        var (treeOps, contentOps) = Split(ops);

        if (treeOps.Count > 0)
        {
            await _courseService.UpdateAsync(roomId, ToDocument<UpdateCourseDTO>(treeOps));
        }

        foreach (var (materialId, materialOps) in contentOps)
        {
            await _courseService.UpdateMaterialContentAsync(
                roomId,
                materialId,
                ToDocument<UpdateCourseMaterialContentDTO>(materialOps)
            );
        }
    }

    /// <summary>
    /// Collapses the draft by dropping what later operations already settle, rather than by
    /// rebuilding the document and diffing it: a course spans two tables, and reassembling it here
    /// would mean reading all of it to shorten a list.
    ///
    /// Region by region, because path coverage stops at the boundary between them: replacing a
    /// whole material rewrites its title and its ordering, and says nothing at all about the
    /// content row it points at.
    /// </summary>
    public Task<IReadOnlyList<CollaborationOp>> CompactAsync(
        Ulid roomId,
        IReadOnlyList<CollaborationOp> ops,
        CancellationToken ct = default
    )
    {
        var compacted = ops.GroupBy(op => ContentMaterialOf(op.Path))
            .SelectMany(region => CollaborationOps.Deduplicate([.. region]))
            .ToList();

        return Task.FromResult<IReadOnlyList<CollaborationOp>>(compacted);
    }

    /// <summary>
    /// Which material's content a path belongs to, or null when it belongs to the tree. This is
    /// the boundary between the two entities a course is made of.
    /// </summary>
    private static Ulid? ContentMaterialOf(string path) =>
        path.Split('/') is ["", "chapters", _, "materials", var material, _contentSegment, ..]
        && Ulid.TryParse(material, out var materialId)
            ? materialId
            : null;

    /// <summary>
    /// Splits the draft into the course patch and one content patch per material, rewriting the
    /// content paths to what <see cref="UpdateCourseMaterialContentDTO"/> expects. Materials keep
    /// the order they were first touched in, so a replay reads the way the session ran.
    /// </summary>
    private static (
        List<CollaborationOp> Tree,
        List<KeyValuePair<Ulid, List<CollaborationOp>>> Content
    ) Split(IReadOnlyList<CollaborationOp> ops)
    {
        var tree = new List<CollaborationOp>();
        var content = new List<KeyValuePair<Ulid, List<CollaborationOp>>>();

        foreach (var op in ops)
        {
            var segments = op.Path.Split('/');

            if (
                segments is ["", "chapters", _, "materials", var material, _contentSegment, ..]
                && Ulid.TryParse(material, out var materialId)
            )
            {
                var group = content.FirstOrDefault(entry => entry.Key == materialId).Value;

                if (group is null)
                {
                    group = [];
                    content.Add(new KeyValuePair<Ulid, List<CollaborationOp>>(materialId, group));
                }

                // "/chapters/{c}/materials/{m}/content/pollId" becomes "/pollId"; the content
                // itself becomes "" — an operation on the whole document, which the patch
                // document does not accept, so it is dropped rather than sent to fail.
                var rest = string.Join('/', segments.Skip(6));

                if (rest.Length > 0)
                {
                    group.Add(op with { Path = $"/{rest}" });
                }

                continue;
            }

            tree.Add(op);
        }

        return (tree, content);
    }

    private static JsonPatchDocument<T> ToDocument<T>(IReadOnlyList<CollaborationOp> ops)
        where T : class
    {
        var document = new JsonPatchDocument<T> { Options = _json };

        foreach (var op in ops)
        {
            document.Operations.Add(
                new Operation<T>(op.Op, op.Path, from: null, value: ToValue(op.Value))
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

    private static bool IsId(string segment) => Ulid.TryParse(segment, out _);

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
