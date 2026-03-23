using System.Text.Json;
using DotOcpi.Models.V2_2_1;
using DotOcpi.Serialization;
using DotOcpi.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Serialization;

public class SerializationRoundTripTests
{
    private static readonly JsonSerializerOptions Options = OcpiJsonOptions.V2_2_1;

    [Fact]
    public void Location_RoundTrips()
    {
        var original = TestData.CreateLocation(evse: TestData.CreateEvse());
        AssertJsonRoundTrip<Location>(original);
    }

    [Fact]
    public void Session_RoundTrips()
    {
        var original = TestData.CreateSession();
        AssertJsonRoundTrip<Session>(original);
    }

    [Fact]
    public void Cdr_RoundTrips()
    {
        var original = TestData.CreateCdr();
        AssertJsonRoundTrip<Cdr>(original);
    }

    [Fact]
    public void Tariff_RoundTrips()
    {
        var original = TestData.CreateTariff();
        AssertJsonRoundTrip<Tariff>(original);
    }

    [Fact]
    public void Token_RoundTrips()
    {
        var original = TestData.CreateToken();
        AssertJsonRoundTrip<Token>(original);
    }

    [Fact]
    public void Credentials_RoundTrips()
    {
        var original = TestData.CreateCredentials();
        AssertJsonRoundTrip<Credentials>(original);
    }

    [Fact]
    public void Connector_RoundTrips()
    {
        var original = TestData.CreateConnector();
        AssertJsonRoundTrip<Connector>(original);
    }

    /// <summary>
    /// Verifies that serialize -> deserialize -> serialize produces identical JSON.
    /// This is a stronger test than object equality because record equality fails on
    /// IReadOnlyList properties (reference vs structural equality).
    /// </summary>
    private static void AssertJsonRoundTrip<T>(T original)
    {
        var json1 = JsonSerializer.Serialize(original, Options);
        var deserialized = JsonSerializer.Deserialize<T>(json1, Options);
        deserialized.Should().NotBeNull();
        var json2 = JsonSerializer.Serialize(deserialized, Options);
        json2.Should().Be(json1);
    }
}
