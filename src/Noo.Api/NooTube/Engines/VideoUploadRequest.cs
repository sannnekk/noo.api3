namespace Noo.Api.NooTube.Engines;

public record VideoUploadRequest
{
    public required string Title { get; init; }

    public string? Description { get; init; }

    public required long FileSize { get; init; }

    public string? FileName { get; init; }
}
