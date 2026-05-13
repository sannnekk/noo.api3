using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.Response;

namespace Noo.Api.Core.Request;

public abstract class ApiController : ControllerBase
{
    protected readonly IMapper _mapper;

    protected ApiController(IMapper mapper)
    {
        _mapper = mapper;
    }

    /// <summary>
    /// Sends a response with no content (HTTP 204 No Content).
    /// </summary>
    protected IActionResult SendResponse()
    {
        return NoContent();
    }

    /// <summary>
    /// Sends a response with the provided data mapped to the specified DTO type.
    /// If the data is null, it returns a NotFound result.
    /// </summary>
    protected IActionResult SendResponse<TModel, TDto>(TModel? data)
        where TModel : class
    {
        if (data == null)
        {
            return NotFound();
        }

        var dto = _mapper.Map<TDto>(data);

        return Ok(new ApiResponseDTO<TDto>(dto, null));
    }

    /// <summary>
    /// Sends a response with the provided search result, mapping the items to the specified DTO type.
    /// </summary>
    protected IActionResult SendResponse<TModel, TDto>(SearchResult<TModel> data)
        where TModel : BaseModel
        where TDto : class
    {
        var dto = _mapper.Map<IEnumerable<TDto>>(data.Items);
        var metadata = data.Metadata;

        return Ok(new ApiResponseDTO<IEnumerable<TDto>>(dto, metadata));
    }

    /// <summary>
    /// Sends a response with the provided data mapped to the specified DTO type.
    /// If the data is null, it returns a NotFound result.
    /// </summary>
    protected IActionResult SendResponse<TDto>(TDto? data)
        where TDto : class
    {
        if (data == null)
        {
            return NotFound();
        }

        return Ok(new ApiResponseDTO<TDto>(data, null));
    }

    /// <summary>
    /// Sends a response with the provided primitive data.
    /// If the data is null, it returns a NotFound result.
    /// </summary>
    protected IActionResult SendResponse<T>(T? data)
        where T : struct
    {
        if (data == null)
        {
            return NotFound();
        }

        return Ok(new ApiResponseDTO<T?>(data, null));
    }

    /// <summary>
    /// Sends a response with the provided ID, typically used for creation responses.
    /// (HTTP 201 Created)
    /// </summary>
    /// <remarks>
    /// Not compliant with RFC 9110, but used for simplicity in this API
    /// </remarks>
    protected IActionResult SendResponse(Ulid id)
    {
        // Not compliant with rfc but we do not need it
        return Created(
            string.Empty,
            new ApiResponseDTO<IdResponseDTO>(new IdResponseDTO(id), null)
        );
    }
}
