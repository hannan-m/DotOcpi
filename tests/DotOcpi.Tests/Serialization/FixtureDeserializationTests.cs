using System.Text.Json;
using DotOcpi.Models.V2_2_1;
using DotOcpi.Serialization;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Serialization;

/// <summary>
/// Deserializes OCPI spec-style JSON fixtures and verifies the resulting
/// models have correct field values. Validates that the serialization layer
/// handles real-world OCPI payloads, not just round-trip equality.
/// </summary>
public class FixtureDeserializationTests
{
    private static readonly JsonSerializerOptions Options = OcpiJsonOptions.V2_2_1;

    private static string LoadFixture(string name) => File.ReadAllText(Path.Combine("Fixtures", "Json", name));

    [Fact]
    public void Location_Fixture_DeserializesCorrectly()
    {
        var json = LoadFixture("location-v2_2_1.json");

        var location = JsonSerializer.Deserialize<Location>(json, Options);

        location.Should().NotBeNull();
        ((string)location!.CountryCode).Should().Be("BE");
        ((string)location.PartyId).Should().Be("BEC");
        ((string)location.Id).Should().Be("LOC1");
        location.Publish.Should().BeTrue();
        location.Name.Should().Be("Gent Zuid");
        location.Address.Should().Be("F.Rooseveltlaan 3A");
        location.City.Should().Be("Gent");
        location.PostalCode.Should().Be("9000");
        location.Country.Should().Be("BEL");
        location.Coordinates.Latitude.Should().Be("51.047599");
        location.Coordinates.Longitude.Should().Be("3.729944");
        location.ParkingType.Should().Be(ParkingType.ON_STREET);
        location.TimeZone.Should().Be("Europe/Brussels");
    }

    [Fact]
    public void Location_Fixture_DeserializesEvses()
    {
        var json = LoadFixture("location-v2_2_1.json");

        var location = JsonSerializer.Deserialize<Location>(json, Options);

        location!.Evses.Should().NotBeNull();
        var evses = location.Evses!;
        evses.Should().HaveCount(2);

        var evse1 = evses[0];
        ((string)evse1.Uid).Should().Be("3256");
        ((string)evse1.EvseId!.Value).Should().Be("BE*BEC*E041503001");
        evse1.Status.Should().Be(Status.AVAILABLE);
        evse1.Capabilities.Should().Contain(Capability.RESERVABLE);
        evse1.Connectors.Should().HaveCount(2);
        evse1.FloorLevel.Should().Be("-1");
        evse1.PhysicalReference.Should().Be("1");

        var evse2 = evses[1];
        ((string)evse2.Uid).Should().Be("3257");
        evse2.Status.Should().Be(Status.RESERVED);
        evse2.Connectors.Should().HaveCount(1);
    }

    [Fact]
    public void Location_Fixture_DeserializesConnectors()
    {
        var json = LoadFixture("location-v2_2_1.json");

        var location = JsonSerializer.Deserialize<Location>(json, Options);

        var connector = location!.Evses![0].Connectors[0];
        ((string)connector.Id).Should().Be("1");
        connector.Standard.Should().Be(ConnectorType.IEC_62196_T2);
        connector.Format.Should().Be(ConnectorFormat.CABLE);
        connector.PowerType.Should().Be(PowerType.AC_3_PHASE);
        connector.MaxVoltage.Should().Be(220);
        connector.MaxAmperage.Should().Be(16);
        connector.TariffIds.Should().Contain(new CiString("11"));
    }

    [Fact]
    public void Location_Fixture_RoundTrips()
    {
        var json = LoadFixture("location-v2_2_1.json");

        var location = JsonSerializer.Deserialize<Location>(json, Options);
        var json1 = JsonSerializer.Serialize(location, Options);
        var location2 = JsonSerializer.Deserialize<Location>(json1, Options);
        var json2 = JsonSerializer.Serialize(location2, Options);

        json2.Should().Be(json1);
    }

    [Fact]
    public void Session_Fixture_DeserializesCorrectly()
    {
        var json = LoadFixture("session-v2_2_1.json");

        var session = JsonSerializer.Deserialize<Session>(json, Options);

        session.Should().NotBeNull();
        ((string)session!.CountryCode).Should().Be("BE");
        ((string)session.PartyId).Should().Be("BEC");
        ((string)session.Id).Should().Be("101");
        session.StartDateTime.Should().Be(new DateTimeOffset(2015, 6, 29, 21, 39, 9, TimeSpan.Zero));
        session.Kwh.Should().Be(0.0m);
        session.AuthMethod.Should().Be(AuthMethod.WHITELIST);
        ((string)session.LocationId).Should().Be("LOC1");
        ((string)session.EvseUid).Should().Be("3256");
        ((string)session.ConnectorId).Should().Be("1");
        session.Currency.Should().Be("EUR");
        session.Status.Should().Be(SessionStatus.ACTIVE);
    }

    [Fact]
    public void Session_Fixture_DeserializesCdrToken()
    {
        var json = LoadFixture("session-v2_2_1.json");

        var session = JsonSerializer.Deserialize<Session>(json, Options);

        var token = session!.CdrToken;
        ((string)token.CountryCode).Should().Be("NL");
        ((string)token.PartyId).Should().Be("TNM");
        ((string)token.Uid).Should().Be("012345678");
        token.Type.Should().Be(TokenType.RFID);
        ((string)token.ContractId).Should().Be("NL-TNM-000660-8");
    }

    [Fact]
    public void Session_Fixture_DeserializesTotalCost()
    {
        var json = LoadFixture("session-v2_2_1.json");

        var session = JsonSerializer.Deserialize<Session>(json, Options);

        session!.TotalCost.Should().NotBeNull();
        session.TotalCost!.ExclVat.Should().Be(2.50m);
        session.TotalCost.InclVat.Should().Be(3.00m);
    }

    [Fact]
    public void Session_Fixture_RoundTrips()
    {
        var json = LoadFixture("session-v2_2_1.json");

        var session = JsonSerializer.Deserialize<Session>(json, Options);
        var json1 = JsonSerializer.Serialize(session, Options);
        var session2 = JsonSerializer.Deserialize<Session>(json1, Options);
        var json2 = JsonSerializer.Serialize(session2, Options);

        json2.Should().Be(json1);
    }

    [Fact]
    public void Tariff_Fixture_DeserializesCorrectly()
    {
        var json = LoadFixture("tariff-v2_2_1.json");

        var tariff = JsonSerializer.Deserialize<Tariff>(json, Options);

        tariff.Should().NotBeNull();
        ((string)tariff!.CountryCode).Should().Be("DE");
        ((string)tariff.PartyId).Should().Be("ALL");
        ((string)tariff.Id).Should().Be("12");
        tariff.Currency.Should().Be("EUR");
        tariff.Elements.Should().HaveCount(1);

        var component = tariff.Elements[0].PriceComponents[0];
        component.Type.Should().Be(TariffDimensionType.ENERGY);
        component.Price.Should().Be(0.20m);
        component.Vat.Should().Be(10.0m);
        component.StepSize.Should().Be(1);
    }

    [Fact]
    public void Tariff_Fixture_RoundTrips()
    {
        var json = LoadFixture("tariff-v2_2_1.json");

        var tariff = JsonSerializer.Deserialize<Tariff>(json, Options);
        var json1 = JsonSerializer.Serialize(tariff, Options);
        var tariff2 = JsonSerializer.Deserialize<Tariff>(json1, Options);
        var json2 = JsonSerializer.Serialize(tariff2, Options);

        json2.Should().Be(json1);
    }

    [Fact]
    public void Token_Fixture_DeserializesCorrectly()
    {
        var json = LoadFixture("token-v2_2_1.json");

        var token = JsonSerializer.Deserialize<Token>(json, Options);

        token.Should().NotBeNull();
        ((string)token!.CountryCode).Should().Be("NL");
        ((string)token.PartyId).Should().Be("TNM");
        ((string)token.Uid).Should().Be("012345678");
        token.Type.Should().Be(TokenType.RFID);
        ((string)token.ContractId).Should().Be("NL-TNM-000660-8");
        token.Issuer.Should().Be("TheNewMotion");
        token.Valid.Should().BeTrue();
        token.Whitelist.Should().Be(WhitelistType.ALWAYS);
    }

    [Fact]
    public void Token_Fixture_RoundTrips()
    {
        var json = LoadFixture("token-v2_2_1.json");

        var token = JsonSerializer.Deserialize<Token>(json, Options);
        var json1 = JsonSerializer.Serialize(token, Options);
        var token2 = JsonSerializer.Deserialize<Token>(json1, Options);
        var json2 = JsonSerializer.Serialize(token2, Options);

        json2.Should().Be(json1);
    }

    [Fact]
    public void Connector_Fixture_DeserializesCorrectly()
    {
        var json = LoadFixture("connector-v2_2_1.json");

        var connector = JsonSerializer.Deserialize<Connector>(json, Options);

        connector.Should().NotBeNull();
        ((string)connector!.Id).Should().Be("1");
        connector.Standard.Should().Be(ConnectorType.IEC_62196_T2);
        connector.Format.Should().Be(ConnectorFormat.SOCKET);
        connector.PowerType.Should().Be(PowerType.AC_3_PHASE);
        connector.MaxVoltage.Should().Be(220);
        connector.MaxAmperage.Should().Be(16);
        connector.TariffIds.Should().NotBeNull();
        connector.TariffIds!.Should().ContainSingle().Which.Should().Be(new CiString("11"));
    }

    [Fact]
    public void Connector_Fixture_RoundTrips()
    {
        var json = LoadFixture("connector-v2_2_1.json");

        var connector = JsonSerializer.Deserialize<Connector>(json, Options);
        var json1 = JsonSerializer.Serialize(connector, Options);
        var connector2 = JsonSerializer.Deserialize<Connector>(json1, Options);
        var json2 = JsonSerializer.Serialize(connector2, Options);

        json2.Should().Be(json1);
    }
}
