namespace Noo.Api.Works.Models;

public static class WorkEnumDbDataTypes
{
    public const string WorkType = "ENUM('Test','MiniTest','Phrase','TrialWork','SecondPart')";
    public const string WorkTaskType = "ENUM('Word','Text','Essay','FinalEssay','Dictation')";
    public const string WorkTaskCheckSteategy = "ENUM('Manual', 'ExactMatchOrZero', 'ExactMatchWithWrongCharacter', 'MultipleChoice', 'Sequence')";
}
