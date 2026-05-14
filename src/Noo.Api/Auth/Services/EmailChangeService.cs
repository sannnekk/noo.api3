using Noo.Api.Core.Exceptions.Http;
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

    public EmailChangeService(
        IUserRepository userRepository,
        ITokenService tokenService,
        IAuthEmailService emailService,
        IAuthUrlGenerator urlGenerator
    )
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _emailService = emailService;
        _urlGenerator = urlGenerator;
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

        user.Email = newEmail;
        _userRepository.Update(user);
        _tokenService.DeleteTokens(user.Id, TokenType.EmailChange);
    }
}
