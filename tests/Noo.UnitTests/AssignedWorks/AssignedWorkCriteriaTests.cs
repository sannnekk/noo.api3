using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Specifications;
using Noo.Api.Works.Types;

namespace Noo.UnitTests.AssignedWorks;

/// <summary>
/// "Takes part in this work" is written twice — once for queries, once for works already in
/// hand. These check the two never drift apart.
/// </summary>
public class AssignedWorkCriteriaTests
{
    private static readonly Ulid Student = Ulid.NewUlid();
    private static readonly Ulid MainMentor = Ulid.NewUlid();
    private static readonly Ulid HelperMentor = Ulid.NewUlid();
    private static readonly Ulid Outsider = Ulid.NewUlid();

    private static AssignedWorkModel Work() => new()
    {
        Title = "A",
        Type = WorkType.Test,
        Attempt = 1,
        StudentId = Student,
        MainMentorId = MainMentor,
        HelperMentorId = HelperMentor,
        MaxScore = 10,
    };

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void The_Query_And_The_In_Memory_Rule_Agree(int who, bool expected)
    {
        var userId = new[] { Student, MainMentor, HelperMentor, Outsider }[who];
        var work = Work();

        var byQuery = AssignedWorkCriteria.ParticipatedBy(userId).Compile()(work);

        Assert.Equal(expected, byQuery);
        Assert.Equal(expected, work.IsParticipant(userId));
    }

    [Fact]
    public void A_Work_Without_Mentors_Belongs_To_Its_Student_Alone()
    {
        var work = Work();
        work.MainMentorId = null;
        work.HelperMentorId = null;

        Assert.True(work.IsParticipant(Student));
        Assert.False(work.IsParticipant(MainMentor));
        Assert.False(AssignedWorkCriteria.ParticipatedBy(MainMentor).Compile()(work));
    }
}
