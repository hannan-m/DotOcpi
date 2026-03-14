using DotOcpi.Models.V2_2_1;
using DotOcpi.Tests.Fixtures;
using DotOcpi.Validation;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Validation;

public class CredentialsValidatorTests
{
    private readonly CredentialsValidator _validator = new();

    [Fact]
    public void Validate_ValidCredentials_ReturnsValid()
    {
        var credentials = TestData.CreateCredentials();

        var result = _validator.Validate(credentials);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_HttpUrl_ReturnsError()
    {
        var credentials = TestData.CreateCredentials() with { Url = "http://example.com/versions" };

        var result = _validator.Validate(credentials);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "CREDENTIALS_INVALID_URL");
    }

    [Fact]
    public void Validate_InvalidUrl_ReturnsError()
    {
        var credentials = TestData.CreateCredentials() with { Url = "not-a-url" };

        var result = _validator.Validate(credentials);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "CREDENTIALS_INVALID_URL");
    }

    [Fact]
    public void Validate_EmptyToken_ReturnsError()
    {
        var credentials = TestData.CreateCredentials() with { Token = "" };

        var result = _validator.Validate(credentials);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "CREDENTIALS_EMPTY_TOKEN");
    }

    [Fact]
    public void Validate_WhitespaceToken_ReturnsError()
    {
        var credentials = TestData.CreateCredentials() with { Token = "   " };

        var result = _validator.Validate(credentials);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "CREDENTIALS_EMPTY_TOKEN");
    }

    [Fact]
    public void Validate_EmptyRoles_ReturnsError()
    {
        var credentials = TestData.CreateCredentials() with { Roles = [] };

        var result = _validator.Validate(credentials);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "CREDENTIALS_NO_ROLES");
    }

    [Fact]
    public void Validate_RoleWithInvalidCountryCode_ReturnsError()
    {
        var credentials = TestData.CreateCredentials() with
        {
            Roles =
            [
                new CredentialsRole
                {
                    Role = Role.CPO,
                    CountryCode = new CiString("X"),
                    PartyId = new CiString("TNM"),
                    BusinessDetails = new BusinessDetails { Name = "Test" },
                },
            ],
        };

        var result = _validator.Validate(credentials);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "CREDENTIALS_ROLE_INVALID_COUNTRY_CODE");
    }

    [Fact]
    public void Validate_MultipleRoles_ValidatesEach()
    {
        var credentials = TestData.CreateCredentials() with
        {
            Roles =
            [
                new CredentialsRole
                {
                    Role = Role.CPO,
                    CountryCode = new CiString("12"),
                    PartyId = new CiString("TNM"),
                    BusinessDetails = new BusinessDetails { Name = "Test1" },
                },
                new CredentialsRole
                {
                    Role = Role.EMSP,
                    CountryCode = new CiString("AB"),
                    PartyId = new CiString("XYZ"),
                    BusinessDetails = new BusinessDetails { Name = "Test2" },
                },
            ],
        };

        var result = _validator.Validate(credentials);

        result.Errors.Should().ContainSingle(e => e.Code == "CREDENTIALS_ROLE_INVALID_COUNTRY_CODE");
        result.Errors.Should().ContainSingle().Which.PropertyPath.Should().Be("Roles[0].CountryCode");
    }
}
