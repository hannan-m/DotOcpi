using System.Text.Json;
using DotOcpi.Serialization;
using DotOcpi.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Serialization;

public class SnakeCaseTests
{
    private static readonly JsonSerializerOptions Options = OcpiJsonOptions.V2_2_1;

    [Fact]
    public void Location_PropertyNames_AreSnakeCase()
    {
        var location = TestData.CreateLocation();

        var json = JsonSerializer.Serialize(location, Options);

        json.Should().Contain("\"country_code\":");
        json.Should().Contain("\"party_id\":");
        json.Should().Contain("\"time_zone\":");
        json.Should().Contain("\"last_updated\":");
    }

    [Fact]
    public void Session_PropertyNames_AreSnakeCase()
    {
        var session = TestData.CreateSession();

        var json = JsonSerializer.Serialize(session, Options);

        json.Should().Contain("\"start_date_time\":");
        json.Should().Contain("\"cdr_token\":");
        json.Should().Contain("\"auth_method\":");
        json.Should().Contain("\"location_id\":");
        json.Should().Contain("\"evse_uid\":");
        json.Should().Contain("\"connector_id\":");
    }

    [Fact]
    public void Connector_PropertyNames_AreSnakeCase()
    {
        var connector = TestData.CreateConnector();

        var json = JsonSerializer.Serialize(connector, Options);

        json.Should().Contain("\"max_voltage\":");
        json.Should().Contain("\"max_amperage\":");
        json.Should().Contain("\"power_type\":");
    }

    [Fact]
    public void NullOptionalFields_AreOmitted()
    {
        var location = TestData.CreateLocation();

        var json = JsonSerializer.Serialize(location, Options);

        json.Should().NotContain("\"name\":");
        json.Should().NotContain("\"postal_code\":");
        json.Should().NotContain("\"evses\":");
    }

    [Fact]
    public void DateTimes_AreUtcRfc3339()
    {
        var session = TestData.CreateSession();

        var json = JsonSerializer.Serialize(session, Options);

        json.Should().Contain("\"2026-01-15T12:00:00Z\"");
    }

    [Fact]
    public void Enums_AreUppercaseStrings()
    {
        var session = TestData.CreateSession();

        var json = JsonSerializer.Serialize(session, Options);

        json.Should().Contain("\"WHITELIST\"");
        json.Should().Contain("\"ACTIVE\"");
    }

    [Fact]
    public void CiStrings_ArePreservedAsPlainStrings()
    {
        var location = TestData.CreateLocation(countryCode: "NL", partyId: "TNM");

        var json = JsonSerializer.Serialize(location, Options);

        json.Should().Contain("\"NL\"");
        json.Should().Contain("\"TNM\"");
    }

    [Fact]
    public void GeoLocation_SerializesAsObject()
    {
        var location = TestData.CreateLocation();

        var json = JsonSerializer.Serialize(location, Options);

        json.Should().Contain("\"coordinates\":{\"latitude\":\"52.364115\",\"longitude\":\"4.891860\"}");
    }
}
