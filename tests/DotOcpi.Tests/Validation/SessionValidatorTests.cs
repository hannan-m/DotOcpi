using DotOcpi.Models.V2_2_1;
using DotOcpi.Tests.Fixtures;
using DotOcpi.Validation;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Validation;

public class SessionValidatorTests
{
    private readonly SessionValidator _validator = new();

    [Fact]
    public void Validate_ValidSession_ReturnsValid()
    {
        var session = TestData.CreateSession();

        var result = _validator.Validate(session);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NegativeKwh_ReturnsError()
    {
        var session = TestData.CreateSession() with { Kwh = -1.0m };

        var result = _validator.Validate(session);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "SESSION_NEGATIVE_KWH");
    }

    [Fact]
    public void Validate_ZeroKwh_ReturnsValid()
    {
        var session = TestData.CreateSession() with { Kwh = 0m };

        var result = _validator.Validate(session);

        result.Errors.Should().NotContain(e => e.Code == "SESSION_NEGATIVE_KWH");
    }

    [Fact]
    public void Validate_EndBeforeStart_ReturnsError()
    {
        var start = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var session = TestData.CreateSession() with { StartDateTime = start, EndDateTime = start.AddHours(-1) };

        var result = _validator.Validate(session);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "SESSION_END_BEFORE_START");
    }

    [Fact]
    public void Validate_EndEqualsStart_ReturnsValid()
    {
        var ts = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var session = TestData.CreateSession() with { StartDateTime = ts, EndDateTime = ts };

        var result = _validator.Validate(session);

        result.Errors.Should().NotContain(e => e.Code == "SESSION_END_BEFORE_START");
    }

    [Fact]
    public void Validate_NoEndDateTime_ReturnsValid()
    {
        var session = TestData.CreateSession() with { EndDateTime = null };

        var result = _validator.Validate(session);

        result.Errors.Should().NotContain(e => e.Code == "SESSION_END_BEFORE_START");
    }

    [Fact]
    public void Validate_InvalidCurrency_ReturnsError()
    {
        var session = TestData.CreateSession() with { Currency = "AB" };

        var result = _validator.Validate(session);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "SESSION_INVALID_CURRENCY");
    }

    [Fact]
    public void Validate_NumericCurrency_ReturnsError()
    {
        var session = TestData.CreateSession() with { Currency = "123" };

        var result = _validator.Validate(session);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "SESSION_INVALID_CURRENCY");
    }

    [Fact]
    public void Validate_CompletedWithoutEnd_ReturnsError()
    {
        var session = TestData.CreateSession(status: SessionStatus.COMPLETED) with { EndDateTime = null };

        var result = _validator.Validate(session);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "SESSION_COMPLETED_WITHOUT_END");
    }

    [Fact]
    public void Validate_CompletedWithEnd_ReturnsValid()
    {
        var session = TestData.CreateSession(status: SessionStatus.COMPLETED) with
        {
            EndDateTime = new DateTimeOffset(2026, 1, 15, 14, 0, 0, TimeSpan.Zero),
        };

        var result = _validator.Validate(session);

        result.Errors.Should().NotContain(e => e.Code == "SESSION_COMPLETED_WITHOUT_END");
    }

    [Fact]
    public void Validate_ActiveWithoutEnd_ReturnsValid()
    {
        var session = TestData.CreateSession(status: SessionStatus.ACTIVE) with { EndDateTime = null };

        var result = _validator.Validate(session);

        result.Errors.Should().NotContain(e => e.Code == "SESSION_COMPLETED_WITHOUT_END");
    }
}
