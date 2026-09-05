using System.Text.Json;
using System.Text.Json.Serialization;
using Noo.Api.Core.System.Realtime;

namespace Noo.UnitTests.Core.Realtime;

/// <summary>
/// Guards the one thing about a hub that cannot fail loudly: a renamed client method is a
/// message the frontend silently stops receiving — no error, no 404, nothing in a log.
///
/// The generated file is vendored by front-2 next to <c>openapi.yaml</c>, where a vitest asserts
/// its TypeScript contracts still cover it. Drift then fails on whichever side is stale.
/// </summary>
public class RealtimeContractTests
{
    private static readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private sealed record ContractMethod(string Name, string[] Parameters);

    private sealed record HubContract(string Client, ContractMethod[] Methods);

    private sealed record RealtimeContract(HubContract[] Hubs);

    private static string ContractPath =>
        Path.Combine(FindRepoRoot(), "src", "Noo.Api", "realtime-contract.json");

    [Fact]
    public void TheCheckedInContractMatchesTheHubClientInterfaces()
    {
        var actual = Serialize(BuildFromAssembly());

        Assert.True(
            File.Exists(ContractPath),
            $"{ContractPath} is missing. Regenerate it with the instructions in this test."
        );

        var committed = File.ReadAllText(ContractPath).ReplaceLineEndings();

        Assert.True(
            committed == actual,
            "realtime-contract.json is out of date with the hub client interfaces.\n"
                + "Rewrite it with this content and vendor the same file into front-2:\n\n"
                + actual
        );
    }

    // Nothing enforces this from the compiler's side, so it is asserted here: the suffix is part
    // of the wire name, and a client interface without it would not match what the hub sends.
    [Fact]
    public void EveryHubClientMethodIsAnAsyncSuffixedTask()
    {
        foreach (var client in HubClientInterfaces())
        {
            foreach (var method in client.GetMethods())
            {
                Assert.True(
                    method.ReturnType == typeof(Task),
                    $"{client.Name}.{method.Name} must return Task."
                );
                Assert.EndsWith("Async", method.Name, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>
    /// Every hub's client contract, found through the hubs themselves rather than by naming
    /// convention — an interface that happened to be named differently would otherwise be left
    /// silently uncovered, which is the same failure this file exists to prevent.
    /// </summary>
    private static IEnumerable<Type> HubClientInterfaces() =>
        typeof(RealtimeMetrics)
            .Assembly.GetTypes()
            .Where(type => !type.IsAbstract)
            .Select(ClientContractOf)
            .Where(client => client is not null)
            .Select(client => client!)
            .Distinct()
            .OrderBy(client => client.Name, StringComparer.Ordinal);

    private static Type? ClientContractOf(Type hub)
    {
        for (var current = hub.BaseType; current is not null; current = current.BaseType)
        {
            if (
                current.IsGenericType
                && current.GetGenericTypeDefinition() == typeof(NooHub<>)
            )
            {
                return current.GetGenericArguments()[0];
            }
        }

        return null;
    }

    private static RealtimeContract BuildFromAssembly() =>
        new(
            [
                .. HubClientInterfaces()
                    .Select(client => new HubContract(
                        client.Name,
                        [
                            .. client
                                .GetMethods()
                                .OrderBy(method => method.Name, StringComparer.Ordinal)
                                .Select(method => new ContractMethod(
                                    method.Name,
                                    [
                                        .. method
                                            .GetParameters()
                                            .Select(parameter => parameter.ParameterType.Name),
                                    ]
                                )),
                        ]
                    )),
            ]
        );

    private static string Serialize(RealtimeContract contract) =>
        (JsonSerializer.Serialize(contract, _json) + Environment.NewLine).ReplaceLineEndings();

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "noo.api.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
