namespace Noo.Api.NooTube.Engines;

public record VideoUploadTicket
{
    public required string ExternalId { get; init; }

    public required string UploadUrl { get; init; }
}
