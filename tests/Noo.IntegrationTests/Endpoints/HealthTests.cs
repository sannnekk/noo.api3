using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Noo.IntegrationTests.Endpoints;

public class HealthTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;

    public HealthTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact(DisplayName = "Health endpoint returns OK status")]
    public async Task Get_Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "Health endpoint includes all live checks (liveness, memory)")]
    public async Task Get_Health_IncludesLiveChecks()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var checks = doc.RootElement.GetProperty("checks");

        bool hasLiveness = false,
            hasMemory = false;
        foreach (var entry in checks.EnumerateArray())
        {
            var key = entry.GetProperty("key").GetString();
            if (key == "liveness")
                hasLiveness = true;
            if (key == "memory")
                hasMemory = true;
        }

        hasLiveness.Should().BeTrue("liveness check should be registered under 'live' tag");
        hasMemory.Should().BeTrue("memory check should be registered under 'live' tag");
    }

    [Fact(DisplayName = "Health endpoint includes ready check (database)")]
    public async Task Get_Health_IncludesReadyChecks()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var checks = doc.RootElement.GetProperty("checks");

        bool hasDatabase = false;
        foreach (var entry in checks.EnumerateArray())
        {
            var key = entry.GetProperty("key").GetString();
            if (key == "database")
                hasDatabase = true;
        }

        hasDatabase.Should().BeTrue("database check should be registered under 'ready' tag");
    }

    [Fact(DisplayName = "Memory health check includes expected data fields")]
    public async Task Get_Health_MemoryCheck_HasData()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var checks = doc.RootElement.GetProperty("checks");

        JsonElement? memoryEntry = null;
        foreach (var entry in checks.EnumerateArray())
        {
            var key = entry.GetProperty("key").GetString();
            if (key == "memory")
            {
                memoryEntry = entry;
                break;
            }
        }

        memoryEntry.HasValue.Should().BeTrue("memory check should be present");
        var data = memoryEntry!.Value.GetProperty("data");
        data.TryGetProperty("allocatedMemoryInMb", out _).Should().BeTrue();
        data.TryGetProperty("availableMemoryInMb", out _).Should().BeTrue();
        data.TryGetProperty("ratio", out _).Should().BeTrue();
    }

    [Fact(DisplayName = "Health endpoint returns 503 when database check is unhealthy")]
    public async Task Get_Health_ReturnsUnhealthy_WhenDatabaseFails()
    {
        // Create a client that overrides the 'database' check to always be unhealthy
        var badFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.PostConfigure<HealthCheckServiceOptions>(options =>
                {
                    // Remove existing 'database' registrations (mutate in place)
                    var dbRegs = options
                        .Registrations.Where(r =>
                            string.Equals(r.Name, "database", StringComparison.OrdinalIgnoreCase)
                        )
                        .ToList();
                    foreach (var reg in dbRegs)
                        options.Registrations.Remove(reg);

                    // Add failing 'database' check
                    options.Registrations.Add(
                        new HealthCheckRegistration(
                            "database",
                            sp => new AlwaysUnhealthyCheck("simulated db failure"),
                            failureStatus: HealthStatus.Unhealthy,
                            tags: new[] { "ready" }
                        )
                    );
                });
            });
        });

        using var client = badFactory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Unhealthy");
        var checks = doc.RootElement.GetProperty("checks");
        var dbCheck = checks
            .EnumerateArray()
            .FirstOrDefault(e => e.GetProperty("key").GetString() == "database");
        dbCheck.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        dbCheck.GetProperty("value").GetString().Should().Be("Unhealthy");
    }

    [Fact(DisplayName = "Health endpoint returns 503 when memory check is unhealthy")]
    public async Task Get_Health_ReturnsUnhealthy_WhenMemoryFails()
    {
        var badFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.PostConfigure<HealthCheckServiceOptions>(options =>
                {
                    // Remove existing 'memory' registrations (mutate in place)
                    var memRegs = options
                        .Registrations.Where(r =>
                            string.Equals(r.Name, "memory", StringComparison.OrdinalIgnoreCase)
                        )
                        .ToList();
                    foreach (var reg in memRegs)
                        options.Registrations.Remove(reg);

                    // Add failing 'memory' check
                    options.Registrations.Add(
                        new HealthCheckRegistration(
                            "memory",
                            sp => new AlwaysUnhealthyCheck("simulated memory pressure"),
                            failureStatus: HealthStatus.Unhealthy,
                            tags: new[] { "live" }
                        )
                    );
                });
            });
        });

        using var client = badFactory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Unhealthy");
        var checks = doc.RootElement.GetProperty("checks");
        var memCheck = checks
            .EnumerateArray()
            .FirstOrDefault(e => e.GetProperty("key").GetString() == "memory");
        memCheck.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        memCheck.GetProperty("value").GetString().Should().Be("Unhealthy");
    }

    private sealed class AlwaysUnhealthyCheck : IHealthCheck
    {
        private readonly string _message;

        public AlwaysUnhealthyCheck(string message) => _message = message;

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(HealthCheckResult.Unhealthy(_message));
    }
}
