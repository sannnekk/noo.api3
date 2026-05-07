namespace Noo.Api.Media.Access;

public sealed record MediaAccessDecision(bool Allowed, string? Reason = null)
{
    public static MediaAccessDecision Allow() => new(true);

    public static MediaAccessDecision Deny(string reason) => new(false, reason);
}
