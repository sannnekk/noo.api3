using SystemTextJsonPatch;

namespace Noo.Api.Core.Request.Patching
{
    public interface IJsonPatchUpdateService
    {
        public void ApplyPatch<TEntity, TUpdateDto>(
            TEntity entity,
            JsonPatchDocument<TUpdateDto> patchDocument
        ) where TEntity : class where TUpdateDto : class;
    }
}
