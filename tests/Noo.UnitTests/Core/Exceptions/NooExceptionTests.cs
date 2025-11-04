using System.Net;
using System.Text.Json;
using Noo.Api.Core.Exceptions;

namespace Noo.UnitTests.Core.Exceptions;

public class NooExceptionTests
{
    [Fact]
    public void Serialize_ProducesExpectedShape()
    {
        var ex = new NooException("Boom")
        {
            Id = "CUSTOM",
            StatusCode = HttpStatusCode.BadRequest,
            Payload = new { a = 1 }
        };

        var json = JsonSerializer.Serialize(ex.Serialize());
        Assert.Contains("\"id\":\"CUSTOM\"", json);
        Assert.Contains("\"statusCode\":400", json);
        Assert.Contains("\"message\":\"Boom\"", json);
        Assert.Contains("\"payload\":{\"a\":1}", json);
    }

    [Fact]
    public void SerializePublicly_HidesMessage_WhenInternal()
    {
        var ex = new NooException("Sensitive")
        {
            Id = "INTERNAL",
            StatusCode = HttpStatusCode.InternalServerError,
            IsInternal = true
        };

        var dto = ex.SerializePublicly();
        Assert.Equal("INTERNAL", dto.Id);
        Assert.Equal(500, dto.StatusCode);
        Assert.Equal("An error occurred. Please try again later.", dto.Message);
    }

    [Fact]
    public void FromUnhandled_SetsInternalAndLogId()
    {
        var unhandled = new InvalidOperationException("oops");
        var ex = NooException.FromUnhandled(unhandled);
        Assert.True(ex.IsInternal);
        Assert.False(string.IsNullOrWhiteSpace(ex.LogId));
        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
    }
}
