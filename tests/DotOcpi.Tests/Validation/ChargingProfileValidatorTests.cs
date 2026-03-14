using DotOcpi.Models.V2_2_1;
using DotOcpi.Validation;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Validation;

public class ChargingProfileValidatorTests
{
    private readonly ChargingProfileValidator _validator = new();

    private static SetChargingProfile CreateValid() =>
        new()
        {
            ResponseUrl = "https://example.com/callback",
            ChargingProfile = new ChargingProfile
            {
                ChargingRateUnit = ChargingRateUnit.W,
                ChargingProfilePeriod = [new ChargingProfilePeriod { StartPeriod = 0, Limit = 32.0m }],
            },
        };

    [Fact]
    public void Validate_Valid_ReturnsValid()
    {
        var result = _validator.Validate(CreateValid());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_HttpUrl_ReturnsError()
    {
        var cmd = CreateValid() with { ResponseUrl = "http://example.com/callback" };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "COMMAND_INVALID_RESPONSE_URL");
    }

    [Fact]
    public void Validate_NoPeriods_ReturnsError()
    {
        var cmd = CreateValid() with
        {
            ChargingProfile = new ChargingProfile { ChargingRateUnit = ChargingRateUnit.W, ChargingProfilePeriod = [] },
        };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "CHARGING_PROFILE_NO_PERIODS");
    }

    [Fact]
    public void Validate_NegativeDuration_ReturnsError()
    {
        var cmd = CreateValid() with
        {
            ChargingProfile = new ChargingProfile
            {
                ChargingRateUnit = ChargingRateUnit.W,
                Duration = -1,
                ChargingProfilePeriod = [new ChargingProfilePeriod { StartPeriod = 0, Limit = 32.0m }],
            },
        };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "CHARGING_PROFILE_NEGATIVE_DURATION");
    }

    [Fact]
    public void Validate_ZeroDuration_ReturnsValid()
    {
        var cmd = CreateValid() with
        {
            ChargingProfile = new ChargingProfile
            {
                ChargingRateUnit = ChargingRateUnit.W,
                Duration = 0,
                ChargingProfilePeriod = [new ChargingProfilePeriod { StartPeriod = 0, Limit = 32.0m }],
            },
        };

        var result = _validator.Validate(cmd);

        result.Errors.Should().NotContain(e => e.Code == "CHARGING_PROFILE_NEGATIVE_DURATION");
    }

    [Fact]
    public void Validate_NegativeMinRate_ReturnsError()
    {
        var cmd = CreateValid() with
        {
            ChargingProfile = new ChargingProfile
            {
                ChargingRateUnit = ChargingRateUnit.A,
                MinChargingRate = -5.0m,
                ChargingProfilePeriod = [new ChargingProfilePeriod { StartPeriod = 0, Limit = 32.0m }],
            },
        };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "CHARGING_PROFILE_NEGATIVE_MIN_RATE");
    }
}
