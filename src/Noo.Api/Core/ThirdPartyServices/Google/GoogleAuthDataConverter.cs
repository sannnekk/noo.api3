using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Noo.Api.Core.ThirdPartyServices.Google;

public class GoogleAuthDataConverter : ValueConverter<GoogleAuthData, string>
{
    public GoogleAuthDataConverter() : base(
        v => v.Serialize(),
        v => GoogleAuthData.Deserialize(v))
    {
    }
}
