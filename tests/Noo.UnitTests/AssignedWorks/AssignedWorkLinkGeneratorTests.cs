using Noo.Api.AssignedWorks.Services;

namespace Noo.UnitTests.AssignedWorks;

public class AssignedWorkLinkGeneratorTests
{
    [Fact]
    public void GenerateViewLink_Returns_Json_With_Id()
    {
        var gen = new AssignedWorkLinkGenerator();
        var id = Ulid.NewUlid();
        var json = gen.GenerateViewLink(id);
        Assert.Contains(id.ToString(), json);
        Assert.Contains("assigned-works.view", json);
    }
}
