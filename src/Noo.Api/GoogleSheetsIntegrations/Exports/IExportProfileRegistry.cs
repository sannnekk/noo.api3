using Noo.Api.GoogleSheetsIntegrations.Types;

namespace Noo.Api.GoogleSheetsIntegrations.Exports;

public interface IExportProfileRegistry
{
    /// <summary>
    /// Resolves the profile that implements a given export type.
    /// </summary>
    /// <exception cref="Noo.Api.GoogleSheetsIntegrations.Exceptions.UnknownExportTypeException">
    /// No profile is registered for the given type.
    /// </exception>
    public IExportProfile Get(GoogleSheetsIntegrationType type);
}
