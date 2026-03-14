using DotOcpi.AspNetCore.Handlers.ChargingProfiles;
using DotOcpi.Modules;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;
using static DotOcpi.AspNetCore.Tests.Handlers.OcpiEndpointTestHelper;

namespace DotOcpi.AspNetCore.Tests.Handlers.ChargingProfiles;

public class ChargingProfilesEndpointsTests
{
    private static DefaultHttpContext CreateHttpContext(
        IChargingProfilesCallback callback,
        OcpiVersion version,
        string? body = null
    ) => OcpiEndpointTestHelper.CreateHttpContext(callback, version, "chargingprofiles", body);

    [Fact]
    public async Task HandleChargingProfileResult_DeserializesAndReturnsSuccess()
    {
        var callback = Substitute.For<IChargingProfilesCallback>();
        callback
            .OnChargingProfileResultAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Success());

        var httpContext = CreateHttpContext(callback, OcpiVersion.V2_2_1, """{"result": "ACCEPTED"}""");
        httpContext.Request.RouteValues["correlationId"] = "cp-123";

        await ChargingProfilesEndpoints.HandleChargingProfileResult(httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("1000");

        await callback
            .Received(1)
            .OnChargingProfileResultAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Is("cp-123"),
                Arg.Is<object>(o => o is Models.V2_2_1.ChargingProfileResult),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleActiveChargingProfileUpdate_DeserializesAndReturnsSuccess()
    {
        var callback = Substitute.For<IChargingProfilesCallback>();
        callback
            .OnActiveChargingProfileUpdateAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Success());

        var httpContext = CreateHttpContext(
            callback,
            OcpiVersion.V2_2_1,
            """
            {
                "start_date_time": "2024-01-01T10:00:00Z",
                "charging_profile": {
                    "charging_rate_unit": "W",
                    "charging_profile_period": [{"start_period": 0, "limit": 11000.0}]
                }
            }
            """
        );
        httpContext.Request.RouteValues["sessionId"] = "SES1";

        await ChargingProfilesEndpoints.HandleActiveChargingProfileUpdate(httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("1000");

        await callback
            .Received(1)
            .OnActiveChargingProfileUpdateAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Is("SES1"),
                Arg.Is<object>(o => o is Models.V2_2_1.ActiveChargingProfile),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleChargingProfileResult_EmptyBody_Returns400()
    {
        var callback = Substitute.For<IChargingProfilesCallback>();
        var httpContext = CreateHttpContext(callback, OcpiVersion.V2_2_1);
        httpContext.Request.Body = new MemoryStream(Array.Empty<byte>());
        httpContext.Request.RouteValues["correlationId"] = "cp-789";

        await ChargingProfilesEndpoints.HandleChargingProfileResult(httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleChargingProfileResult_InvalidJson_Returns400()
    {
        var callback = Substitute.For<IChargingProfilesCallback>();
        var httpContext = CreateHttpContext(callback, OcpiVersion.V2_2_1, "broken json{");
        httpContext.Request.RouteValues["correlationId"] = "cp-000";

        await ChargingProfilesEndpoints.HandleChargingProfileResult(httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("invalid JSON");
    }

    [Fact]
    public async Task HandleChargingProfileResult_CallbackFailure_Returns400()
    {
        var callback = Substitute.For<IChargingProfilesCallback>();
        callback
            .OnChargingProfileResultAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Failure(OcpiStatusCode.GenericClientError, "Unknown correlation"));

        var httpContext = CreateHttpContext(callback, OcpiVersion.V2_2_1, """{"result": "ACCEPTED"}""");
        httpContext.Request.RouteValues["correlationId"] = "unknown";

        await ChargingProfilesEndpoints.HandleChargingProfileResult(httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("2000");
    }
}
