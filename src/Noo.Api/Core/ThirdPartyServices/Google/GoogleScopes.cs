using Google.Apis.Drive.v3;

namespace Noo.Api.Core.ThirdPartyServices.Google;

public static class GoogleScopes
{
    public const string OpenId = "openid";
    public const string Email = "email";

    /// <summary>
    /// Grants access only to files this application created itself. Deliberately narrower than
    /// the full <c>spreadsheets</c> scope: <c>drive.file</c> is non-sensitive and needs no Google
    /// app verification, at the cost of not being able to write to a pre-existing spreadsheet.
    /// </summary>
    public static readonly string DriveFile = DriveService.Scope.DriveFile;

    public static readonly string[] Required = [OpenId, Email, DriveFile];
}
