using Noo.Api.Core.Initialization.App;
using Noo.Api.Core.Initialization.ServiceCollection;
using Noo.Api.Core.Initialization.WebHostBuilder;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Sessions.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider((context, options) =>
{
    options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
    options.ValidateOnBuild = true;
});

builder.Services.LoadEnvConfigs(builder.Configuration);
builder.Services.AddS3Storage();
builder.Services.AddNooDbContext(builder.Configuration);
builder.Services.AddNooAuthentication(builder.Configuration);
builder.Services.AddNooAuthorization();
builder.Services.AddNooSwagger(builder.Configuration);
builder.Services.AddLogger(builder.Configuration);
builder.Services.RegisterDependencies();
builder.Services.AddNooControllersAndConfigureJson();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClientFactory();
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddNooResponseCompression();
builder.Services.AddHealthcheckServices();
builder.Services.AddRouteConstraints();
builder.Services.AddRequestRateLimiter();
builder.Services.AddRouting();
builder.Services.AddAutoMapperProfiles();
builder.Services.AddCacheProvider(builder.Configuration);
builder.Services.AddMetrics();
builder.Services.AddDomainEventsBackgroundWorker();
builder.Services.AddHostedServices();
builder.Services.AddMediatR();

builder.WebHost.AddWebServerConfiguration(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandling();
app.UseRouting();
app.UseRateLimiter();
app.UseNooSwagger(app.Configuration);
app.UseCors();
app.UseResponseCompression();
//app.UseResponseCaching();
app.UseAuthentication();
app.UseAuthorization();
app.UseSessionActivity();
app.MapControllers();
app.MapHealthAllChecks();

// Ensure database is created when running tests
if (app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
    await db.Database.EnsureCreatedAsync();
}

await app.RunAsync();
