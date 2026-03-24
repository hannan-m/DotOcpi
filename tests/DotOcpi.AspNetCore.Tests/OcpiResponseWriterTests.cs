using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace DotOcpi.AspNetCore.Tests;

public class OcpiResponseWriterTests
{
    private static DefaultHttpContext CreateHttpContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private static async Task<JsonElement> ReadResponseAsync(HttpContext ctx)
    {
        ctx.Response.Body.Position = 0;
        return await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Response.Body);
    }

    [Fact]
    public async Task WriteSuccessNoDataAsync_WritesCorrectEnvelope()
    {
        var ctx = CreateHttpContext();

        await OcpiResponseWriter.WriteSuccessNoDataAsync(ctx);

        var body = await ReadResponseAsync(ctx);
        body.GetProperty("status_code").GetInt32().Should().Be(1000);
        body.TryGetProperty("timestamp", out _).Should().BeTrue();
        ctx.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task WriteSuccessNoDataAsync_IncludesMessageWhenProvided()
    {
        var ctx = CreateHttpContext();

        await OcpiResponseWriter.WriteSuccessNoDataAsync(ctx, statusMessage: "All good");

        var body = await ReadResponseAsync(ctx);
        body.GetProperty("status_message").GetString().Should().Be("All good");
    }

    [Fact]
    public async Task WriteSuccessNoDataAsync_OmitsMessageWhenNull()
    {
        var ctx = CreateHttpContext();

        await OcpiResponseWriter.WriteSuccessNoDataAsync(ctx);

        var body = await ReadResponseAsync(ctx);
        body.TryGetProperty("status_message", out _).Should().BeFalse();
    }

    [Fact]
    public async Task WriteErrorAsync_SetsHttpStatusAndOcpiStatus()
    {
        var ctx = CreateHttpContext();

        await OcpiResponseWriter.WriteErrorAsync(ctx, 400, 2001, "Invalid parameters");

        ctx.Response.StatusCode.Should().Be(400);
        var body = await ReadResponseAsync(ctx);
        body.GetProperty("status_code").GetInt32().Should().Be(2001);
        body.GetProperty("status_message").GetString().Should().Be("Invalid parameters");
    }

    [Fact]
    public async Task WriteErrorAsync_ServerError_Sets500()
    {
        var ctx = CreateHttpContext();

        await OcpiResponseWriter.WriteErrorAsync(ctx, 500, 3000, "Internal error");

        ctx.Response.StatusCode.Should().Be(500);
        var body = await ReadResponseAsync(ctx);
        body.GetProperty("status_code").GetInt32().Should().Be(3000);
    }

    [Fact]
    public async Task WriteSuccessObjectAsync_IncludesDataPayload()
    {
        var ctx = CreateHttpContext();
        var location = new Models.V2_2_1.Location
        {
            CountryCode = new CiString("DE"),
            PartyId = new CiString("ALL"),
            Id = "LOC1",
            Publish = true,
            Address = "123 Main St",
            City = "Berlin",
            Country = "DEU",
            Coordinates = new GeoLocation("52.5200", "13.4050"),
            ParkingType = Models.V2_2_1.ParkingType.ON_STREET,
            TimeZone = "Europe/Berlin",
            LastUpdated = DateTimeOffset.Parse(
                "2024-01-15T10:00:00Z",
                System.Globalization.CultureInfo.InvariantCulture
            ),
        };

        await OcpiResponseWriter.WriteSuccessObjectAsync(ctx, location, OcpiVersion.V2_2_1);

        var body = await ReadResponseAsync(ctx);
        body.GetProperty("status_code").GetInt32().Should().Be(1000);
        body.GetProperty("data").GetProperty("id").GetString().Should().Be("LOC1");
        body.GetProperty("data").GetProperty("country_code").GetString().Should().Be("DE");
    }

    [Fact]
    public async Task WriteSuccessObjectAsync_NullData_OmitsDataField()
    {
        var ctx = CreateHttpContext();

        await OcpiResponseWriter.WriteSuccessObjectAsync(ctx, null, OcpiVersion.V2_2_1);

        var body = await ReadResponseAsync(ctx);
        body.GetProperty("status_code").GetInt32().Should().Be(1000);
        body.TryGetProperty("data", out _).Should().BeFalse();
    }

    [Fact]
    public async Task WriteResultAsync_Success_Returns200()
    {
        var ctx = CreateHttpContext();
        var result = OcpiResult.Success("Done");

        await OcpiResponseWriter.WriteResultAsync(ctx, result);

        ctx.Response.StatusCode.Should().Be(200);
        var body = await ReadResponseAsync(ctx);
        body.GetProperty("status_code").GetInt32().Should().Be(1000);
        body.GetProperty("status_message").GetString().Should().Be("Done");
    }

    [Fact]
    public async Task WriteResultAsync_ClientError_Returns400()
    {
        var ctx = CreateHttpContext();
        var result = OcpiResult.Failure(OcpiStatusCode.InvalidParameters, "Bad input");

        await OcpiResponseWriter.WriteResultAsync(ctx, result);

        ctx.Response.StatusCode.Should().Be(400);
        var body = await ReadResponseAsync(ctx);
        body.GetProperty("status_code").GetInt32().Should().Be(2001);
    }

    [Fact]
    public async Task WriteResultAsync_ServerError_Returns500()
    {
        var ctx = CreateHttpContext();
        var result = OcpiResult.Failure(OcpiStatusCode.GenericServerError, "Server broke");

        await OcpiResponseWriter.WriteResultAsync(ctx, result);

        ctx.Response.StatusCode.Should().Be(500);
        var body = await ReadResponseAsync(ctx);
        body.GetProperty("status_code").GetInt32().Should().Be(3000);
    }

    [Fact]
    public async Task WriteResultObjectAsync_Success_IncludesData()
    {
        var ctx = CreateHttpContext();
        var data = new Models.V2_2_1.Location
        {
            CountryCode = new CiString("DE"),
            PartyId = new CiString("ALL"),
            Id = "LOC1",
            Publish = true,
            Address = "123 Main St",
            City = "Berlin",
            Country = "DEU",
            Coordinates = new GeoLocation("52.5200", "13.4050"),
            ParkingType = Models.V2_2_1.ParkingType.ON_STREET,
            TimeZone = "Europe/Berlin",
            LastUpdated = DateTimeOffset.Parse(
                "2024-01-15T10:00:00Z",
                System.Globalization.CultureInfo.InvariantCulture
            ),
        };
        var result = OcpiResult<object>.Success(data);

        await OcpiResponseWriter.WriteResultObjectAsync(ctx, result, OcpiVersion.V2_2_1);

        ctx.Response.StatusCode.Should().Be(200);
        var body = await ReadResponseAsync(ctx);
        body.GetProperty("data").GetProperty("id").GetString().Should().Be("LOC1");
    }

    [Fact]
    public async Task WriteResultObjectAsync_Failure_ReturnsError()
    {
        var ctx = CreateHttpContext();
        var result = OcpiResult<object>.Failure(OcpiStatusCode.UnknownLocation, "Not found");

        await OcpiResponseWriter.WriteResultObjectAsync(ctx, result, OcpiVersion.V2_2_1);

        ctx.Response.StatusCode.Should().Be(400);
        var body = await ReadResponseAsync(ctx);
        body.GetProperty("status_code").GetInt32().Should().Be(2003);
        body.GetProperty("status_message").GetString().Should().Be("Not found");
    }

    [Fact]
    public async Task WriteSuccessListAsync_WritesArrayData()
    {
        var ctx = CreateHttpContext();
        var items = new List<object>
        {
            new Models.V2_2_1.Location
            {
                CountryCode = new CiString("DE"),
                PartyId = new CiString("ALL"),
                Id = "LOC1",
                Publish = true,
                Address = "123 Main St",
                City = "Berlin",
                Country = "DEU",
                Coordinates = new GeoLocation("52.5200", "13.4050"),
                ParkingType = Models.V2_2_1.ParkingType.ON_STREET,
                TimeZone = "Europe/Berlin",
                LastUpdated = DateTimeOffset.Parse(
                    "2024-01-15T10:00:00Z",
                    System.Globalization.CultureInfo.InvariantCulture
                ),
            },
        };

        await OcpiResponseWriter.WriteSuccessListAsync(ctx, items, OcpiVersion.V2_2_1);

        var body = await ReadResponseAsync(ctx);
        body.GetProperty("status_code").GetInt32().Should().Be(1000);
        body.GetProperty("data").GetArrayLength().Should().Be(1);
        body.GetProperty("data")[0].GetProperty("id").GetString().Should().Be("LOC1");
    }

    [Fact]
    public async Task ValidationErrorResult_Returns400WithErrors()
    {
        var ctx = CreateHttpContext();
        var errors = new[]
        {
            new Validation.OcpiValidationError("VAL001", "Name required", "Set Name") { PropertyPath = "name" },
        };

        var result = OcpiResponseWriter.ValidationErrorResult(errors);
        await result.ExecuteAsync(ctx);

        ctx.Response.StatusCode.Should().Be(400);
        var body = await ReadResponseAsync(ctx);
        body.GetProperty("status_code").GetInt32().Should().Be(2001);
        body.GetProperty("data").GetArrayLength().Should().Be(1);
        body.GetProperty("data")[0].GetProperty("code").GetString().Should().Be("VAL001");
        body.GetProperty("data")[0].GetProperty("property_path").GetString().Should().Be("name");
    }
}
