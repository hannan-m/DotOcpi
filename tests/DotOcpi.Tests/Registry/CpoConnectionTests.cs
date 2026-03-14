using DotOcpi.Registry;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Registry;

public class CpoConnectionTests
{
    private static CpoConnection CreateConnection(
        string cpoCountryCode = "DE",
        string cpoPartyId = "ALL",
        ConnectionStatus status = ConnectionStatus.Connected
    ) =>
        new()
        {
            CpoCountryCode = cpoCountryCode,
            CpoPartyId = cpoPartyId,
            EmspCountryCode = "NL",
            EmspPartyId = "TNM",
            Version = OcpiVersion.V2_2_1,
            ModuleEndpoints = new Dictionary<string, string>
            {
                ["locations"] = "https://cpo.example.com/ocpi/2.2.1/locations",
                ["sessions"] = "https://cpo.example.com/ocpi/2.2.1/sessions",
            },
            TokenBHash = "abc123hash",
            Status = status,
            CreatedAt = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero),
        };

    [Fact]
    public void ConnectionKey_CombinesCountryAndParty()
    {
        var connection = CreateConnection();

        connection.ConnectionKey.Should().Be("DE:ALL");
    }

    [Fact]
    public void ConnectionKey_DifferentParties_AreDifferent()
    {
        var conn1 = CreateConnection(cpoCountryCode: "DE", cpoPartyId: "ALL");
        var conn2 = CreateConnection(cpoCountryCode: "NL", cpoPartyId: "TNM");

        conn1.ConnectionKey.Should().NotBe(conn2.ConnectionKey);
    }

    [Fact]
    public void ConcurrencyVersion_DefaultsToZero()
    {
        var connection = CreateConnection();

        connection.ConcurrencyVersion.Should().Be(0);
    }

    [Fact]
    public void Status_CanBeSetToAnyValue()
    {
        foreach (var status in Enum.GetValues<ConnectionStatus>())
        {
            var connection = CreateConnection(status: status);
            connection.Status.Should().Be(status);
        }
    }

    [Fact]
    public void ModuleEndpoints_AreAccessible()
    {
        var connection = CreateConnection();

        connection.ModuleEndpoints.Should().ContainKey("locations");
        connection.ModuleEndpoints.Should().ContainKey("sessions");
    }

    [Fact]
    public void RecordWith_CreatesModifiedCopy()
    {
        var original = CreateConnection();
        var updated = original with
        {
            Status = ConnectionStatus.Offline,
            UpdatedAt = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
            ConcurrencyVersion = 1,
        };

        updated.Status.Should().Be(ConnectionStatus.Offline);
        updated.ConcurrencyVersion.Should().Be(1);
        original.Status.Should().Be(ConnectionStatus.Connected);
    }
}
