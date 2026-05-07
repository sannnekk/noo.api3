using AutoFilterer.Types;
using AutoMapper;
using Noo.Api.Calendar.DTO;
using Noo.Api.Calendar.Filters;
using Noo.Api.Calendar.Models;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Calendar.Services;

[RegisterScoped(typeof(ICalendarService))]
public class CalendarService : ICalendarService
{
    private readonly ICalendarEventRepository _calendarEventRepository;

    private readonly IMapper _mapper;

    public CalendarService(ICalendarEventRepository calendarEventRepository, IMapper mapper)
    {
        _calendarEventRepository = calendarEventRepository;
        _mapper = mapper;
    }

    public Ulid CreateCalendarEvent(Ulid userId, CreateCalendarEventDTO dto)
    {
        // TODO: refactor!!!
        var calendarEvent = _mapper.Map<CalendarEventModel>(dto);
        calendarEvent.UserId = userId;

        _calendarEventRepository.Add(calendarEvent);
        return calendarEvent.Id;
    }

    public void DeleteCalendarEvent(Ulid userId, Ulid eventId)
    {
        _calendarEventRepository.DeleteById(eventId);
    }

    public Task<SearchResult<CalendarEventModel>> GetCalendarEventsAsync(
        Ulid userId,
        int year,
        int month
    )
    {
        var filter = new CalendarEventFilter()
        {
            UserId = userId,
            StartDateTime = new Range<DateTime>(
                new DateTime(year, month, 1),
                new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59)
            ),
        };

        return _calendarEventRepository.GetManyAsync(filter);
    }
}
