using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Noo.Api;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Users.Models;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using Noo.Api.Core.DataAbstraction.Cache;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.System.Email;

namespace Noo.IntegrationTests;

public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing"); // enables appsettings.Testing.json if you have it

        // load appsettings.testing.json
        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Keep existing sources and append test settings last to override
            config.AddJsonFile("appsettings.testing.json", optional: true, reloadOnChange: true);
            config.AddEnvironmentVariables();
            // Force sane JWT defaults for tests regardless of machine env
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "1rzc9zrjU6BtzJwY1fofO08R7JhhdRk3lv0fSEStBkIQbFgi79k4j3/LjhG7BsEiUNkOBENRobH8jRKiGo7tFw==",
                ["Jwt:Issuer"] = "https://localhost:5001",
                ["Jwt:Audience"] = "https://localhost:5001",
                ["Jwt:ExpireDays"] = "60",
            });
        })
            .ConfigureServices(services =>
        {
            // Ensure Email options are valid and decouple SMTP for tests
            services.PostConfigure<EmailConfig>(cfg =>
            {
                // Provide safe defaults if missing/placeholder values are used
                cfg.FromEmail = string.IsNullOrWhiteSpace(cfg.FromEmail) || cfg.FromEmail.Contains("...")
                    ? "no-reply@example.com"
                    : cfg.FromEmail;
                cfg.FromName = string.IsNullOrWhiteSpace(cfg.FromName) || cfg.FromName.Contains("...")
                    ? "Noo.Api Tests"
                    : cfg.FromName;
                cfg.SmtpHost = string.IsNullOrWhiteSpace(cfg.SmtpHost) ? "localhost" : cfg.SmtpHost;
                cfg.SmtpPort = cfg.SmtpPort <= 0 ? 1025 : cfg.SmtpPort;
                cfg.SmtpTimeout = cfg.SmtpTimeout <= 0 ? 10000 : cfg.SmtpTimeout;
                cfg.SmtpUsername = string.IsNullOrWhiteSpace(cfg.SmtpUsername) || cfg.SmtpUsername.Contains("...") ? "test" : cfg.SmtpUsername;
                cfg.SmtpPassword = string.IsNullOrWhiteSpace(cfg.SmtpPassword) || cfg.SmtpPassword.Contains("...") ? "test" : cfg.SmtpPassword;
            });

            // JwtConfig is bound directly from configuration; we set it above via in-memory override

            // Replace real email client with a no-op fake to avoid external SMTP dependency
            services.RemoveAll<IEmailClient>();
            services.AddSingleton<IEmailClient, FakeEmailClient>();
            // 0) Remove any mysql registrations
            services.RemoveAll<NooDbContext>();
            services.RemoveAll<DbContextOptions<NooDbContext>>();
            services.RemoveAll<IDbContextFactory<NooDbContext>>();
            services.RemoveAll<IConfigureOptions<DbContextOptions<NooDbContext>>>();
            services.RemoveAll<IPostConfigureOptions<DbContextOptions<NooDbContext>>>();

            // 1) Add InMemory provider (single DB name per factory to share across requests within a test)
            // IMPORTANT: Do NOT call Guid.NewGuid() inside the options lambda; that would create a new
            // in-memory store for every DbContext instance, making data from one request invisible to the next.
            var dbName = $"TestDb-{Guid.NewGuid()}";
            services.RemoveAll<IOptions<DbContextOptions<NooDbContext>>>();
            services.RemoveAll<IOptionsSnapshot<DbContextOptions<NooDbContext>>>();
            services.RemoveAll<IOptionsMonitor<DbContextOptions<NooDbContext>>>();
            services.RemoveAll<IOptionsFactory<DbContextOptions<NooDbContext>>>();

            // Register in-memory DbContextOptions and NooDbContext explicitly (avoid AddDbContext to prevent hidden factories)
            services.AddSingleton(_ =>
                new DbContextOptionsBuilder<NooDbContext>()
                    .UseInMemoryDatabase(dbName)
                    .Options);
            services.AddScoped(sp =>
            {
                var options = sp.GetRequiredService<DbContextOptions<NooDbContext>>();
                var cfg = sp.GetRequiredService<IOptions<Api.Core.Config.Env.DbConfig>>();
                return new NooDbContext(cfg, options);
            });

            // 2) Replace Redis with in-memory caching
            // Remove IDistributedCache (Redis), IConnectionMultiplexer and any existing ICacheRepository
            services.RemoveAll<IDistributedCache>();
            services.RemoveAll<IConnectionMultiplexer>();
            services.RemoveAll<ICacheRepository>();

            // Register distributed in-memory cache and a lightweight test ICacheRepository
            services.AddDistributedMemoryCache();
            services.AddSingleton<ICacheRepository, MemoryCacheRepository>();

            // 3) Seed data for tests
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
            db.Database.EnsureCreated();
            // Seed users for all roles
            var users = db.GetDbSet<UserModel>();

            if (!users.Any())
            {
                users.AddRange(
                [
                    NewUser("Admin User", "admin", "admin@example.com", UserRoles.Admin),
                    NewUser("Teacher User", "teacher", "teacher@example.com", UserRoles.Teacher),
                    NewUser("Mentor User", "mentor", "mentor@example.com", UserRoles.Mentor),
                    NewUser("Assistant User", "assistant", "assistant@example.com", UserRoles.Assistant),
                    NewUser("Student User", "student", "student@example.com", UserRoles.Student)
                ]);

                db.SaveChanges();
            }
        });
    }

    private static UserModel NewUser(string name, string username, string email, UserRoles role)
        => new UserModel
        {
            // Id is auto-generated by BaseModel (Ulid)
            Name = name,
            Username = username,
            Email = email,
            PasswordHash = "test", // any non-empty string to satisfy validation
            Role = role,
            IsBlocked = false,
            IsVerified = true
        };
}

internal sealed class FakeEmailClient : IEmailClient
{
    public Task SendHtmlEmailAsync(string? fromEmail, string? fromName, string toEmail, string toName, string subject, string htmlBody)
        => Task.CompletedTask;

    public void Dispose()
    {
    }
}
