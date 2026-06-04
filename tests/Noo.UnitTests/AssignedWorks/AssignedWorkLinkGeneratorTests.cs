using System.Text.Json;
using Noo.Api.AssignedWorks.Services;

namespace Noo.UnitTests.AssignedWorks;

public class AssignedWorkLinkGeneratorTests
{
    [Fact]
    public void GenerateViewLink_Returns_Link_With_Id()
    {
        var gen = new AssignedWorkLinkGenerator();
        var id = Ulid.NewUlid();
        var link = gen.GenerateViewLink(id);
        Assert.Equal("assigned-works.detail", link.Name);
        Assert.Contains(id.ToString(), JsonSerializer.Serialize(link.Params));
    }
}
