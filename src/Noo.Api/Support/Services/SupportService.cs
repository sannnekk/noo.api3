using AutoMapper;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Support.DTO;
using Noo.Api.Support.Models;
using SystemTextJsonPatch;

namespace Noo.Api.Support.Services;

[RegisterScoped(typeof(ISupportService))]
public class SupportService : ISupportService
{
    private readonly ISupportArticleRepository _articleRepository;
    private readonly ISupportCategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJsonPatchUpdateService _jsonPatchUpdateService;
    private readonly IMapper _mapper;

    public SupportService(
        IUnitOfWork unitOfWork,
        ISupportArticleRepository articleRepository,
        ISupportCategoryRepository categoryRepository,
        IJsonPatchUpdateService jsonPatchUpdateService,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _articleRepository = articleRepository;
        _categoryRepository = categoryRepository;
        _jsonPatchUpdateService = jsonPatchUpdateService;
        _mapper = mapper;
    }

    public async Task<Ulid> CreateArticleAsync(CreateSupportArticleDTO dto)
    {
        var model = _mapper.Map<SupportArticleModel>(dto);

        _articleRepository.Add(model);
        await _unitOfWork.CommitAsync();

        return model.Id;
    }

    public async Task<Ulid> CreateCategoryAsync(CreateSupportCategoryDTO dto)
    {
        var model = _mapper.Map<SupportCategoryModel>(dto);

        _categoryRepository.Add(model);
        await _unitOfWork.CommitAsync();

        return model.Id;
    }

    public async Task DeleteArticleAsync(Ulid articleId)
    {
        _articleRepository.DeleteById(articleId);
        await _unitOfWork.CommitAsync();
    }

    public async Task DeleteCategoryAsync(Ulid categoryId)
    {
        _categoryRepository.DeleteById(categoryId);
        await _unitOfWork.CommitAsync();
    }

    public Task<SupportArticleModel?> GetArticleAsync(Ulid articleId)
    {
        return _articleRepository
            .GetByIdAsync(articleId);
    }

    public Task<IEnumerable<SupportCategoryModel>> GetCategoryTreeAsync()
    {
        return _categoryRepository.GetCategoryTreeAsync(false);
    }

    public async Task UpdateArticleAsync(Ulid articleId, JsonPatchDocument<UpdateSupportArticleDTO> dto)
    {
        var model = await _articleRepository.GetByIdAsync(articleId);

        model.ThrowNotFoundIfNull();

        _jsonPatchUpdateService.ApplyPatch(model, dto);

        _articleRepository.Update(model);
        await _unitOfWork.CommitAsync();
    }

    public async Task UpdateCategoryAsync(Ulid categoryId, JsonPatchDocument<UpdateSupportCategoryDTO> dto)
    {
        var model = await _categoryRepository.GetByIdAsync(categoryId);

        model.ThrowNotFoundIfNull();

        _jsonPatchUpdateService.ApplyPatch(model, dto);

        _categoryRepository.Update(model);
        await _unitOfWork.CommitAsync();
    }
}
