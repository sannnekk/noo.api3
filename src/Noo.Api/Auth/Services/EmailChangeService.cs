using Noo.Api.Auth.Events;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Users.Services;

namespace Noo.Api.Auth.Services;

[RegisterScoped(typeof(IEmailChangeService))]
public class EmailChangeService : IEmailChangeService
{
    private readonly IUserRepository _userRepository;

    private readonly ITokenService _tokenService;

    private readonly IAuthEmailService _emailService;

    private readonly IAuthUrlGenerator _urlGenerator;

    private readonly IEventPublisher _events;

    public EmailChangeService(
        IUserRepository userRepository,
        ITokenService tokenService,
        IAuthEmailService emailService,
        IAuthUrlGenerator urlGenerator,
        IEventPublisher events
    )
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _emailService = emailService;
        _urlGenerator = urlGenerator;
        _events = events;
    }

    public async Task RequestAsync(Ulid userId, string newEmail)
    {
        var user = await _userRepository.GetByIdAsync(userId) ?? throw new NotFoundException();

        var exists = await _userRepository.ExistsByUsernameOrEmailAsync(null, newEmail);

        if (exists)
        {
            throw new EmailAlreadyExistsException();
        }

        var token = _tokenService.CreateToken(user.Id, TokenType.EmailChange, newEmail);
        var link = _urlGenerator.GenerateEmailChangeUrl(token.Token);

        await _emailService.SendEmailChangeEmailAsync(newEmail, user.Name, link);
    }

    public async Task ConfirmAsync(Ulid userId, string newEmail)
    {
        var user = await _userRepository.GetByIdAsync(userId) ?? throw new NotFoundException();

        var oldEmail = user.Email;

        user.Email = newEmail;

        _tokenService.DeleteTokens(user.Id, TokenType.EmailChange);

        await _events.PublishAsync(new UserEmailChangedEvent(user.Id, oldEmail, newEmail));
    }
}
