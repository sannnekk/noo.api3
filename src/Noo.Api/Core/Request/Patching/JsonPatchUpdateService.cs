using AutoMapper;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Core.Utils.Json;
using SystemTextJsonPatch;

namespace Noo.Api.Core.Request.Patching;

[RegisterScoped(typeof(IJsonPatchUpdateService))]
public class JsonPatchUpdateService : IJsonPatchUpdateService
{
    private readonly IMapper _mapper;

    public JsonPatchUpdateService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public void ApplyPatch<TEntity, TUpdateDto>(
        TEntity entity,
        JsonPatchDocument<TUpdateDto> patchDocument
    ) where TEntity : class where TUpdateDto : class
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(patchDocument);

        var modelState = new ModelStateDictionary();
        var dto = _mapper.Map<TUpdateDto>(entity);

        patchDocument.ApplyToAndValidate(dto, modelState);

        modelState.ThrowValidationExceptionIfInvalid();

        _mapper.Map(dto, entity);
    }
}
