using SystemTextJsonPatch;

namespace Noo.Api.Core.Request.Patching;

/// <summary>
/// Service for applying JSON Patch documents to entities.
/// </summary>
public interface IJsonPatchUpdateService
{
    /// <summary>
    /// Applies a JSON Patch document to an entity and returns the patched DTO
    /// so callers that need the post-patch state (e.g. to resolve relation ids)
    /// can use it without duplicating the patch flow.
    /// </summary>
    public TUpdateDto ApplyPatch<TEntity, TUpdateDto>(
        TEntity entity,
        JsonPatchDocument<TUpdateDto> patchDocument
    ) where TEntity : class where TUpdateDto : class;
}

