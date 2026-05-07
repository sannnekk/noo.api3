using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.Statistics.Exceptions;

/// <summary>
/// Error Code: STATISTICS.NO_STATISTICS_FOR_ROLE
/// Name: Нет статистики для роли
/// Description: Статистика недоступна для указанной роли пользователя
/// </summary>
public class NoStatisticsForRoleException : NooException
{
    public NoStatisticsForRoleException()
        : base("No statistics available for the specified user role.")
    {
        StatusCode = HttpStatusCode.NotFound;
        Id = "STATISTICS.NO_STATISTICS_FOR_ROLE";
    }
}
