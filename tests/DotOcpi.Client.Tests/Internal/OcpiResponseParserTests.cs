using System.Net;
using System.Text;
using System.Text.Json;
using DotOcpi.Client.Internal;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Client.Tests.Internal;

public class OcpiResponseParserTests
{
    private static HttpResponseMessage CreateResponse(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        IDictionary<string, string>? headers = null
    )
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        if (headers is not null)
        {
            foreach (var (key, value) in headers)
            {
                response.Headers.TryAddWithoutValidation(key, value);
            }
        }

        return response;
    }

    [Fact]
    public async Task ParseObjectAsync_SuccessResponse_ReturnsData()
    {
        var json = """{"status_code": 1000, "data": {"id": "LOC1"}, "timestamp": "2024-01-01T00:00:00Z"}""";
        var response = CreateResponse(json);

        var result = await OcpiResponseParser.ParseObjectAsync<JsonElement>(response, OcpiVersion.V2_2_1);

        result.IsSuccess.Should().BeTrue();
        result.Data.GetProperty("id").GetString().Should().Be("LOC1");
    }

    [Fact]
    public async Task ParseObjectAsync_ErrorResponse_ReturnsFailure()
    {
        var json =
            """{"status_code": 2003, "status_message": "Unknown location", "timestamp": "2024-01-01T00:00:00Z"}""";
        var response = CreateResponse(json);

        var result = await OcpiResponseParser.ParseObjectAsync<JsonElement>(response, OcpiVersion.V2_2_1);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Value.Should().Be(2003);
        result.StatusMessage.Should().Contain("Unknown location");
    }

    [Fact]
    public async Task ParseObjectAsync_HttpError_ReturnsFailure()
    {
        var json = """{"status_code": 2002, "status_message": "Invalid token"}""";
        var response = CreateResponse(json, HttpStatusCode.Unauthorized);

        var result = await OcpiResponseParser.ParseObjectAsync<JsonElement>(response, OcpiVersion.V2_2_1);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Value.Should().Be(2002);
    }

    [Fact]
    public async Task ParseObjectAsync_HttpError_NonJsonBody_ReturnsGenericError()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal Server Error", Encoding.UTF8, "text/plain"),
        };

        var result = await OcpiResponseParser.ParseObjectAsync<JsonElement>(response, OcpiVersion.V2_2_1);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Value.Should().Be(3000);
    }

    [Fact]
    public async Task ParseNoDataAsync_SuccessResponse_ReturnsSuccess()
    {
        var json = """{"status_code": 1000, "timestamp": "2024-01-01T00:00:00Z"}""";
        var response = CreateResponse(json);

        var result = await OcpiResponseParser.ParseNoDataAsync(response);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ParseNoDataAsync_ErrorResponse_ReturnsFailure()
    {
        var json = """{"status_code": 2000, "status_message": "Bad request"}""";
        var response = CreateResponse(json);

        var result = await OcpiResponseParser.ParseNoDataAsync(response);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Value.Should().Be(2000);
    }

    [Fact]
    public async Task ParseListAsync_ReturnsItemsAndPaginationInfo()
    {
        var json =
            """{"status_code": 1000, "data": [{"id": "LOC1"}, {"id": "LOC2"}], "timestamp": "2024-01-01T00:00:00Z"}""";
        var headers = new Dictionary<string, string>
        {
            ["X-Total-Count"] = "10",
            ["Link"] = """<https://cpo.example.com/ocpi/2.2.1/cpo/locations?offset=2&limit=2>; rel="next" """,
        };
        var response = CreateResponse(json, headers: headers);

        var result = await OcpiResponseParser.ParseListAsync(response, OcpiVersion.V2_2_1, typeof(JsonElement));

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(10);
        result.NextLink.Should().Contain("offset=2");
        result.StatusCode.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ParseListAsync_LastPage_NoNextLink()
    {
        var json = """{"status_code": 1000, "data": [{"id": "LOC1"}], "timestamp": "2024-01-01T00:00:00Z"}""";
        var headers = new Dictionary<string, string> { ["X-Total-Count"] = "3" };
        var response = CreateResponse(json, headers: headers);

        var result = await OcpiResponseParser.ParseListAsync(response, OcpiVersion.V2_2_1, typeof(JsonElement));

        result.Items.Should().HaveCount(1);
        result.NextLink.Should().BeNull();
    }

    [Fact]
    public async Task ParseListAsync_ErrorResponse_ReturnsEmpty()
    {
        var json = """{"status_code": 3000, "status_message": "Server error"}""";
        var response = CreateResponse(json, HttpStatusCode.InternalServerError);

        var result = await OcpiResponseParser.ParseListAsync(response, OcpiVersion.V2_2_1, typeof(JsonElement));

        result.Items.Should().BeEmpty();
        result.StatusCode.Value.Should().Be(3000);
    }

    [Fact]
    public void ParseLinkHeader_ValidLink_ExtractsUrl()
    {
        var response = new HttpResponseMessage();
        response.Headers.TryAddWithoutValidation(
            "Link",
            """<https://cpo.example.com/ocpi/2.2.1/cpo/locations?offset=50&limit=50>; rel="next" """
        );

        var url = OcpiResponseParser.ParseLinkHeader(response);

        url.Should().Be("https://cpo.example.com/ocpi/2.2.1/cpo/locations?offset=50&limit=50");
    }

    [Fact]
    public void ParseLinkHeader_NoHeader_ReturnsNull()
    {
        var response = new HttpResponseMessage();

        var url = OcpiResponseParser.ParseLinkHeader(response);

        url.Should().BeNull();
    }

    [Fact]
    public async Task ParseVersionedObjectAsync_DeserializesCorrectType()
    {
        var json = """{"status_code": 1000, "data": {"id": "LOC1"}, "timestamp": "2024-01-01T00:00:00Z"}""";
        var response = CreateResponse(json);

        var result = await OcpiResponseParser.ParseVersionedObjectAsync(
            response,
            OcpiVersion.V2_2_1,
            typeof(JsonElement)
        );

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeOfType<JsonElement>();
        ((JsonElement)result.Data!).GetProperty("id").GetString().Should().Be("LOC1");
    }

    [Fact]
    public async Task ParseVersionedObjectAsync_ErrorStatus_ReturnsFailure()
    {
        var json = """{"status_code": 2003, "status_message": "Not found"}""";
        var response = CreateResponse(json);

        var result = await OcpiResponseParser.ParseVersionedObjectAsync(
            response,
            OcpiVersion.V2_2_1,
            typeof(JsonElement)
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Value.Should().Be(2003);
    }
}
