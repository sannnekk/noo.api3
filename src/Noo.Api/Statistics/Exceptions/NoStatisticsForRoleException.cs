using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.Statistics.Exceptions;

public class NoStatisticsForRoleException : NooException
{
    public NoStatisticsForRoleException()
        : base("No statistics available for the specified user role.")
    {
        StatusCode = HttpStatusCode.NotFound;
        Id = "statistics.no_statistics_for_role";
    }
}
