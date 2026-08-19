using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.Courses.Exceptions;

/// <summary>
/// Error Code: COURSE.STUDENT_ALREADY_ON_COURSE
/// Name: Ученик уже на курсе
/// Description: У ученика уже есть доступ к этому курсу
/// </summary>
public class StudentAlreadyOnCourseException : NooException
{
    public StudentAlreadyOnCourseException()
        : base("The student already has a membership on this course.")
    {
        Id = "COURSE.STUDENT_ALREADY_ON_COURSE";
        StatusCode = HttpStatusCode.Conflict;
    }
}
