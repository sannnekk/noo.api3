using System.Text.Json;
using System.Text.Json.Serialization;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Core.Utils.Json;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.Platform.DTO;
using Noo.Api.Platform.Models;
using Noo.Api.Platform.Types;
using SystemTextJsonPatch;

namespace Noo.Api.Platform.Services;

[RegisterScoped(typeof(IPlatformService))]
public class PlatformService : IPlatformService
{
    private const string _changelogResourceName = "Noo.Api.Platform.changelog.json";

    private readonly IPlatformSettingsRepository _settingsRepository;
    private readonly IJsonPatchUpdateService _jsonPatchUpdateService;

    public PlatformService(
        IPlatformSettingsRepository settingsRepository,
        IJsonPatchUpdateService jsonPatchUpdateService
    )
    {
        _settingsRepository = settingsRepository;
        _jsonPatchUpdateService = jsonPatchUpdateService;
    }

    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new HyphenLowerCaseStringEnumConverterFactory() }
    };

    /// <summary>
    /// The changelog is generated at release time and embedded into the
    /// assembly, so it never changes at runtime and is parsed once per process.
    /// </summary>
    private static readonly Lazy<IReadOnlyList<ChangeLogDTO>> _changelog = new(LoadChangelog);

    /// <summary>
    /// The version of the most recent release, which is the git tag the running
    /// build was cut from. Falls back to the API contract version when the
    /// changelog is empty (a repository with no tags yet).
    /// </summary>
    public string GetPlatformVersion()
    {
        return _changelog.Value.Count > 0
            ? _changelog.Value[0].Version
            : NooApiVersions.Current;
    }

    public SearchResult<ChangeLogDTO> GetChangelog()
    {
        var changelog = _changelog.Value;

        return new SearchResult<ChangeLogDTO>(changelog, changelog.Count);
    }

    /// <summary>
    /// A transient default instance stands in until an admin saves for the first
    /// time, so that the anonymous read never writes a row of its own.
    /// </summary>
    public async Task<PlatformSettingsModel> GetSettingsAsync()
    {
        return await _settingsRepository.GetSingletonAsync() ?? new PlatformSettingsModel();
    }

    public async Task UpdateSettingsAsync(JsonPatchDocument<UpdatePlatformSettingsDTO> dto)
    {
        var settings = await _settingsRepository.GetOrCreateSingletonAsync();

        _jsonPatchUpdateService.ApplyPatch(settings, dto);
    }

    private static IReadOnlyList<ChangeLogDTO> LoadChangelog()
    {
        using var stream = typeof(PlatformService).Assembly
            .GetManifestResourceStream(_changelogResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{_changelogResourceName}' was not found."
            );

        var entries = JsonSerializer.Deserialize<IReadOnlyList<ChangelogEntry>>(
            stream,
            _serializerOptions
        ) ?? [];

        return [.. entries.Select(entry => new ChangeLogDTO
        {
            Version = entry.Version,
            Date = Clock.ToMoscow(entry.Date),
            Changes = entry.Changes
        })];
    }

    /// <summary>
    /// The on-disk shape of a changelog release. Kept separate from
    /// <see cref="ChangeLogDTO"/> so the generated file can carry an explicit
    /// UTC offset, which is normalised to Moscow time on load.
    /// </summary>
    private sealed record ChangelogEntry
    {
        [JsonPropertyName("version")]
        public string Version { get; init; } = string.Empty;

        [JsonPropertyName("date")]
        public DateTimeOffset Date { get; init; }

        [JsonPropertyName("changes")]
        public IEnumerable<PlatformChange> Changes { get; init; } = [];
    }
}
