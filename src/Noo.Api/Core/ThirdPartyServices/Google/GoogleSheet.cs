namespace Noo.Api.Core.ThirdPartyServices.Google;

public class GoogleSheet
{
    public string? SpreadsheetId { get; internal set; }
    public string Name { get; }

    private DataTable? _table;
    private IEnumerable<string> _tags = Array.Empty<string>();
    private bool _tableDirty;
    private bool _tagsDirty;

    public GoogleSheet(string name)
    {
        Name = name;
    }

    public void AddTable(DataTable data)
    {
        _table = data;
        _tableDirty = true;
    }

    public void AddTags(IEnumerable<string> sheetTags)
    {
        _tags = sheetTags.ToArray();
        _tagsDirty = true;
    }

    public void UpdateTable(DataTable data)
    {
        _table = data;
        _tableDirty = true;
    }

    internal (DataTable? table, bool tableDirty, IEnumerable<string> tags, bool tagsDirty) Snapshot()
    {
        return (_table, _tableDirty, _tags, _tagsDirty);
    }

    internal void MarkClean()
    {
        _tableDirty = false;
        _tagsDirty = false;
    }
}

