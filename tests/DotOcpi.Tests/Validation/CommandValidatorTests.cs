using DotOcpi.Models.V2_2_1;
using DotOcpi.Tests.Fixtures;
using DotOcpi.Validation;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Validation;

public class CommandValidatorTests
{
    private readonly CommandValidator _validator = new();

    [Fact]
    public void Validate_StartSession_ValidUrl_ReturnsValid()
    {
        var cmd = new StartSession
        {
            ResponseUrl = "https://example.com/callback",
            Token = TestData.CreateToken(),
            LocationId = new CiString("LOC1"),
        };

        var result = ((IOcpiValidator<StartSession>)_validator).Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_StartSession_HttpUrl_ReturnsError()
    {
        var cmd = new StartSession
        {
            ResponseUrl = "http://example.com/callback",
            Token = TestData.CreateToken(),
            LocationId = new CiString("LOC1"),
        };

        var result = ((IOcpiValidator<StartSession>)_validator).Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "COMMAND_INVALID_RESPONSE_URL");
    }

    [Fact]
    public void Validate_StartSession_InvalidUrl_ReturnsError()
    {
        var cmd = new StartSession
        {
            ResponseUrl = "not-a-url",
            Token = TestData.CreateToken(),
            LocationId = new CiString("LOC1"),
        };

        var result = ((IOcpiValidator<StartSession>)_validator).Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "COMMAND_INVALID_RESPONSE_URL");
    }

    [Fact]
    public void Validate_StopSession_ValidUrl_ReturnsValid()
    {
        var cmd = new StopSession { ResponseUrl = "https://example.com/callback", SessionId = new CiString("SES-001") };

        var result = ((IOcpiValidator<StopSession>)_validator).Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_StopSession_HttpUrl_ReturnsError()
    {
        var cmd = new StopSession { ResponseUrl = "http://example.com/callback", SessionId = new CiString("SES-001") };

        var result = ((IOcpiValidator<StopSession>)_validator).Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "COMMAND_INVALID_RESPONSE_URL");
    }

    [Fact]
    public void Validate_ReserveNow_Valid_ReturnsValid()
    {
        var cmd = new ReserveNow
        {
            ResponseUrl = "https://example.com/callback",
            Token = TestData.CreateToken(),
            ExpiryDate = DateTimeOffset.UtcNow.AddHours(1),
            ReservationId = new CiString("RSV-001"),
            LocationId = new CiString("LOC1"),
        };

        var result = ((IOcpiValidator<ReserveNow>)_validator).Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ReserveNow_PastExpiry_ReturnsError()
    {
        var cmd = new ReserveNow
        {
            ResponseUrl = "https://example.com/callback",
            Token = TestData.CreateToken(),
            ExpiryDate = DateTimeOffset.UtcNow.AddHours(-1),
            ReservationId = new CiString("RSV-001"),
            LocationId = new CiString("LOC1"),
        };

        var result = ((IOcpiValidator<ReserveNow>)_validator).Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "COMMAND_EXPIRY_IN_PAST");
    }

    [Fact]
    public void Validate_CancelReservation_ValidUrl_ReturnsValid()
    {
        var cmd = new CancelReservation
        {
            ResponseUrl = "https://example.com/callback",
            ReservationId = new CiString("RSV-001"),
        };

        var result = ((IOcpiValidator<CancelReservation>)_validator).Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_UnlockConnector_ValidUrl_ReturnsValid()
    {
        var cmd = new UnlockConnector
        {
            ResponseUrl = "https://example.com/callback",
            LocationId = new CiString("LOC1"),
            EvseUid = new CiString("3256"),
            ConnectorId = new CiString("1"),
        };

        var result = ((IOcpiValidator<UnlockConnector>)_validator).Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_UnlockConnector_HttpUrl_ReturnsError()
    {
        var cmd = new UnlockConnector
        {
            ResponseUrl = "http://insecure.example.com/callback",
            LocationId = new CiString("LOC1"),
            EvseUid = new CiString("3256"),
            ConnectorId = new CiString("1"),
        };

        var result = ((IOcpiValidator<UnlockConnector>)_validator).Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "COMMAND_INVALID_RESPONSE_URL");
    }
}
