using DotOcpi.Models.V2_2_1;
using DotOcpi.Tests.Fixtures;
using DotOcpi.Validation;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Validation;

public class CdrValidatorTests
{
    private readonly CdrValidator _validator = new();

    [Fact]
    public void Validate_ValidCdr_ReturnsValid()
    {
        var cdr = TestData.CreateCdr();

        var result = _validator.Validate(cdr);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EndBeforeStart_ReturnsError()
    {
        var start = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var cdr = TestData.CreateCdr() with { StartDateTime = start, EndDateTime = start.AddHours(-1) };

        var result = _validator.Validate(cdr);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "CDR_END_BEFORE_START");
    }

    [Fact]
    public void Validate_NegativeEnergy_ReturnsError()
    {
        var cdr = TestData.CreateCdr() with { TotalEnergy = -1.0m };

        var result = _validator.Validate(cdr);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "CDR_NEGATIVE_ENERGY");
    }

    [Fact]
    public void Validate_ZeroEnergy_ReturnsValid()
    {
        var cdr = TestData.CreateCdr() with { TotalEnergy = 0m };

        var result = _validator.Validate(cdr);

        result.Errors.Should().NotContain(e => e.Code == "CDR_NEGATIVE_ENERGY");
    }

    [Fact]
    public void Validate_NegativeTime_ReturnsError()
    {
        var cdr = TestData.CreateCdr() with { TotalTime = -1.0m };

        var result = _validator.Validate(cdr);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "CDR_NEGATIVE_TIME");
    }

    [Fact]
    public void Validate_NegativeParkingTime_ReturnsError()
    {
        var cdr = TestData.CreateCdr() with { TotalParkingTime = -0.5m };

        var result = _validator.Validate(cdr);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "CDR_NEGATIVE_PARKING_TIME");
    }

    [Fact]
    public void Validate_NullParkingTime_ReturnsValid()
    {
        var cdr = TestData.CreateCdr() with { TotalParkingTime = null };

        var result = _validator.Validate(cdr);

        result.Errors.Should().NotContain(e => e.Code == "CDR_NEGATIVE_PARKING_TIME");
    }

    [Fact]
    public void Validate_InvalidCurrency_ReturnsError()
    {
        var cdr = TestData.CreateCdr() with { Currency = "12" };

        var result = _validator.Validate(cdr);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "CDR_INVALID_CURRENCY");
    }

    [Fact]
    public void Validate_EmptyChargingPeriods_ReturnsError()
    {
        var cdr = TestData.CreateCdr() with { ChargingPeriods = [] };

        var result = _validator.Validate(cdr);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "CDR_NO_CHARGING_PERIODS");
    }

    [Fact]
    public void Validate_CreditWithoutReference_ReturnsError()
    {
        var cdr = TestData.CreateCdr() with { Credit = true, CreditReferenceId = null };

        var result = _validator.Validate(cdr);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "CDR_CREDIT_MISSING_REFERENCE");
    }

    [Fact]
    public void Validate_CreditWithReference_ReturnsValid()
    {
        var cdr = TestData.CreateCdr() with { Credit = true, CreditReferenceId = new CiString("CDR-000") };

        var result = _validator.Validate(cdr);

        result.Errors.Should().NotContain(e => e.Code == "CDR_CREDIT_MISSING_REFERENCE");
    }

    [Fact]
    public void Validate_NegativeTotalCost_ReturnsError()
    {
        var cdr = TestData.CreateCdr() with { TotalCost = new Price { ExclVat = -1.0m } };

        var result = _validator.Validate(cdr);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "CDR_NEGATIVE_TOTAL_COST");
    }

    [Fact]
    public void Validate_MultipleErrors_ReportsAll()
    {
        var start = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var cdr = TestData.CreateCdr() with
        {
            StartDateTime = start,
            EndDateTime = start.AddHours(-1),
            TotalEnergy = -1m,
            Currency = "X",
        };

        var result = _validator.Validate(cdr);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(3);
    }
}
