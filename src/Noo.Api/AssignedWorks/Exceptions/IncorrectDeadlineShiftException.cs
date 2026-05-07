using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.AssignedWorks.Exceptions;

/// <summary>
/// Error Code: ASSIGNED_WORK.INCORRECT_DEADLINE_SHIFT
/// Name: Некорректный сдвиг дедлайна
/// Description: Проверьте правильность указанного значения сдвига дедлайна
/// </summary>
public class IncorrectDeadlineShiftException : NooException
{
    public IncorrectDeadlineShiftException()
        : base("The deadline shift is incorrect.")
    {
        Id = "ASSIGNED_WORK.INCORRECT_DEADLINE_SHIFT";
        StatusCode = HttpStatusCode.BadRequest;
    }
}
