using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.GoogleSheetsIntegrations.Exceptions;

/// <summary>
/// Error Code: GOOGLE_SHEETS_INTEGRATION.GOOGLE_PROBLEM
/// Name: Ошибка Google Sheets
/// Description: Возникла ошибка при работе с Google Sheets. Попробуйте позже
/// </summary>
public class GoogleServiceException : NooException
{
    public GoogleServiceException(string message = "Problem with google services occured.") : base(message)
    {
        StatusCode = HttpStatusCode.InternalServerError;
        Id = "GOOGLE_SHEETS_INTEGRATION.GOOGLE_PROBLEM";
    }
}
