namespace Noo.Api.Polls.Types;

public static class PollFileAnswerLimits
{
    /// <summary>
    /// How many files a <see cref="PollQuestionType.Files"/> question accepts when its
    /// author did not narrow it down, and the ceiling they can raise it to.
    /// </summary>
    public const int MaxFileCount = 10;
}
