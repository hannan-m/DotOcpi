using DotOcpi.Models.V2_2_1;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Models.V2_2_1;

public class LocationTests
{
    [Fact]
    public void Location_RequiredProperties_SetCorrectly()
    {
        var location = new Location
        {
            CountryCode = "NL",
            PartyId = "TNM",
            Id = "LOC1",
            Publish = true,
            Address = "Keizersgracht 585",
            City = "Amsterdam",
            Country = "NLD",
            Coordinates = new GeoLocation("52.364115", "4.891860"),
            TimeZone = "Europe/Amsterdam",
            LastUpdated = DateTimeOffset.UtcNow,
        };

        ((string)location.CountryCode).Should().Be("NL");
        ((string)location.PartyId).Should().Be("TNM");
        ((string)location.Id).Should().Be("LOC1");
        location.Publish.Should().BeTrue();
        location.Address.Should().Be("Keizersgracht 585");
        location.City.Should().Be("Amsterdam");
        location.Country.Should().Be("NLD");
        location.TimeZone.Should().Be("Europe/Amsterdam");
    }

    [Fact]
    public void Location_OptionalProperties_DefaultToNull()
    {
        var location = new Location
        {
            CountryCode = "NL",
            PartyId = "TNM",
            Id = "LOC1",
            Publish = true,
            Address = "Keizersgracht 585",
            City = "Amsterdam",
            Country = "NLD",
            Coordinates = new GeoLocation("52.364115", "4.891860"),
            TimeZone = "Europe/Amsterdam",
            LastUpdated = DateTimeOffset.UtcNow,
        };

        location.Name.Should().BeNull();
        location.PostalCode.Should().BeNull();
        location.State.Should().BeNull();
        location.Evses.Should().BeNull();
        location.ParkingType.Should().BeNull();
        location.Operator.Should().BeNull();
        location.EnergyMix.Should().BeNull();
    }

    [Fact]
    public void Location_WithEvses_ContainsConnectors()
    {
        var connector = new Connector
        {
            Id = "1",
            Standard = ConnectorType.IEC_62196_T2,
            Format = ConnectorFormat.SOCKET,
            PowerType = PowerType.AC_3_PHASE,
            MaxVoltage = 230,
            MaxAmperage = 32,
            LastUpdated = DateTimeOffset.UtcNow,
        };

        var evse = new Evse
        {
            Uid = "3256",
            EvseId = "NL*TNM*E01",
            Status = Status.AVAILABLE,
            Connectors = [connector],
            LastUpdated = DateTimeOffset.UtcNow,
        };

        var location = new Location
        {
            CountryCode = "NL",
            PartyId = "TNM",
            Id = "LOC1",
            Publish = true,
            Address = "Keizersgracht 585",
            City = "Amsterdam",
            Country = "NLD",
            Coordinates = new GeoLocation("52.364115", "4.891860"),
            TimeZone = "Europe/Amsterdam",
            Evses = [evse],
            LastUpdated = DateTimeOffset.UtcNow,
        };

        location.Evses.Should().HaveCount(1);
        location.Evses![0].Connectors.Should().HaveCount(1);
        location.Evses[0].Connectors[0].Standard.Should().Be(ConnectorType.IEC_62196_T2);
    }

    [Fact]
    public void Evse_Capabilities_CanContainMultipleValues()
    {
        var evse = new Evse
        {
            Uid = "3256",
            Status = Status.AVAILABLE,
            Capabilities = [Capability.REMOTE_START_STOP_CAPABLE, Capability.RFID_READER, Capability.RESERVABLE],
            Connectors =
            [
                new Connector
                {
                    Id = "1",
                    Standard = ConnectorType.IEC_62196_T2,
                    Format = ConnectorFormat.CABLE,
                    PowerType = PowerType.AC_1_PHASE,
                    MaxVoltage = 230,
                    MaxAmperage = 16,
                    LastUpdated = DateTimeOffset.UtcNow,
                },
            ],
            LastUpdated = DateTimeOffset.UtcNow,
        };

        evse.Capabilities.Should().HaveCount(3);
        evse.Capabilities.Should().Contain(Capability.RESERVABLE);
    }

    [Fact]
    public void Connector_TariffIds_SupportsMultipleTariffs()
    {
        var connector = new Connector
        {
            Id = "1",
            Standard = ConnectorType.IEC_62196_T2_COMBO,
            Format = ConnectorFormat.CABLE,
            PowerType = PowerType.DC,
            MaxVoltage = 400,
            MaxAmperage = 125,
            MaxElectricPower = 50000,
            TariffIds = [(CiString)"TARIFF-1", (CiString)"TARIFF-2"],
            LastUpdated = DateTimeOffset.UtcNow,
        };

        connector.TariffIds.Should().HaveCount(2);
        connector.MaxElectricPower.Should().Be(50000);
    }

    [Fact]
    public void Location_PublishAllowedTo_RestrictsVisibility()
    {
        var location = new Location
        {
            CountryCode = "NL",
            PartyId = "TNM",
            Id = "LOC-PRIVATE",
            Publish = false,
            PublishAllowedTo =
            [
                new PublishTokenType { Uid = "TOKEN-001", Type = TokenType.RFID },
                new PublishTokenType { GroupId = "FLEET-A" },
            ],
            Address = "Private Road 1",
            City = "Rotterdam",
            Country = "NLD",
            Coordinates = new GeoLocation("51.9225", "4.47917"),
            TimeZone = "Europe/Amsterdam",
            LastUpdated = DateTimeOffset.UtcNow,
        };

        location.Publish.Should().BeFalse();
        location.PublishAllowedTo.Should().HaveCount(2);
    }

    [Fact]
    public void Location_RecordEquality_WorksCorrectly()
    {
        var now = DateTimeOffset.UtcNow;

        var loc1 = new Location
        {
            CountryCode = "NL",
            PartyId = "TNM",
            Id = "LOC1",
            Publish = true,
            Address = "Addr",
            City = "City",
            Country = "NLD",
            Coordinates = new GeoLocation("52.0", "4.0"),
            TimeZone = "Europe/Amsterdam",
            LastUpdated = now,
        };

        var loc2 = new Location
        {
            CountryCode = "NL",
            PartyId = "TNM",
            Id = "LOC1",
            Publish = true,
            Address = "Addr",
            City = "City",
            Country = "NLD",
            Coordinates = new GeoLocation("52.0", "4.0"),
            TimeZone = "Europe/Amsterdam",
            LastUpdated = now,
        };

        loc1.Should().Be(loc2);
    }
}
