namespace Noo.Api.Auth.External.Types;

/// <summary>
/// A provider's answer, normalized. Everything but <see cref="SubjectId"/> is optional
/// because scopes are grantable and users decline them.
/// </summary>
public sealed record ExternalUserProfile
{
    /// <summary>The provider's stable user id. Yandex <c>id</c>, VK <c>user_id</c>.</summary>
    public required string SubjectId { get; init; }

    public string? Email { get; init; }

    /// <summary>Gates auto-linking an existing account by email.</summary>
    public bool EmailIsVerified { get; init; }

    public string? DisplayName { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? AvatarUrl { get; init; }

    /// <summary>Provider-side login, when there is one. Seeds username generation.</summary>
    public string? ProviderLogin { get; init; }
}
