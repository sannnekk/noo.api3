using Microsoft.EntityFrameworkCore;
using Noo.Api.Calendar.Models;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Calendar.Services;

[RegisterScoped(typeof(ICalendarEventRepository))]
public class CalendarEventRepository : Repository<CalendarEventModel>, ICalendarEventRepository
{
    public CalendarEventRepository(NooDbContext dbContext) : base(dbContext)
    {
    }

    public Task<CalendarEventModel?> GetEventAsync(Ulid userId, Ulid eventId)
    {
        return Context.Set<CalendarEventModel>()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Id == eventId);
    }
}
