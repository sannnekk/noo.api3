using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.GoogleSheetsIntegrations.Exceptions;

public class UnknownDataSelectorException : NooException
{
    public UnknownDataSelectorException(string message = "Unknown data selector") : base(message)
    {
        StatusCode = HttpStatusCode.BadRequest;
        Id = "google_sheets_integration.unknown_data_selector";
    }
}
