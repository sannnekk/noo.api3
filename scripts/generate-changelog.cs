// Generates src/Noo.Api/Platform/changelog.json from the git history.
//
// Commits are grouped by the git tag they were released under, newest release
// first. Only commits that classify into one of the four ChangeType values end
// up in the changelog; chore, docs, test and friends are dropped.
//
// Usage:
//   dotnet run scripts/generate-changelog.cs                  # released tags only
//   dotnet run scripts/generate-changelog.cs -- 1.2.0-alpha   # + unreleased
//
// The file is embedded into the assembly and served by PlatformService, so the
// git history is not needed at build or run time (the Docker build excludes it).

// File-based apps disable reflection-based JSON by default; this script is a
// build-time tool, so the trimming-friendly source generator is not worth it.
#:property JsonSerializerIsReflectionEnabledByDefault=true
#:property NoWarn=IL2026;IL3050

using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

// Conventional commit types that map onto the ChangeType enum. The values are
// the hyphen-lowercase form the API serializes enums with.
var changeTypes = new Dictionary<string, string>
{
    ["feat"] = "feature",
    ["fix"] = "bug-fix",
    ["perf"] = "optimization",
    ["refactor"] = "refactor"
};

// Valid commit types that are intentionally kept out of the changelog.
var silentTypes = new HashSet<string> { "chore", "docs", "test", "style", "ci", "build" };

// Fallback for commits written before the conventional-commit convention was
// adopted. The old history is consistent enough about its leading verb that
// this recovers almost every entry.
var legacyVerbs = new (Regex Pattern, string Type)[]
{
    (new Regex(@"^added\b", RegexOptions.IgnoreCase), "feature"),
    (new Regex(@"^implemented\b", RegexOptions.IgnoreCase), "feature"),
    (new Regex(@"^fixed\b", RegexOptions.IgnoreCase), "bug-fix"),
    (new Regex(@"^improved\b", RegexOptions.IgnoreCase), "optimization"),
    (new Regex(@"^optimi[sz]ed\b", RegexOptions.IgnoreCase), "optimization"),
    (new Regex(@"^refactored\b", RegexOptions.IgnoreCase), "refactor")
};

// Order changes are listed in within a single release.
var typeOrder = new[] { "feature", "bug-fix", "optimization", "refactor" };

var conventionalPattern = new Regex(@"^([a-z]+)(?:\(([^)]+)\))?(!)?:\s*(.+)$");

const char FieldSeparator = '\u001f';

string Git(params string[] args)
{
    var startInfo = new ProcessStartInfo("git")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    foreach (var arg in args)
    {
        startInfo.ArgumentList.Add(arg);
    }

    using var process = Process.Start(startInfo)
        ?? throw new InvalidOperationException("Could not start git.");

    var output = process.StandardOutput.ReadToEnd();
    var error = process.StandardError.ReadToEnd();

    process.WaitForExit();

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException($"git {string.Join(' ', args)} failed: {error}");
    }

    return output.TrimEnd();
}

// Turns a commit subject into a changelog entry, or null when the commit
// should not appear in the changelog at all.
Change? Classify(string subject, string author)
{
    var conventional = conventionalPattern.Match(subject);

    if (conventional.Success)
    {
        var type = conventional.Groups[1].Value;
        var scope = conventional.Groups[2].Value;
        var description = conventional.Groups[4].Value;

        if (silentTypes.Contains(type) || !changeTypes.TryGetValue(type, out var mapped))
        {
            return null;
        }

        return new Change(
            mapped,
            author,
            string.IsNullOrEmpty(scope) ? description : $"{scope}: {description}"
        );
    }

    foreach (var (pattern, type) in legacyVerbs)
    {
        if (pattern.IsMatch(subject))
        {
            return new Change(type, author, subject);
        }
    }

    return null;
}

// Reads the commits in range and returns their changelog entries.
List<Change> CollectChanges(string range)
{
    var log = Git("log", "--no-merges", $"--format=%an{FieldSeparator}%s", range);

    if (string.IsNullOrEmpty(log))
    {
        return [];
    }

    var changes = new List<Change>();

    foreach (var line in log.Split('\n'))
    {
        var parts = line.Split(FieldSeparator, 2);

        if (parts.Length != 2)
        {
            continue;
        }

        var change = Classify(parts[1], parts[0]);

        if (change is not null)
        {
            changes.Add(change);
        }
    }

    return [.. changes.OrderBy(change => Array.IndexOf(typeOrder, change.Type))];
}

// Strips the v prefix so versions match what the API reports.
string ToVersion(string tag) => tag.StartsWith('v') ? tag[1..] : tag;

var unreleasedVersion = args.Length > 0 ? args[0] : null;

// Tags oldest-first. Lightweight tags make creatordate the commit date.
var tagOutput = Git("tag", "--list", "--sort=creatordate");
string[] tags = string.IsNullOrEmpty(tagOutput) ? [] : tagOutput.Split('\n');

var releases = new List<Release>();

for (var index = 0; index < tags.Length; index++)
{
    var tag = tags[index];
    var range = index == 0 ? tag : $"{tags[index - 1]}..{tag}";
    var changes = CollectChanges(range);

    if (changes.Count > 0)
    {
        releases.Add(new Release(ToVersion(tag), Git("log", "-1", "--format=%aI", tag), changes));
    }
}

if (unreleasedVersion is not null)
{
    var range = tags.Length > 0 ? $"{tags[^1]}..HEAD" : "HEAD";
    var changes = CollectChanges(range);

    if (changes.Count > 0)
    {
        releases.Add(new Release(
            ToVersion(unreleasedVersion),
            Git("log", "-1", "--format=%aI", "HEAD"),
            changes
        ));
    }
}

releases.Reverse();

var root = Git("rev-parse", "--show-toplevel");
var outputPath = Path.Combine(root, "src", "Noo.Api", "Platform", "changelog.json");

var json = JsonSerializer.Serialize(releases, new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    WriteIndented = true
});

File.WriteAllText(outputPath, json + Environment.NewLine);

var total = releases.Sum(release => release.Changes.Count);

Console.WriteLine($"changelog.json: {releases.Count} release(s), {total} change(s) -> {outputPath}");

internal sealed record Change(string Type, string Author, string Description);

internal sealed record Release(string Version, string Date, List<Change> Changes);
