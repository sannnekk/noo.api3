using Microsoft.AspNetCore.Mvc;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Response;
using Noo.Api.Core.Utils.Json;

namespace Noo.Api.Core.Initialization.ServiceCollection;

public static class ControllersAndJsonExtension
{
    public static void AddNooControllersAndConfigureJson(this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddJsonOptions(
                options => options.JsonSerializerOptions.Converters.Add(new HyphenLowerCaseStringEnumConverterFactory())
            )
            .ConfigureApiBehaviorOptions(
                options => options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(entry => entry.Value?.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value!.Errors.Select(error => error.Exception?.Message ?? error.ErrorMessage).ToArray()
                        );

                    var error = new BadRequestException
                    {
                        Payload = errors
                    };

                    var response = new ErrorApiResponseDTO(error.Serialize());
                    return new BadRequestObjectResult(response);
                }
            );
    }
}
