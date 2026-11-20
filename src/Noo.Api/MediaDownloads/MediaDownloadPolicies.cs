using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.MediaDownloads;

public class MediaDownloadPolicies : IPolicyRegistrar
{
    public const string CanGetMaterialFileDownloads = nameof(CanGetMaterialFileDownloads);

    public void RegisterPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(
            CanGetMaterialFileDownloads,
            policy =>
            {
                policy
                    .RequireRole(
                        nameof(UserRoles.Admin),
                        nameof(UserRoles.Teacher),
                        nameof(UserRoles.Assistant)
                    )
                    .RequireNotBlocked();
            }
        );
    }
}
