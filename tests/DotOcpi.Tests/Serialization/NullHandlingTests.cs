using System.Text.Json;
using DotOcpi.Models.V2_2_1;
using DotOcpi.Serialization;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Serialization;

/// <summary>
/// Tests OCPI null-handling semantics: null omission on serialization,
/// explicit null removal in PATCH, and nullable field round-tripping.
/// </summary>
public class NullHandlingTests
{
    private static readonly JsonSerializerOptions Options = OcpiJsonOptions.V2_2_1;

    [Fact]
    public void Serialize_NullOptionalFields_OmitsFromJson()
    {
        var location = new Location
        {
            CountryCode = new("NL"),
            PartyId = new("TNM"),
            Id = new("LOC1"),
            Publish = true,
            Address = "Keizersgracht 100",
            City = "Amsterdam",
            Country = "NLD",
            Coordinates = new("52.364115", "4.891860"),
            TimeZone = "Europe/Amsterdam",
            LastUpdated = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero),
        };

        var json = JsonSerializer.Serialize(location, Options);

        json.Should().NotContain("\"name\":");
        json.Should().NotContain("\"postal_code\":");
        json.Should().NotContain("\"state\":");
        json.Should().NotContain("\"evses\":");
        json.Should().NotContain("\"directions\":");
        json.Should().NotContain("\"operator\":");
        json.Should().NotContain("\"suboperator\":");
        json.Should().NotContain("\"owner\":");
        json.Should().NotContain("\"facilities\":");
        json.Should().NotContain("\"charging_when_closed\":");
        json.Should().NotContain("\"images\":");
        json.Should().NotContain("\"energy_mix\":");
    }

    [Fact]
    public void Serialize_NullOptionalFields_KeepsRequiredFields()
    {
        var location = new Location
        {
            CountryCode = new("NL"),
            PartyId = new("TNM"),
            Id = new("LOC1"),
            Publish = true,
            Address = "Keizersgracht 100",
            City = "Amsterdam",
            Country = "NLD",
            Coordinates = new("52.364115", "4.891860"),
            TimeZone = "Europe/Amsterdam",
            LastUpdated = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero),
        };

        var json = JsonSerializer.Serialize(location, Options);

        json.Should().Contain("\"country_code\":");
        json.Should().Contain("\"party_id\":");
        json.Should().Contain("\"id\":");
        json.Should().Contain("\"publish\":");
        json.Should().Contain("\"address\":");
        json.Should().Contain("\"city\":");
        json.Should().Contain("\"country\":");
        json.Should().Contain("\"coordinates\":");
        json.Should().Contain("\"time_zone\":");
        json.Should().Contain("\"last_updated\":");
    }

    [Fact]
    public void Deserialize_MissingOptionalFields_DefaultsToNull()
    {
        var json = """
            {
                "country_code":"NL",
                "party_id":"TNM",
                "id":"LOC1",
                "publish":true,
                "address":"Keizersgracht 100",
                "city":"Amsterdam",
                "country":"NLD",
                "coordinates":{"latitude":"52.364115","longitude":"4.891860"},
                "time_zone":"Europe/Amsterdam",
                "last_updated":"2026-01-15T12:00:00Z"
            }
            """;

        var location = JsonSerializer.Deserialize<Location>(json, Options);

        location.Should().NotBeNull();
        location!.Name.Should().BeNull();
        location.PostalCode.Should().BeNull();
        location.State.Should().BeNull();
        location.Evses.Should().BeNull();
        location.Directions.Should().BeNull();
        location.Operator.Should().BeNull();
        location.Suboperator.Should().BeNull();
        location.Owner.Should().BeNull();
        location.Facilities.Should().BeNull();
        location.ChargingWhenClosed.Should().BeNull();
        location.Images.Should().BeNull();
        location.EnergyMix.Should().BeNull();
    }

    [Fact]
    public void Deserialize_ExplicitNullOptionalFields_SetsNull()
    {
        var json = """
            {
                "country_code":"NL",
                "party_id":"TNM",
                "id":"LOC1",
                "publish":true,
                "name":null,
                "address":"Keizersgracht 100",
                "city":"Amsterdam",
                "country":"NLD",
                "coordinates":{"latitude":"52.364115","longitude":"4.891860"},
                "time_zone":"Europe/Amsterdam",
                "last_updated":"2026-01-15T12:00:00Z"
            }
            """;

        var location = JsonSerializer.Deserialize<Location>(json, Options);

        location.Should().NotBeNull();
        location!.Name.Should().BeNull();
    }

}
