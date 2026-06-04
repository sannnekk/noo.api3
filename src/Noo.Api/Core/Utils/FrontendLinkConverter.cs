using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Noo.Api.Core.Utils;

public class FrontendLinkConverter : ValueConverter<FrontendLink, string>
{
    public FrontendLinkConverter() : base(
        v => v.Serialize(),
        v => FrontendLink.Deserialize(v))
    {
    }
}
