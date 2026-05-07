namespace Noo.Api.Media.Types;

public enum MediaStatus
{
    /// <summary>Upload URL has been issued; the file may or may not be in S3 yet.</summary>
    Pending,

    /// <summary>Upload has been confirmed by the client; metadata is final.</summary>
    Completed,
}
