using Microsoft.AspNetCore.Components;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.System.Email;

[RegisterScoped(typeof(IEmailService))]
public class EmailService : IEmailService
{
    private readonly IEmailClient _client;

    private readonly IEmailRenderer _renderer;

    private readonly ILogger<EmailService> _logger;

    public EmailService(IEmailClient client, IEmailRenderer renderer, ILogger<EmailService> logger)
    {
        _client = client;
        _renderer = renderer;
        _logger = logger;
    }

    public async Task SendEmailAsync<TData, TTemplate>(Email<TData, TTemplate> email) where TTemplate : IComponent where TData : class
    {
        var renderedBody = await _renderer.RenderEmailAsync<TTemplate, TData>(email.Body);

        try
        {
            await _client.SendHtmlEmailAsync(
                email.FromEmail,
                email.FromName,
                email.ToEmail,
                email.ToName,
                email.Subject,
                renderedBody
            );
        }
        catch (Exception ex)
        {
            throw new CantSendEmailException(ex);
        }
    }
}
