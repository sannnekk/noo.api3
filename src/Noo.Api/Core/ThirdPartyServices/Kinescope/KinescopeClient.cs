using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.ThirdPartyServices.Kinescope.Exceptions;
using Noo.Api.Core.ThirdPartyServices.Kinescope.Models;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.ThirdPartyServices.Kinescope;

[RegisterScoped(typeof(IKinescopeClient))]
public class KinescopeClient : IKinescopeClient
{
    public const string HttpClientName = "DefaultExternal";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KinescopeConfig _config;

    public KinescopeClient(IHttpClientFactory httpClientFactory, IOptions<KinescopeConfig> config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config.Value;
    }

    public async Task<CreateUploadResult> CreateUploadAsync(
        CreateUploadRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var payload = request with
        {
            ParentId = string.IsNullOrWhiteSpace(request.ParentId)
                ? _config.DefaultParentId
                : request.ParentId,
        };

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_config.UploaderBaseUrl}/v2/init"
        )
        {
            Content = JsonContent.Create(payload, options: KinescopeSerialization.Options),
        };

        var result = await SendAsync<CreateUploadResult>(message, cancellationToken);

        return result ?? throw new KinescopeException("Kinescope returned an empty upload response.");
    }

    public async Task<KinescopeVideo?> GetVideoAsync(
        string videoId,
        CancellationToken cancellationToken = default
    )
    {
        using var message = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_config.ApiBaseUrl}/v1/videos/{Uri.EscapeDataString(videoId)}"
        );

        return await SendAsync<KinescopeVideo>(message, cancellationToken, allowNotFound: true);
    }

    public async Task DeleteVideoAsync(string videoId, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{_config.ApiBaseUrl}/v1/videos/{Uri.EscapeDataString(videoId)}"
        );

        await SendAsync<object>(message, cancellationToken, allowNotFound: true);
    }

    public async Task<KinescopeAnalyticsOverview?> GetVideoAnalyticsOverviewAsync(
        string videoId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    )
    {
        var query =
            $"?video_id={Uri.EscapeDataString(videoId)}"
            + $"&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&group_time=day";

        using var message = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_config.ApiBaseUrl}/v1/analytics/overview{query}"
        );

        return await SendAsync<KinescopeAnalyticsOverview>(
            message,
            cancellationToken,
            allowNotFound: true
        );
    }

    private async Task<T?> SendAsync<T>(
        HttpRequestMessage message,
        CancellationToken cancellationToken,
        bool allowNotFound = false
    )
        where T : class
    {
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiToken);

        var client = _httpClientFactory.CreateClient(HttpClientName);

        HttpResponseMessage response;

        try
        {
            response = await client.SendAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is not KinescopeException)
        {
            throw new KinescopeException($"Failed to reach Kinescope: {ex.Message}");
        }

        using (response)
        {
            if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new KinescopeException(
                    $"Kinescope responded with {(int)response.StatusCode}: {body}"
                );
            }

            if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
            {
                return null;
            }

            var wrapper = await response.Content.ReadFromJsonAsync<KinescopeResponse<T>>(
                KinescopeSerialization.Options,
                cancellationToken
            );

            return wrapper?.Data;
        }
    }
}
