namespace Noo.Api.Core.Security;

public interface IHashService
{
    public string Hash(string input);
    public bool Verify(string input, string hash);
    public bool VerifyPassword(string passwordToCheck, string passwordHash);
}
