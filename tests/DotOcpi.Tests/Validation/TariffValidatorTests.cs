using DotOcpi.Models.V2_2_1;
using DotOcpi.Tests.Fixtures;
using DotOcpi.Validation;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Validation;

public class TariffValidatorTests
{
    private readonly TariffValidator _validator = new();

    [Fact]
    public void Validate_ValidTariff_ReturnsValid()
    {
        var tariff = TestData.CreateTariff();

        var result = _validator.Validate(tariff);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidCurrency_ReturnsError()
    {
        var tariff = TestData.CreateTariff() with { Currency = "X" };

        var result = _validator.Validate(tariff);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TARIFF_INVALID_CURRENCY");
    }

    [Fact]
    public void Validate_EmptyElements_ReturnsError()
    {
        var tariff = TestData.CreateTariff() with { Elements = [] };

        var result = _validator.Validate(tariff);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TARIFF_NO_ELEMENTS");
    }

    [Fact]
    public void Validate_ElementWithNoPriceComponents_ReturnsError()
    {
        var tariff = TestData.CreateTariff() with { Elements = [new TariffElement { PriceComponents = [] }] };

        var result = _validator.Validate(tariff);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TARIFF_ELEMENT_NO_PRICE_COMPONENTS");
    }

    [Fact]
    public void Validate_ZeroStepSize_ReturnsError()
    {
        var tariff = TestData.CreateTariff() with
        {
            Elements = [new TariffElement { PriceComponents = [TestData.CreatePriceComponent(stepSize: 0)] }],
        };

        var result = _validator.Validate(tariff);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TARIFF_INVALID_STEP_SIZE");
    }

    [Fact]
    public void Validate_NegativeStepSize_ReturnsError()
    {
        var tariff = TestData.CreateTariff() with
        {
            Elements = [new TariffElement { PriceComponents = [TestData.CreatePriceComponent(stepSize: -1)] }],
        };

        var result = _validator.Validate(tariff);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TARIFF_INVALID_STEP_SIZE");
    }

    [Fact]
    public void Validate_EndBeforeStart_ReturnsError()
    {
        var start = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var tariff = TestData.CreateTariff() with { StartDateTime = start, EndDateTime = start.AddDays(-1) };

        var result = _validator.Validate(tariff);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TARIFF_END_BEFORE_START");
    }

    [Fact]
    public void Validate_NoStartOrEnd_ReturnsValid()
    {
        var tariff = TestData.CreateTariff() with { StartDateTime = null, EndDateTime = null };

        var result = _validator.Validate(tariff);

        result.Errors.Should().NotContain(e => e.Code == "TARIFF_END_BEFORE_START");
    }

    [Fact]
    public void Validate_MinExceedsMax_ReturnsError()
    {
        var tariff = TestData.CreateTariff() with
        {
            MinPrice = new Price { ExclVat = 10.0m },
            MaxPrice = new Price { ExclVat = 5.0m },
        };

        var result = _validator.Validate(tariff);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TARIFF_MIN_EXCEEDS_MAX");
    }

    [Fact]
    public void Validate_MinEqualsMax_ReturnsValid()
    {
        var tariff = TestData.CreateTariff() with
        {
            MinPrice = new Price { ExclVat = 5.0m },
            MaxPrice = new Price { ExclVat = 5.0m },
        };

        var result = _validator.Validate(tariff);

        result.Errors.Should().NotContain(e => e.Code == "TARIFF_MIN_EXCEEDS_MAX");
    }

    [Fact]
    public void Validate_StepSizeErrorIncludesPropertyPath()
    {
        var tariff = TestData.CreateTariff() with
        {
            Elements =
            [
                new TariffElement
                {
                    PriceComponents = [TestData.CreatePriceComponent(), TestData.CreatePriceComponent(stepSize: 0)],
                },
            ],
        };

        var result = _validator.Validate(tariff);

        result
            .Errors.Should()
            .ContainSingle(e => e.Code == "TARIFF_INVALID_STEP_SIZE")
            .Which.PropertyPath.Should()
            .Be("Elements[0].PriceComponents[1].StepSize");
    }
}
