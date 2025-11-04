using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.Security;

[RegisterTransient(typeof(IHashService))]
public class HashService : IHashService
{
    private readonly HashAlgorithm _hashAlgorithm = SHA256.Create();

    private readonly string _secret;

    public HashService(IOptions<AppConfig> appConfig)
    {
        _secret = appConfig.Value.HashSecret;
    }

    public string Hash(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(_secret + input);
        var hashBytes = _hashAlgorithm.ComputeHash(inputBytes);

        return Convert.ToBase64String(hashBytes);
    }

    public bool Verify(string input, string hash)
    {
        return string.Equals(Hash(input), hash, StringComparison.Ordinal);
    }
}
