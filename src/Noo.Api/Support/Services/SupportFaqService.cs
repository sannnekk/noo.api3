using AutoMapper;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Support.DTO;
using Noo.Api.Support.Filters;
using Noo.Api.Support.Models;
using SystemTextJsonPatch;

namespace Noo.Api.Support.Services;

[RegisterScoped(typeof(ISupportFaqService))]
public class SupportFaqService : ISupportFaqService
{
    private readonly ISupportFaqItemRepository _faqItemRepository;
    private readonly IJsonPatchUpdateService _jsonPatchUpdateService;
    private readonly IMapper _mapper;

    public SupportFaqService(
        ISupportFaqItemRepository faqItemRepository,
        IJsonPatchUpdateService jsonPatchUpdateService,
        IMapper mapper
    )
    {
        _faqItemRepository = faqItemRepository;
        _jsonPatchUpdateService = jsonPatchUpdateService;
        _mapper = mapper;
    }

    public Ulid CreateItem(CreateSupportFaqItemDTO dto)
    {
        var model = _mapper.Map<SupportFaqItemModel>(dto);

        _faqItemRepository.Add(model);

        return model.Id;
    }

    public void DeleteItem(Ulid itemId)
    {
        _faqItemRepository.DeleteById(itemId);
    }

    public Task<SearchResult<SupportFaqItemModel>> GetItemsAsync(SupportFaqItemFilter filter)
    {
        return _faqItemRepository.SearchAsync(filter);
    }

    public async Task UpdateItemAsync(
        Ulid itemId,
        JsonPatchDocument<UpdateSupportFaqItemDTO> dto
    )
    {
        var model = await _faqItemRepository.GetByIdAsync(itemId);

        model.ThrowNotFoundIfNull();

        _jsonPatchUpdateService.ApplyPatch(model, dto);
    }
}
