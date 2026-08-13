namespace Noo.Api.Core.ThirdPartyServices.Google;

public static class GoogleDriveTags
{
    /// <summary>
    /// Written to the Drive file's appProperties so platform-generated spreadsheets are
    /// recognizable among a user's own files.
    /// </summary>
    public static readonly IEnumerable<string> SheetTags = ["НОО.Платформа"];
}
