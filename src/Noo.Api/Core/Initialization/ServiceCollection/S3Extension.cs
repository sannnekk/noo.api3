using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;

namespace Noo.Api.Core.Initialization.ServiceCollection;

public static class S3Extension
{
    public static void AddS3Storage(this IServiceCollection services)
    {
        // Bind S3Config is expected to be registered via LoadEnvConfigsExtension
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<S3Config>>().Value;
            var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region)
            };
            return new AmazonS3Client(credentials, config);
        });
    }
}
