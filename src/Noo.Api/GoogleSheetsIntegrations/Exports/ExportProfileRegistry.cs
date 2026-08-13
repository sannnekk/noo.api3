using Noo.Api.Core.Utils.DI;
using Noo.Api.GoogleSheetsIntegrations.Exceptions;
using Noo.Api.GoogleSheetsIntegrations.Types;

namespace Noo.Api.GoogleSheetsIntegrations.Exports;

[RegisterScoped(typeof(IExportProfileRegistry))]
public class ExportProfileRegistry : IExportProfileRegistry
{
    private readonly Dictionary<GoogleSheetsIntegrationType, IExportProfile> _profiles;

    public ExportProfileRegistry(IEnumerable<IExportProfile> profiles)
    {
        _profiles = profiles.ToDictionary(profile => profile.Type);
    }

    public IExportProfile Get(GoogleSheetsIntegrationType type)
    {
        return _profiles.TryGetValue(type, out var profile)
            ? profile
            : throw new UnknownExportTypeException(type);
    }
}
