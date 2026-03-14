using DotOcpi.Models.V2_2_1;
using DotOcpi.Tests.Fixtures;
using DotOcpi.Validation;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Validation;

public class LocationValidatorTests
{
    private readonly LocationValidator _validator = new();

    [Fact]
    public void Validate_ValidLocation_ReturnsValid()
    {
        var location = TestData.CreateLocation(evse: TestData.CreateEvse());

        var result = _validator.Validate(location);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValidLocationWithoutEvses_ReturnsValid()
    {
        var location = TestData.CreateLocation();

        var result = _validator.Validate(location);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidLatitude_ReturnsError()
    {
        var location = TestData.CreateLocation() with { Coordinates = new GeoLocation("91.0", "4.891860") };

        var result = _validator.Validate(location);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "LOCATION_INVALID_LATITUDE");
    }

    [Fact]
    public void Validate_InvalidLongitude_ReturnsError()
    {
        var location = TestData.CreateLocation() with { Coordinates = new GeoLocation("52.364115", "-181.0") };

        var result = _validator.Validate(location);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "LOCATION_INVALID_LONGITUDE");
    }

    [Fact]
    public void Validate_NonNumericLatitude_ReturnsError()
    {
        var location = TestData.CreateLocation() with { Coordinates = new GeoLocation("not_a_number", "4.891860") };

        var result = _validator.Validate(location);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "LOCATION_INVALID_LATITUDE");
    }

    [Fact]
    public void Validate_BoundaryLatitude_ReturnsValid()
    {
        var location = TestData.CreateLocation() with { Coordinates = new GeoLocation("90.0", "180.0") };

        var result = _validator.Validate(location);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NegativeBoundary_ReturnsValid()
    {
        var location = TestData.CreateLocation() with { Coordinates = new GeoLocation("-90.0", "-180.0") };

        var result = _validator.Validate(location);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidCountryCode_ReturnsError()
    {
        var location = TestData.CreateLocation() with { CountryCode = new CiString("X") };

        var result = _validator.Validate(location);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "LOCATION_INVALID_COUNTRY_CODE");
    }

    [Fact]
    public void Validate_NumericCountryCode_ReturnsError()
    {
        var location = TestData.CreateLocation() with { CountryCode = new CiString("12") };

        var result = _validator.Validate(location);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "LOCATION_INVALID_COUNTRY_CODE");
    }

    [Fact]
    public void Validate_InvalidCountryAlpha3_ReturnsError()
    {
        var location = TestData.CreateLocation() with { Country = "XX" };

        var result = _validator.Validate(location);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "LOCATION_INVALID_COUNTRY");
    }

    [Fact]
    public void Validate_PublishFalseWithoutAllowedTo_ReturnsError()
    {
        var location = TestData.CreateLocation() with { Publish = false, PublishAllowedTo = null };

        var result = _validator.Validate(location);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "LOCATION_PUBLISH_MISSING_ALLOWED_TO");
    }

    [Fact]
    public void Validate_PublishFalseWithEmptyAllowedTo_ReturnsError()
    {
        var location = TestData.CreateLocation() with { Publish = false, PublishAllowedTo = [] };

        var result = _validator.Validate(location);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "LOCATION_PUBLISH_MISSING_ALLOWED_TO");
    }

    [Fact]
    public void Validate_PublishFalseWithAllowedTo_ReturnsValid()
    {
        var location = TestData.CreateLocation() with
        {
            Publish = false,
            PublishAllowedTo = [new PublishTokenType { Type = TokenType.RFID }],
        };

        var result = _validator.Validate(location);

        result.Errors.Should().NotContain(e => e.Code == "LOCATION_PUBLISH_MISSING_ALLOWED_TO");
    }

    [Fact]
    public void Validate_DuplicateEvseUids_ReturnsError()
    {
        var evse1 = TestData.CreateEvse() with { Uid = new CiString("EVSE1") };
        var evse2 = TestData.CreateEvse() with { Uid = new CiString("EVSE1") };
        var location = TestData.CreateLocation() with { Evses = [evse1, evse2] };

        var result = _validator.Validate(location);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "LOCATION_DUPLICATE_EVSE_UID");
    }

    [Fact]
    public void Validate_DuplicateConnectorIds_ReturnsError()
    {
        var connector1 = TestData.CreateConnector() with { Id = new CiString("1") };
        var connector2 = TestData.CreateConnector() with { Id = new CiString("1") };
        var evse = TestData.CreateEvse() with { Connectors = [connector1, connector2] };
        var location = TestData.CreateLocation() with { Evses = [evse] };

        var result = _validator.Validate(location);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "EVSE_DUPLICATE_CONNECTOR_ID");
    }

    [Fact]
    public void Validate_EmptyConnectors_ReturnsError()
    {
        var evse = TestData.CreateEvse() with { Connectors = [] };
        var location = TestData.CreateLocation() with { Evses = [evse] };

        var result = _validator.Validate(location);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "EVSE_NO_CONNECTORS");
    }

    [Fact]
    public void Validate_EvseInvalidCoordinates_ReturnsError()
    {
        var evse = TestData.CreateEvse() with { Coordinates = new GeoLocation("999", "999") };
        var location = TestData.CreateLocation() with { Evses = [evse] };

        var result = _validator.Validate(location);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "LOCATION_INVALID_LATITUDE");
        result.Errors.Should().Contain(e => e.Code == "LOCATION_INVALID_LONGITUDE");
    }

    [Fact]
    public void Validate_ErrorIncludesPropertyPath()
    {
        var location = TestData.CreateLocation() with { Coordinates = new GeoLocation("91.0", "4.891860") };

        var result = _validator.Validate(location);

        result.Errors.Should().ContainSingle().Which.PropertyPath.Should().Be("Coordinates.Latitude");
    }

    [Fact]
    public void Validate_MultipleErrors_ReportsAll()
    {
        var location = TestData.CreateLocation() with
        {
            CountryCode = new CiString("X"),
            Country = "XX",
            Coordinates = new GeoLocation("999", "999"),
        };

        var result = _validator.Validate(location);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(4);
    }
}
