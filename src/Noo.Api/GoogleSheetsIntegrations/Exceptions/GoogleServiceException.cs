using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.GoogleSheetsIntegrations.Exceptions;

public class GoogleServiceException : NooException
{
    public GoogleServiceException(string message = "Problem with google services occured.") : base(message)
    {
        StatusCode = HttpStatusCode.InternalServerError;
        Id = "google_sheets_integration.google_problem";
    }
}
