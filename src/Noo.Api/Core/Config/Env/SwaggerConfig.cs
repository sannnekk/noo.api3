namespace Noo.Api.Core.Config.Env;

public class SwaggerConfig : IConfig
{
    public static string SectionName => "Swagger";

    public string Title { get; init; } = "Noo API";

    public string Description { get; init; } = "Noo API Documentation";

    public string Endpoint { get; init; } = "/swagger/v1/swagger.json";

    public string RoutePrefix { get; init; } = "api-docs";

    public string Version { get; init; } = "v1";

    public bool EnableUI { get; set; } = true;
}
