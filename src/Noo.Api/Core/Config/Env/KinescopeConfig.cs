using System.ComponentModel.DataAnnotations;

namespace Noo.Api.Core.Config.Env;

public class KinescopeConfig : IConfig
{
    public static string SectionName => "Kinescope";

    [Required]
    public required string ApiToken { get; set; }

    [Required]
    public required string DefaultParentId { get; set; }

    public string ApiBaseUrl { get; set; } = "https://api.kinescope.io";

    public string UploaderBaseUrl { get; set; } = "https://uploader.kinescope.io";

    public string? WebhookSecret { get; set; }
}
