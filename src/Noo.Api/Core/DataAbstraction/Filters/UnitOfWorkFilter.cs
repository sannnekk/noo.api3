using Microsoft.AspNetCore.Mvc.Filters;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.DataAbstraction.Filters;

[RegisterScoped(typeof(UnitOfWorkFilter))]
public class UnitOfWorkFilter : IAsyncActionFilter
{
    private readonly IUnitOfWork _unitOfWork;

    public UnitOfWorkFilter(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next
    )
    {
        var resultContext = await next();

        if (resultContext.Exception == null || resultContext.ExceptionHandled)
        {
            await _unitOfWork.CommitAsync();
        }
    }
}
