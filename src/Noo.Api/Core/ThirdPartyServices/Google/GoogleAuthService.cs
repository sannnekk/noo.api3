using Noo.Api.Core.Utils.DI;
using Google.Apis.Sheets.v4;
using Google.Apis.Drive.v3;

namespace Noo.Api.Core.ThirdPartyServices.Google;

[RegisterSingleton(typeof(IGoogleAuthService))]
public class GoogleAuthService : IGoogleAuthService
{
    private static readonly string[] _defaultScopes =
    [
        SheetsService.Scope.Spreadsheets,
        DriveService.Scope.DriveFile // to create/update files owned by the app
    ];

    public Task<GoogleAuth> AuthenticateAsync(GoogleAuthData googleAuthData)
    {
        var credential = GoogleAuth.BuildCredential(googleAuthData, _defaultScopes);
        var auth = new GoogleAuth(credential);
        return Task.FromResult(auth);
    }
}

