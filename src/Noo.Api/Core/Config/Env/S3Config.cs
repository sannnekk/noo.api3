namespace Noo.Api.Core.Config.Env;

public class S3Config : IConfig
{
    public static string SectionName => "S3";

    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}
