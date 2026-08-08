namespace Noo.Api.SavedTasks;

public static class SavedTaskConfig
{
    /// <summary>
    /// Fewer cards than this on a subject and a quiz is not worth running: the
    /// same handful would come back every time.
    /// </summary>
    public const int MinQuizCardCount = 5;

    public const int MaxQuizCardCount = 50;
}
