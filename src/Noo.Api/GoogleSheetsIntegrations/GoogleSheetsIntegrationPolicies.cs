using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.GoogleSheetsIntegrations;

/// <summary>
/// Coarse role gates only. Which data a user may actually export — a mentor is limited to their
/// own students' works — is decided by the export profile, because that check depends on the
/// requested parameters and has to be repeated on every scheduled rerun.
/// </summary>
public class GoogleSheetsIntegrationPolicies : IPolicyRegistrar
{
    public const string CanGetGoogleSheetsIntegrations = nameof(CanGetGoogleSheetsIntegrations);
    public const string CanCreateGoogleSheetsIntegration = nameof(
        CanCreateGoogleSheetsIntegration
    );
    public const string CanDeleteGoogleSheetsIntegration = nameof(
        CanDeleteGoogleSheetsIntegration
    );
    public const string CanRunGoogleSheetsIntegration = nameof(CanRunGoogleSheetsIntegration);
    public const string CanUpdateGoogleSheetsIntegration = nameof(
        CanUpdateGoogleSheetsIntegration
    );

    private static readonly string[] _roles =
    [
        nameof(UserRoles.Admin),
        nameof(UserRoles.Teacher),
        nameof(UserRoles.Mentor),
    ];

    public void RegisterPolicies(AuthorizationOptions options)
    {
        foreach (
            var policy in new[]
            {
                CanGetGoogleSheetsIntegrations,
                CanCreateGoogleSheetsIntegration,
                CanDeleteGoogleSheetsIntegration,
                CanRunGoogleSheetsIntegration,
                CanUpdateGoogleSheetsIntegration,
            }
        )
        {
            options.AddPolicy(
                policy,
                builder => builder.RequireRole(_roles).RequireNotBlocked()
            );
        }
    }
}
