using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Noo.Api.GoogleSheetsIntegrations.Exports;

public class ExportParametersConverter : ValueConverter<ExportParameters, string>
{
    public ExportParametersConverter()
        : base(v => v.Serialize(), v => ExportParameters.Deserialize(v)) { }
}
