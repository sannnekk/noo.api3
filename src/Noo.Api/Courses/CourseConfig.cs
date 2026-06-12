namespace Noo.Api.Courses;

public static class CourseConfig
{
    /// <summary>
	/// Maximum allowed depth for course chapter tree structures.
	/// </summary>
    public const int MaxChapterTreeDepth = 5;

    /// <summary>
    /// Run interval for the course publishing and work assignment deactivation background jobs.
    /// </summary>
    public static readonly TimeSpan BackgroundJobInterval = TimeSpan.FromHours(1);
}
