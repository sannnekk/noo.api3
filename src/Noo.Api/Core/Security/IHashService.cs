namespace Noo.Api.Core.Security;

public interface IHashService
{
    public string Hash(string input);
    public bool Verify(string input, string hash);
    /// <summary>A null hash means the account has no password, so verification fails rather than throws.</summary>
    public bool VerifyPassword(string passwordToCheck, string? passwordHash);
}
