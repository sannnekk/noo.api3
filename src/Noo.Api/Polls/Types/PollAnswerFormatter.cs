namespace Noo.Api.Polls.Types;

/// <summary>
/// Renders a poll answer as human-readable text. Lives apart from the model so that read paths
/// which project answers flat — the Google Sheets export in particular — format them exactly
/// the way the model does.
/// </summary>
public static class PollAnswerFormatter
{
    public static string? Stringify(PollAnswerValue value, IEnumerable<string>? mediaNames = null)
    {
        switch (value.Type)
        {
            case PollQuestionType.Text:
            case PollQuestionType.SingleChoice:
            case PollQuestionType.Number:
                return value.Value?.ToString();
            case PollQuestionType.MultipleChoice:
                return value.Value is IEnumerable<string> choices
                    ? string.Join(", ", choices)
                    : null;
            case PollQuestionType.Date:
                return value.Value is DateTime date ? date.ToString("yyyy.MM.dd") : null;
            case PollQuestionType.DateTime:
                return value.Value is DateTimeOffset dateTimeOffset
                    ? dateTimeOffset.ToString("yyyy.MM.dd HH:mm:ss zzz")
                    : null;
            case PollQuestionType.Checkbox:
                return value.Value is bool boolValue ? (boolValue ? "Да" : "Нет") : null;
            case PollQuestionType.Rating:
                return value.Value is int rating ? rating.ToString() : null;
            case PollQuestionType.Files:
                // Exports are read by people, and a presigned URL would be dead by the
                // time anyone opened the sheet, so the file names stand in for the files.
                return mediaNames is not null && mediaNames.Any()
                    ? string.Join(", ", mediaNames)
                    : null;
            default:
                return "<Unknown question type>";
        }
    }
}
