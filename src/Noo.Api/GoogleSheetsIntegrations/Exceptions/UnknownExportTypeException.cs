using System.Net;
using Noo.Api.Core.Exceptions;
using Noo.Api.GoogleSheetsIntegrations.Types;

namespace Noo.Api.GoogleSheetsIntegrations.Exceptions;

/// <summary>
/// Error Code: GOOGLE_SHEETS_INTEGRATION.UNKNOWN_EXPORT_TYPE
/// Name: Неизвестный тип выгрузки
/// Description: Для выбранного типа выгрузки не найден обработчик
/// </summary>
public class UnknownExportTypeException : NooException
{
    public UnknownExportTypeException(GoogleSheetsIntegrationType type)
        : base($"Неизвестный тип выгрузки: {type}")
    {
        Id = "GOOGLE_SHEETS_INTEGRATION.UNKNOWN_EXPORT_TYPE";
        StatusCode = HttpStatusCode.BadRequest;
    }
}
