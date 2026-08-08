namespace Noo.Api.UserHistory.Types;

/// <summary>
/// Which side of an entry the user being viewed is on.
/// </summary>
public enum UserHistoryPerspective
{
    /// <summary>
    /// Things that happened to the user.
    /// </summary>
    Subject,

    /// <summary>
    /// Things the user did to others.
    /// </summary>
    Actor,
}
