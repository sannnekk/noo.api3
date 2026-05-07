using System.Security.Claims;

namespace Noo.Api.Core.Security;

public interface IJwtService
{
    public (string, DateTime) GenerateToken(IEnumerable<Claim> claims);
}
