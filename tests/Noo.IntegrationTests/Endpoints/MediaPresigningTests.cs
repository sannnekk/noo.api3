using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Media.Models;
using Noo.Api.Media.Types;
using Noo.Api.Users.Models;
using Noo.Api.Users.Types;

namespace Noo.IntegrationTests.Endpoints;

public class MediaPresigningTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public MediaPresigningTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<Ulid> SeedUserWithAvatarMediaAsync(ApiFactory factory, string path)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();

        var user = new UserModel
        {
            Name = "Avatar User",
            Username = $"avatar-{Guid.NewGuid():N}",
            Email = $"avatar-{Guid.NewGuid():N}@example.com",
            PasswordHash = "x",
            Role = UserRoles.Student,
            IsVerified = true,
        };
        db.GetDbSet<UserModel>().Add(user);

        var media = new MediaModel
        {
            Path = path,
            Name = "avatar.png",
            Extension = "png",
            Category = MediaCategory.UserAvatar,
            Status = MediaStatus.Completed,
            OwnerId = user.Id,
        };
        db.GetDbSet<MediaModel>().Add(media);

        db.GetDbSet<UserAvatarModel>().Add(new UserAvatarModel
        {
            UserId = user.Id,
            AvatarType = UserAvatarType.Custom,
            MediaId = media.Id,
        });

        await db.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task GetUserById_Fills_Avatar_Media_Url_With_Presigned_Url()
    {
        var path = $"avatars/{Guid.NewGuid():N}.png";
        var userId = await SeedUserWithAvatarMediaAsync(_factory, path);

        var response = await _factory.CreateClient().AsAdmin().GetAsync($"/user/{userId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var url = doc.RootElement
            .GetProperty("data")
            .GetProperty("avatar")
            .GetProperty("media")
            .GetProperty("url")
            .GetString();

        url.Should().Be($"{FakeS3Storage.DownloadUrlPrefix}{path}");
    }

    [Fact]
    public async Task GetUsers_Fills_Avatar_Media_Url_For_Listed_Users()
    {
        var path = $"avatars/{Guid.NewGuid():N}.png";
        var userId = await SeedUserWithAvatarMediaAsync(_factory, path);

        var response = await _factory.CreateClient().AsAdmin().GetAsync("/user?page=1&perPage=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var user = doc.RootElement.GetProperty("data").EnumerateArray()
            .First(u => u.GetProperty("id").GetString() == userId.ToString());

        user.GetProperty("avatar").GetProperty("media").GetProperty("url").GetString()
            .Should().Be($"{FakeS3Storage.DownloadUrlPrefix}{path}");
    }
}
