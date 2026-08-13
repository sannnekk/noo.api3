using Noo.Api.GoogleSheetsIntegrations.Exports;

namespace Noo.UnitTests.GoogleSheetsIntegrations;

public class ExportColumnsTests
{
    private sealed record Row(int? Score, int MaxScore, DateTime? Moment, bool Flag);

    [Fact]
    public void Percent_Rounds_To_Whole_Percent()
    {
        var column = ExportColumns.Percent<Row>("%", r => r.Score, r => r.MaxScore);

        Assert.Equal(50, column.Value(new Row(5, 10, null, false)));
        Assert.Equal(33, column.Value(new Row(1, 3, null, false)));
    }

    [Fact]
    public void Percent_Is_Blank_When_There_Is_No_Score_Yet()
    {
        var column = ExportColumns.Percent<Row>("%", r => r.Score, r => r.MaxScore);

        // An unchecked work must read as empty, not as a genuine zero.
        Assert.Null(column.Value(new Row(null, 10, null, false)));
    }

    [Fact]
    public void Percent_Is_Blank_When_Max_Score_Is_Zero()
    {
        var column = ExportColumns.Percent<Row>("%", r => r.Score, r => r.MaxScore);

        Assert.Null(column.Value(new Row(3, 0, null, false)));
    }

    [Fact]
    public void Date_Formats_As_Moscow_Wall_Clock()
    {
        var column = ExportColumns.Date<Row>("D", r => r.Moment);
        var moment = new DateTime(2026, 8, 13, 9, 5, 0, DateTimeKind.Unspecified);

        Assert.Equal("13.08.2026 09:05", column.Value(new Row(null, 0, moment, false)));
    }

    [Fact]
    public void Date_Can_Omit_The_Time()
    {
        var column = ExportColumns.Date<Row>("D", r => r.Moment, includeTime: false);
        var moment = new DateTime(2026, 8, 13, 9, 5, 0, DateTimeKind.Unspecified);

        Assert.Equal("13.08.2026", column.Value(new Row(null, 0, moment, false)));
    }

    [Fact]
    public void Date_Is_Blank_When_Missing()
    {
        var column = ExportColumns.Date<Row>("D", r => r.Moment);

        Assert.Null(column.Value(new Row(null, 0, null, false)));
    }

    [Fact]
    public void Bool_Renders_In_Russian()
    {
        var column = ExportColumns.Bool<Row>("B", r => r.Flag);

        Assert.Equal("Да", column.Value(new Row(null, 0, null, true)));
        Assert.Equal("Нет", column.Value(new Row(null, 0, null, false)));
    }
}
