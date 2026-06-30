using Noo.Api.Calendar.DTO;
using Noo.Api.Calendar.Models;
using Noo.Api.Core.DataAbstraction.Db;

namespace Noo.Api.Calendar.Services;

public interface ICalendarService
{
    public Task<SearchResult<CalendarEventModel>> GetCalendarEventsAsync(
        Ulid userId,
        int year,
        int month
    );
    public Ulid CreateCalendarEvent(Ulid userId, CreateCalendarEventDTO dto);
    public Task DeleteCalendarEventAsync(Ulid userId, Ulid eventId);
}
