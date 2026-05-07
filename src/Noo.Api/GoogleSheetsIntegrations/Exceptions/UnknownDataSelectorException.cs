using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.GoogleSheetsIntegrations.Exceptions;

/// <summary>
/// Error Code: GOOGLE_SHEETS_INTEGRATION.UNKNOWN_DATA_SELECTOR
/// Name: Неизвестный селектор данных
/// Description: Проверьте правильность указанного селектора данных
/// </summary>
public class UnknownDataSelectorException : NooException
{
    public UnknownDataSelectorException(string message = "Unknown data selector") : base(message)
    {
        StatusCode = HttpStatusCode.BadRequest;
        Id = "GOOGLE_SHEETS_INTEGRATION.UNKNOWN_DATA_SELECTOR";
    }
}
