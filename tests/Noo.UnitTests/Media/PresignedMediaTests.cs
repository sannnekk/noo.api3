using Noo.Api.AssignedWorks.DTO;
using Noo.Api.Core.Response;
using Noo.Api.Media.DTO;
using Noo.Api.Users.DTO;

namespace Noo.UnitTests.Media;

public class PresignedMediaTests
{
    private static MediaDTO Media(string path) => new() { Id = Ulid.NewUlid(), Path = path };

    private static UserDTO UserWithMedia(string path) => new()
    {
        Id = Ulid.NewUlid(),
        Avatar = new UserAvatarDTO { Id = Ulid.NewUlid(), Media = Media(path) }
    };

    [Fact]
    public void Collect_Null_Returns_Empty()
    {
        Assert.Empty(PresignedMedia.Collect((object?)null));
    }

    [Fact]
    public void Collect_Bare_MediaDto_Returns_Itself()
    {
        var media = Media("a");

        var result = PresignedMedia.Collect(media);

        Assert.Equal([media], result);
    }

    [Fact]
    public void Collect_Reaches_Media_Through_Nested_Dtos()
    {
        var user = UserWithMedia("avatar");

        var result = PresignedMedia.Collect(user).ToArray();

        Assert.Single(result);
        Assert.Same(user.Avatar!.Media, result[0]);
    }

    [Fact]
    public void Collect_AssignedWork_Reaches_Student_And_Both_Mentors()
    {
        var work = new AssignedWorkDTO
        {
            Student = UserWithMedia("student"),
            MainMentor = UserWithMedia("main"),
            HelperMentor = UserWithMedia("helper")
        };

        var paths = PresignedMedia.Collect(work)
            .Select(m => m!.Path)
            .Order()
            .ToArray();

        Assert.Equal(["helper", "main", "student"], paths);
    }

    [Fact]
    public void Collect_Tolerates_Missing_Mentors()
    {
        var work = new AssignedWorkDTO
        {
            Student = UserWithMedia("student"),
            MainMentor = null,
            HelperMentor = null
        };

        var result = PresignedMedia.Collect(work).ToArray();

        Assert.Single(result);
        Assert.Equal("student", result[0]!.Path);
    }

    [Fact]
    public void ApiResponse_Envelope_Collects_From_Single_Item()
    {
        var user = UserWithMedia("avatar");
        var response = new ApiResponseDTO<UserDTO>(user, null);

        var result = ((IHasPresignedMedia)response).GetMediaForPresigning().ToArray();

        Assert.Single(result);
        Assert.Same(user.Avatar!.Media, result[0]);
    }

    [Fact]
    public void ApiResponse_Envelope_Collects_From_Collection()
    {
        var users = new[] { UserWithMedia("a"), UserWithMedia("b") };
        var response = new ApiResponseDTO<IEnumerable<UserDTO>>(users, null);

        var paths = ((IHasPresignedMedia)response).GetMediaForPresigning()
            .Select(m => m!.Path)
            .Order()
            .ToArray();

        Assert.Equal(["a", "b"], paths);
    }
}
