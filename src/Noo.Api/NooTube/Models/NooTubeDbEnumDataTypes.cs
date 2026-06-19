namespace Noo.Api.NooTube.Models;

public static class NooTubeDbEnumDataTypes
{
    public const string NooTubeServiceType = "ENUM('kinescope')";
    public const string VideoState = "ENUM('NotUploaded, Uploading, Encoding, Uploaded, Published')";
    public const string VideoReaction =
        "ENUM('Like', 'Dislike', 'Heart', 'Laugh', 'Sad', 'Mindblowing')";
}
