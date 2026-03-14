using System.Text;
using DotOcpi.AspNetCore.Handlers.ChargingProfiles;
using DotOcpi.Modules;
using DotOcpi.Registry;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace DotOcpi.AspNetCore.Tests.Handlers.ChargingProfiles;

public class ChargingProfilesEndpointsTests
{
    private static readonly CpoConnection TestConnection = new()
    {
        CpoCountryCode = "DE",
        CpoPartyId = "ALL",
        EmspCountryCode = "NL",
        EmspPartyId = "TNM",
        Version = OcpiVersion.V2_2_1,
        ModuleEndpoints = new Dictionary<string, string>(),
        TokenBHash = "hash",
        Status = ConnectionStatus.Connected,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private static OcpiRequestContext CreateContext(OcpiVersion version) =>
        new()
        {
            Connection = TestConnection with { Version = version },
            RequestId = "req-1",
            CorrelationId = "corr-1",
            CpoId = "DE_ALL",
            CpoIdentity = new PartyIdentity("DE", "ALL"),
            EmspIdentity = new PartyIdentity("NL", "TNM"),
            NegotiatedVersion = version,
            ModuleId = "chargingprofiles",
        };

    private static DefaultHttpContext CreateHttpContext(
        IChargingProfilesCallback callback,
        OcpiVersion version,
        string? body = null
    )
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestServices = new ServiceCollection().AddSingleton(callback).BuildServiceProvider();
        httpContext.SetOcpiContext(CreateContext(version));

        if (body is not null)
        {
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            httpContext.Request.ContentType = "application/json";
        }

        return httpContext;
    }

    [Fact]
    public async Task HandleChargingProfileResult_CallsCallback()
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
    public async Task HandleActiveChargingProfileUpdate_CallsCallback()
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
}
