namespace Noo.Api.Media.Models;

public static class MediaEnumDbDataTypes
{
    public const string MediaCategory = "ENUM(" +
        "'UserAvatar'," +
        "'VideoCover'," +
        "'VideoRichText'," +
        "'CourseCover'," +
        "'CourseAttachment'," +
        "'CourseRichText'," +
        "'WorkRichText'," +
        "'ProfileBackground'," +
        "'AssignedWorkStudentRichText'," +
        "'AssignedWorkMentorRichText'," +
        "'AssignedWorkStudentCommentRichText'," +
        "'AssignedWorkMentorCommentRichText'," +
        "'HelpRichText'" +
        ")";

    public const string MediaStatus = "ENUM('Pending','Completed')";
}
