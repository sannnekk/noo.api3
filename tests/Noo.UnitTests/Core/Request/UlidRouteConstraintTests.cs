using Microsoft.AspNetCore.Routing;
using Noo.Api.Core.Request;

namespace Noo.UnitTests.Core.Request;

public class UlidRouteConstraintTests
{
    [Theory(DisplayName = "UlidRouteConstraint matches valid ULIDs and rejects invalid inputs")]
    [InlineData("01H84V6K3M4CQP2W6E3Q2X8V7Z", true)]
    [InlineData("01H84V6K3M4CQP2W6E3Q2X8V7Z_extra", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("00000000000000000000000000", false)]
    public void Match_ValidatesUlid(object? input, bool expected)
    {
        var constraint = new UlidRouteConstraint();
        var values = new RouteValueDictionary { ["id"] = input };
        var matched = constraint.Match(null, null, "id", values, RouteDirection.IncomingRequest);
        Assert.Equal(expected, matched);
    }
}
