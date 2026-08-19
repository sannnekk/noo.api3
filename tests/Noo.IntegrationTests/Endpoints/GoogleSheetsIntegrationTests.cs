using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.GoogleSheetsIntegrations.Models;
using Noo.Api.GoogleSheetsIntegrations.Types;
using Noo.Api.Users.Models;

namespace Noo.IntegrationTests.Endpoints;

/// <summary>
/// Endpoint coverage for the Google Sheets export integrations, focused on who is allowed to
/// export what. Google itself is faked out in <see cref="ApiFactory"/>; the OAuth state signing
/// and every authorization decision still run for real.
/// </summary>
public class GoogleSheetsIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public GoogleSheetsIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<(string Code, string State)> GetGrantAsync(HttpClient client)
    {
        var response = await client.GetAsync("/google-sheets/oauth-url");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var state = document.RootElement.GetProperty("data").GetProperty("state").GetString()!;

        return ("fake-auth-code", state);
    }

    private static async Task<HttpResponseMessage> CreateAsync(
        HttpClient client,
        string type,
        object parameters,
        string schedule = "manual")
    {
        var (code, state) = await GetGrantAsync(client);

        return await client.PostAsJsonAsync("/google-sheets", new
        {
            name = $"Export-{Guid.NewGuid():N}",
            type,
            parameters,
            schedule,
            googleAuthCode = code,
            googleAuthState = state
        });
    }

    private static Ulid SeedMentorAssignment(ApiFactory factory, Ulid mentorId, Ulid studentId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();

        var assignment = new MentorAssignmentModel { MentorId = mentorId, StudentId = studentId };
        db.GetDbSet<MentorAssignmentModel>().Add(assignment);
        db.SaveChanges();

        return assignment.Id;
    }

    [Fact]
    public async Task Student_Cannot_Reach_The_Endpoints_At_All()
    {
        var client = _factory.CreateClient().AsStudent();

        var list = await client.GetAsync("/google-sheets");
        list.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var create = await client.PostAsJsonAsync("/google-sheets", new { });
        create.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Assistant_Cannot_Reach_The_Endpoints_At_All()
    {
        var client = _factory.CreateClient().AsAssistant();

        var list = await client.GetAsync("/google-sheets");
        list.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Teacher_Can_Create_A_Users_Export()
    {
        var client = _factory.CreateClient().AsTeacher();

        var response = await CreateAsync(client, "users", new { role = "student" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Teacher_Can_Create_A_Courses_Export_With_No_Parameters()
    {
        var client = _factory.CreateClient().AsTeacher();

        var response = await CreateAsync(client, "courses", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Mentor_Cannot_Create_A_Users_Export()
    {
        var mentorId = TestDataHelpers.GetUserId(_factory, "mentor");
        var client = _factory.CreateClient().AsMentor(mentorId);

        var response = await CreateAsync(client, "users", new { role = "student" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Mentor_Cannot_Create_A_Poll_Results_Export()
    {
        var mentorId = TestDataHelpers.GetUserId(_factory, "mentor");
        var client = _factory.CreateClient().AsMentor(mentorId);

        var response = await CreateAsync(
            client,
            "poll-results",
            new { pollId = Ulid.NewUlid().ToString() });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Mentor_Can_Export_Their_Own_Students_Works()
    {
        var mentorId = TestDataHelpers.GetUserId(_factory, "mentor");
        var studentId = await TestDataHelpers.CreateUserAsync(
            _factory,
            $"student-{Guid.NewGuid():N}",
            $"{Guid.NewGuid():N}@example.com",
            "pw",
            UserRoles.Student);

        SeedMentorAssignment(_factory, mentorId, studentId);

        var client = _factory.CreateClient().AsMentor(mentorId);

        var response = await CreateAsync(
            client,
            "assigned-works",
            new { studentId = studentId.ToString() });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Mentor_Cannot_Export_A_Student_Who_Is_Not_Theirs()
    {
        var mentorId = TestDataHelpers.GetUserId(_factory, "mentor");
        var strangerId = await TestDataHelpers.CreateUserAsync(
            _factory,
            $"student-{Guid.NewGuid():N}",
            $"{Guid.NewGuid():N}@example.com",
            "pw",
            UserRoles.Student);

        var client = _factory.CreateClient().AsMentor(mentorId);

        var response = await CreateAsync(
            client,
            "assigned-works",
            new { studentId = strangerId.ToString() });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Mentor_Cannot_Export_Another_Mentors_Workload()
    {
        var mentorId = TestDataHelpers.GetUserId(_factory, "mentor");
        var otherMentorId = await TestDataHelpers.CreateUserAsync(
            _factory,
            $"mentor-{Guid.NewGuid():N}",
            $"{Guid.NewGuid():N}@example.com",
            "pw",
            UserRoles.Mentor);

        var client = _factory.CreateClient().AsMentor(mentorId);

        var response = await CreateAsync(
            client,
            "assigned-works",
            new { mentorId = otherMentorId.ToString() });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Assigned_Works_Export_Rejects_Both_Student_And_Mentor()
    {
        var client = _factory.CreateClient().AsTeacher();

        var response = await CreateAsync(
            client,
            "assigned-works",
            new { studentId = Ulid.NewUlid().ToString(), mentorId = Ulid.NewUlid().ToString() });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Assigned_Works_Export_Rejects_Neither_Student_Nor_Mentor()
    {
        var client = _factory.CreateClient().AsTeacher();

        var response = await CreateAsync(client, "assigned-works", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Poll_Results_Export_Requires_A_Poll()
    {
        var client = _factory.CreateClient().AsTeacher();

        var response = await CreateAsync(client, "poll-results", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Rejects_A_Forged_OAuth_State()
    {
        var client = _factory.CreateClient().AsTeacher();

        var response = await client.PostAsJsonAsync("/google-sheets", new
        {
            name = "Forged",
            type = "courses",
            parameters = new { },
            schedule = "manual",
            googleAuthCode = "fake-auth-code",
            googleAuthState = "bm90LWEtcmVhbC1zdGF0ZQ==.deadbeef"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Rejects_Another_Users_OAuth_State()
    {
        // The state is bound to the user it was issued to, so a code obtained through
        // someone else's consent screen cannot be attached to this account.
        var teacherClient = _factory.CreateClient().AsTeacher();
        var (_, teacherState) = await GetGrantAsync(teacherClient);

        var adminClient = _factory.CreateClient().AsAdmin();

        var response = await adminClient.PostAsJsonAsync("/google-sheets", new
        {
            name = "Borrowed state",
            type = "courses",
            parameters = new { },
            schedule = "manual",
            googleAuthCode = "fake-auth-code",
            googleAuthState = teacherState
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Run_Queues_The_Integration_And_Returns_202()
    {
        var client = _factory.CreateClient().AsTeacher();

        var created = await CreateAsync(client, "courses", new { });
        var integrationId = await ReadIdAsync(created);

        var run = await client.PostAsync($"/google-sheets/{integrationId}/run", null);

        run.StatusCode.Should().Be(HttpStatusCode.Accepted);
        GetRunState(integrationId).Should().Be(GoogleSheetsIntegrationRunState.Queued);
    }

    /// <summary>
    /// An integration belongs to whoever set it up: it holds their Google grant and writes to
    /// their spreadsheet. These pin that nobody reaches anyone else's, seniority included —
    /// an admin has no more claim on a teacher's export than a mentor does.
    /// </summary>
    private async Task<(Ulid IntegrationId, Ulid OwnerId)> CreateOwnedByTeacherAsync()
    {
        var teacherId = TestDataHelpers.GetUserId(_factory, "teacher");
        var teacherClient = _factory.CreateClient().AsTeacher(teacherId);
        var created = await CreateAsync(teacherClient, "courses", new { });

        return (await ReadIdAsync(created), teacherId);
    }

    /// <summary>
    /// Who owns each integration the listing came back with. Read out as plain strings
    /// because a JsonElement does not outlive the document it was read from.
    /// </summary>
    private static async Task<List<string?>> ListedOwnerIdsAsync(HttpClient client)
    {
        var response = await client.GetAsync("/google-sheets");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document
            .RootElement.GetProperty("data")
            .EnumerateArray()
            .Select(item => item.GetProperty("ownerId").GetString())
            .ToList();
    }

    [Fact]
    public async Task A_Mentor_Only_Sees_Their_Own_Integrations()
    {
        await CreateOwnedByTeacherAsync();

        var mentorId = TestDataHelpers.GetUserId(_factory, "mentor");
        var mentorClient = _factory.CreateClient().AsMentor(mentorId);

        await CreateAsync(mentorClient, "assigned-works", new { mentorId = mentorId.ToString() });

        var owners = await ListedOwnerIdsAsync(mentorClient);

        owners.Should().NotBeEmpty();
        owners.Should().AllBe(mentorId.ToString());
    }

    [Fact]
    public async Task A_Teacher_Only_Sees_Their_Own_Integrations()
    {
        var (_, teacherId) = await CreateOwnedByTeacherAsync();

        var mentorId = TestDataHelpers.GetUserId(_factory, "mentor");
        var mentorClient = _factory.CreateClient().AsMentor(mentorId);
        await CreateAsync(mentorClient, "assigned-works", new { mentorId = mentorId.ToString() });

        var owners = await ListedOwnerIdsAsync(_factory.CreateClient().AsTeacher(teacherId));

        owners.Should().NotBeEmpty();
        owners.Should().AllBe(teacherId.ToString());
    }

    [Fact]
    public async Task An_Admin_Sees_None_Of_Anybody_Elses_Integrations()
    {
        await CreateOwnedByTeacherAsync();

        // The admin has set none up, so there is nothing of theirs to list.
        var owners = await ListedOwnerIdsAsync(_factory.CreateClient().AsAdmin());

        owners.Should().BeEmpty();
    }

    [Theory]
    [InlineData(UserRoles.Admin)]
    [InlineData(UserRoles.Mentor)]
    public async Task Nobody_Else_Can_Run_An_Integration(UserRoles role)
    {
        var (integrationId, _) = await CreateOwnedByTeacherAsync();

        var response = await _factory
            .CreateClient()
            .AsRole(role)
            .PostAsync($"/google-sheets/{integrationId}/run", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        GetRunState(integrationId).Should().Be(GoogleSheetsIntegrationRunState.Idle);
    }

    [Theory]
    [InlineData(UserRoles.Admin)]
    [InlineData(UserRoles.Mentor)]
    public async Task Nobody_Else_Can_Delete_An_Integration(UserRoles role)
    {
        var (integrationId, _) = await CreateOwnedByTeacherAsync();

        var response = await _factory
            .CreateClient()
            .AsRole(role)
            .DeleteAsync($"/google-sheets/{integrationId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(UserRoles.Admin)]
    [InlineData(UserRoles.Mentor)]
    public async Task Nobody_Else_Can_Change_An_Integration(UserRoles role)
    {
        var (integrationId, _) = await CreateOwnedByTeacherAsync();

        var response = await _factory
            .CreateClient()
            .AsRole(role)
            .PatchAsJsonAsync($"/google-sheets/{integrationId}", new { name = "Taken over" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task The_Owner_Still_Runs_Changes_And_Deletes_Their_Own()
    {
        var teacherId = TestDataHelpers.GetUserId(_factory, "teacher");
        var client = _factory.CreateClient().AsTeacher(teacherId);
        var integrationId = await ReadIdAsync(await CreateAsync(client, "courses", new { }));

        var renamed = await client.PatchAsJsonAsync(
            $"/google-sheets/{integrationId}",
            new { name = "Mine" });
        renamed.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var run = await client.PostAsync($"/google-sheets/{integrationId}/run", null);
        run.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var deleted = await client.DeleteAsync($"/google-sheets/{integrationId}");
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_Can_Disable_An_Integration_But_Not_Mark_It_Errored()
    {
        var client = _factory.CreateClient().AsTeacher();

        var created = await CreateAsync(client, "courses", new { }, schedule: "daily");
        var integrationId = await ReadIdAsync(created);

        var disable = await client.PatchAsJsonAsync(
            $"/google-sheets/{integrationId}",
            new { status = "inactive" });

        disable.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // A disabled integration must stop being scheduled.
        GetIntegration(integrationId).NextRunAt.Should().BeNull();

        var forceError = await client.PatchAsJsonAsync(
            $"/google-sheets/{integrationId}",
            new { status = "error" });

        forceError.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_Removes_The_Integration()
    {
        var client = _factory.CreateClient().AsTeacher();

        var created = await CreateAsync(client, "courses", new { });
        var integrationId = await ReadIdAsync(created);

        var response = await client.DeleteAsync($"/google-sheets/{integrationId}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();

        db.GetDbSet<GoogleSheetsIntegrationModel>()
            .Any(integration => integration.Id == integrationId)
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task Refresh_Token_Is_Never_Stored_In_Clear_Text()
    {
        var client = _factory.CreateClient().AsTeacher();

        var created = await CreateAsync(client, "courses", new { });
        var integrationId = await ReadIdAsync(created);

        var stored = GetIntegration(integrationId).GoogleAuthData;

        stored.RefreshTokenEncrypted.Should().NotBeNullOrEmpty();
        stored.RefreshTokenEncrypted.Should()
            .NotContain(FakeGoogleOAuthExchangeService.RefreshToken);
        stored.AccountEmail.Should().Be(FakeGoogleOAuthExchangeService.AccountEmail);
    }

    private static async Task<Ulid> ReadIdAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return Ulid.Parse(document.RootElement.GetProperty("data").GetProperty("id").GetString()!);
    }

    private GoogleSheetsIntegrationModel GetIntegration(Ulid integrationId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();

        return db.GetDbSet<GoogleSheetsIntegrationModel>()
            .First(integration => integration.Id == integrationId);
    }

    private GoogleSheetsIntegrationRunState GetRunState(Ulid integrationId)
        => GetIntegration(integrationId).RunState;
}
