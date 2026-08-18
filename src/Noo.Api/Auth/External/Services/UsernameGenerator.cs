using Noo.Api.Auth.External.Types;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Users.Services;

namespace Noo.Api.Auth.External.Services;

/// <summary>
/// The binding constraint is the frontend's own username validator: 3-20 characters of
/// [a-zA-Z0-9_-]. A name outside it would be rejected the first time the user opened
/// their profile form, so generated names stay well inside it.
/// </summary>
[RegisterScoped(typeof(IUsernameGenerator))]
public class UsernameGenerator : IUsernameGenerator
{
    private const int _minLength = 3;

    private const int _maxLength = 20;

    private const int _randomSuffixLength = 4;

    /// <summary>Short enough that every candidate below still fits <see cref="_maxLength"/>.</summary>
    private const int _seedMaxLength = _maxLength - _randomSuffixLength - 1;

    private const int _randomAttempts = 10;

    private const string _fallbackSeed = "user";

    private readonly IUserRepository _users;

    public UsernameGenerator(IUserRepository users)
    {
        _users = users;
    }

    public async Task<string> GenerateAsync(
        ExternalUserProfile profile,
        ExternalAuthProviderType provider
    )
    {
        var seed = BuildSeed(profile, provider);

        foreach (var candidate in BuildCandidates(seed))
        {
            if (!await _users.ExistsByUsernameOrEmailAsync(candidate, null))
            {
                return candidate;
            }
        }

        throw new NooException("Не удалось подобрать свободное имя пользователя.");
    }

    private static IEnumerable<string> BuildCandidates(string seed)
    {
        yield return seed;

        for (var suffix = 2; suffix <= 9; suffix++)
        {
            yield return $"{seed}{suffix}";
        }

        // 36^4 codes, but bounded so a pathological collision streak cannot hang the request.
        for (var attempt = 0; attempt < _randomAttempts; attempt++)
        {
            var code = RandomGenerator.GenerateReadableCode(_randomSuffixLength);

            yield return $"{seed}-{code.ToLowerInvariant()}";
        }
    }

    private static string BuildSeed(
        ExternalUserProfile profile,
        ExternalAuthProviderType provider
    )
    {
        string?[] sources =
        [
            profile.ProviderLogin,
            $"{profile.FirstName} {profile.LastName}",
            profile.DisplayName,
            EmailLocalPart(profile.Email),
            provider.ToString(),
        ];

        return sources.Select(Normalize).FirstOrDefault(seed => seed.Length >= _minLength)
            ?? _fallbackSeed;
    }

    private static string? EmailLocalPart(string? email)
    {
        var separator = email?.IndexOf('@') ?? -1;

        return separator > 0 ? email![..separator] : null;
    }

    /// <summary>Slug.Generate transliterates Cyrillic and emits only [a-z0-9-].</summary>
    private static string Normalize(string? source)
    {
        var slug = Slug.Generate(source ?? string.Empty);

        if (slug.Length > _seedMaxLength)
        {
            slug = slug[.._seedMaxLength];
        }

        return slug.Trim('-');
    }
}
