namespace Noo.Api.Core.ThirdPartyServices.Google;

/// <summary>
/// Simplified in-memory tabular data structure used to transfer data to Google Sheets.
/// </summary>
public class DataTable
{
    public IList<string> Headers { get; }
    public IReadOnlyList<IReadOnlyList<object?>> Rows => _rows;

    private readonly List<IReadOnlyList<object?>> _rows = [];

    public DataTable(IEnumerable<string> headers)
    {
        Headers = headers.ToArray();
    }

    public void AddRow(object?[] values)
    {
        if (values.Length != Headers.Count)
        {
            throw new ArgumentException($"Row length {values.Length} doesn't match headers count {Headers.Count}");
        }

        _rows.Add(values);
    }

    internal void AddColumn(string title)
    {
        Headers.Add(title);
    }
}
