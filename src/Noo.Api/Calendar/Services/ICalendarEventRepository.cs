using Noo.Api.Calendar.Models;
using Noo.Api.Core.DataAbstraction.Db;

namespace Noo.Api.Calendar.Services;

public interface ICalendarEventRepository : IRepository<CalendarEventModel>
{
    public Task<CalendarEventModel?> GetEventAsync(Ulid userId, Ulid eventId);
}
