using AutoFilterer.Types;
using AutoMapper;
using Noo.Api.Calendar.DTO;
using Noo.Api.Calendar.Filters;
using Noo.Api.Calendar.Models;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Calendar.Services;

[RegisterScoped(typeof(ICalendarService))]
public class CalendarService : ICalendarService
{
    private readonly ICalendarEventRepository _calendarEventRepository;

    private readonly IUnitOfWork _unitOfWork;

    private readonly IMapper _mapper;

    public CalendarService(IUnitOfWork unitOfWork, ICalendarEventRepository calendarEventRepository, IMapper mapper)
    {
        _calendarEventRepository = calendarEventRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<Ulid> CreateCalendarEventAsync(Ulid userId, CreateCalendarEventDTO dto)
    {
        var calendarEvent = _mapper.Map<CalendarEventModel>(dto);
        calendarEvent.UserId = userId;

        _calendarEventRepository.Add(calendarEvent);
        return _unitOfWork.CommitAsync()
            .ContinueWith(_ => calendarEvent.Id);
    }

    public async Task DeleteCalendarEventAsync(Ulid userId, Ulid eventId)
    {
        var calendarEvent = await _calendarEventRepository.GetEventAsync(userId, eventId);

        if (calendarEvent == null)
        {
            throw new NotFoundException();
        }

        _calendarEventRepository.DeleteById(eventId);
        await _unitOfWork.CommitAsync();
    }

    public Task<SearchResult<CalendarEventModel>> GetCalendarEventsAsync(Ulid userId, int year, int month)
    {
        var filter = new CalendarEventFilter()
        {
            UserId = userId,
            StartDateTime = new Range<DateTime>(
                new DateTime(year, month, 1),
                new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59)
            )
        };

        return _calendarEventRepository.GetManyAsync(filter);
    }
}
