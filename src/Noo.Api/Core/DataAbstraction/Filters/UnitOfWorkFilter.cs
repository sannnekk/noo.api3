using Microsoft.AspNetCore.Mvc.Filters;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.DataAbstraction.Filters;

[RegisterScoped(typeof(UnitOfWorkFilter))]
public class UnitOfWorkFilter : IAsyncActionFilter
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventCollector _eventCollector;

    public UnitOfWorkFilter(IUnitOfWork unitOfWork, IDomainEventCollector eventCollector)
    {
        _unitOfWork = unitOfWork;
        _eventCollector = eventCollector;
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

            // Only now are the events' facts actually true.
            await _eventCollector.FlushAsync();
        }
        else
        {
            _eventCollector.Discard();
        }
    }
}
