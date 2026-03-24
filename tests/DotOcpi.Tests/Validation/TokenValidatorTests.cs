using DotOcpi.Models.V2_2_1;
using DotOcpi.Tests.Fixtures;
using DotOcpi.Validation;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Validation;

[Trait("Category", "Security")]
public class TokenValidatorTests
{
    private readonly TokenValidator _validator = new();

    [Fact]
    public void Validate_ValidToken_ReturnsValid()
    {
        var token = TestData.CreateToken();

        var result = _validator.Validate(token);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidCountryCode_ReturnsError()
    {
        var token = TestData.CreateToken() with { CountryCode = new CiString("X") };

        var result = _validator.Validate(token);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TOKEN_INVALID_COUNTRY_CODE");
    }

    [Fact]
    public void Validate_NumericCountryCode_ReturnsError()
    {
        var token = TestData.CreateToken() with { CountryCode = new CiString("12") };

        var result = _validator.Validate(token);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TOKEN_INVALID_COUNTRY_CODE");
    }

    [Fact]
    public void Validate_InvalidLanguage_ReturnsError()
    {
        var token = TestData.CreateToken() with { Language = "X" };

        var result = _validator.Validate(token);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TOKEN_INVALID_LANGUAGE");
    }

    [Fact]
    public void Validate_NumericLanguage_ReturnsError()
    {
        var token = TestData.CreateToken() with { Language = "12" };

        var result = _validator.Validate(token);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TOKEN_INVALID_LANGUAGE");
    }

    [Fact]
    public void Validate_NullLanguage_ReturnsValid()
    {
        var token = TestData.CreateToken() with { Language = null };

        var result = _validator.Validate(token);

        result.Errors.Should().NotContain(e => e.Code == "TOKEN_INVALID_LANGUAGE");
    }

    [Fact]
    public void Validate_ValidLanguage_ReturnsValid()
    {
        var token = TestData.CreateToken() with { Language = "nl" };

        var result = _validator.Validate(token);

        result.Errors.Should().NotContain(e => e.Code == "TOKEN_INVALID_LANGUAGE");
    }

    [Fact]
    public void Validate_InvalidWithAlwaysWhitelist_ReturnsError()
    {
        var token = TestData.CreateToken() with { Valid = false, Whitelist = WhitelistType.ALWAYS };

        var result = _validator.Validate(token);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TOKEN_INVALID_ALWAYS_WHITELIST");
    }

    [Fact]
    public void Validate_ValidWithAlwaysWhitelist_ReturnsValid()
    {
        var token = TestData.CreateToken() with { Valid = true, Whitelist = WhitelistType.ALWAYS };

        var result = _validator.Validate(token);

        result.Errors.Should().NotContain(e => e.Code == "TOKEN_INVALID_ALWAYS_WHITELIST");
    }

    [Fact]
    public void Validate_InvalidWithNeverWhitelist_ReturnsValid()
    {
        var token = TestData.CreateToken() with { Valid = false, Whitelist = WhitelistType.NEVER };

        var result = _validator.Validate(token);

        result.Errors.Should().NotContain(e => e.Code == "TOKEN_INVALID_ALWAYS_WHITELIST");
    }
}
