namespace Noo.Api.GoogleSheetsIntegrations.Models;

public static class GoogleSheetsIntegrationEnumDataDbTypes
{
    public const string GoogleSheetsIntegrationStatus = "ENUM('Active', 'Inactive', 'Error')";

    public const string GoogleIntegrationTypes =
        "ENUM('Users', 'Courses', 'PollResults', 'AssignedWorks')";

    public const string GoogleSheetsIntegrationSchedule =
        "ENUM('Manual', 'Hourly', 'Daily', 'Weekly')";

    public const string GoogleSheetsIntegrationRunState = "ENUM('Idle', 'Queued', 'Running')";
}
