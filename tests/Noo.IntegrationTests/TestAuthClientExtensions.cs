using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Security;
using Noo.Api.Core.Security.Authorization;

namespace Noo.IntegrationTests;

public static class TestAuthClientExtensions
{
    // Read Jwt config from Noo.Api/appsettings.development.json (fallback to appsettings.json)
    private static readonly Lazy<JwtConfig> Jwt = new(LoadJwtConfig);

    public static HttpClient AsUserId(this HttpClient client, Ulid userId)
        => client.AsRole(UserRoles.Student, userId);

    public static HttpClient AsUsername(this HttpClient client, string username)
    {
        var role = username.ToLowerInvariant() switch
        {
            "admin" => UserRoles.Admin,
            "teacher" => UserRoles.Teacher,
            "mentor" => UserRoles.Mentor,
            "assistant" => UserRoles.Assistant,
            "student" => UserRoles.Student,
            _ => UserRoles.Student
        };
        return client.AsRole(role);
    }

    public static HttpClient AsRole(this HttpClient client, UserRoles role)
        => client.AsRole(role, null);

    public static HttpClient AsAdmin(this HttpClient client) => client.AsRole(UserRoles.Admin);
    public static HttpClient AsTeacher(this HttpClient client) => client.AsRole(UserRoles.Teacher);
    public static HttpClient AsMentor(this HttpClient client) => client.AsRole(UserRoles.Mentor);
    public static HttpClient AsAssistant(this HttpClient client) => client.AsRole(UserRoles.Assistant);
    public static HttpClient AsStudent(this HttpClient client) => client.AsRole(UserRoles.Student);

    private static HttpClient AsRole(this HttpClient client, UserRoles role, Ulid? userId)
    {
        var token = BuildAccessToken(role, userId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string BuildAccessToken(UserRoles role, Ulid? userId)
    {
        var jwtConfig = Jwt.Value;
        var jwtService = new JwtService(Options.Create(jwtConfig));
        IEnumerable<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, (userId ?? Ulid.NewUlid()).ToString()),
            new Claim(ClaimTypes.Sid, Ulid.NewUlid().ToString()),
            new Claim(ClaimTypes.Role, role.ToString()),
        ];

        var (token, _) = jwtService.GenerateToken(claims);
        return token;
    }

    private static JwtConfig LoadJwtConfig()
    {
        var root = FindRepoRoot() ?? throw new InvalidOperationException("Repo root not found.");
        var devPath = Path.Combine(root, "src", "Noo.Api", "appsettings.development.json");
        var basePath = Path.Combine(root, "src", "Noo.Api", "appsettings.json");
        var path = File.Exists(devPath) ? devPath : basePath;
        if (!File.Exists(path))
            throw new FileNotFoundException("appsettings file not found for JWT config.", path);

        using var fs = File.OpenRead(path);
        using var doc = JsonDocument.Parse(fs);
        if (!doc.RootElement.TryGetProperty("Jwt", out var jwt))
            throw new InvalidOperationException("Jwt section missing in appsettings.");

        string GetString(string name)
            => jwt.TryGetProperty(name, out var v) ? v.GetString() ?? string.Empty : string.Empty;
        int GetInt(string name)
            => jwt.TryGetProperty(name, out var v) && v.TryGetInt32(out var i) ? i : 0;

        var cfg = new JwtConfig
        {
            Secret = GetString("Secret"),
            Issuer = GetString("Issuer"),
            Audience = GetString("Audience"),
            ExpireDays = GetInt("ExpireDays")
        };

        if (string.IsNullOrWhiteSpace(cfg.Secret) || string.IsNullOrWhiteSpace(cfg.Issuer) || string.IsNullOrWhiteSpace(cfg.Audience) || cfg.ExpireDays <= 0)
        {
            throw new InvalidOperationException("Invalid Jwt configuration loaded from appsettings.");
        }

        return cfg;
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "noo.api.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}
