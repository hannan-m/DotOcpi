using System.Text.Json;
using System.Text.Json.Serialization;
using DotOcpi.Models.V2_2_1;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Serialization;

public class EnumSerializationTests
{
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    [Theory]
    [InlineData(ConnectorType.IEC_62196_T2, "\"IEC_62196_T2\"")]
    [InlineData(ConnectorType.CHADEMO, "\"CHADEMO\"")]
    [InlineData(ConnectorType.IEC_62196_T2_COMBO, "\"IEC_62196_T2_COMBO\"")]
    public void ConnectorType_SerializesAsOcpiString(ConnectorType value, string expected)
    {
        var options = CreateOptions();
        JsonSerializer.Serialize(value, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("\"AC_3_PHASE\"", PowerType.AC_3_PHASE)]
    [InlineData("\"DC\"", PowerType.DC)]
    [InlineData("\"AC_1_PHASE\"", PowerType.AC_1_PHASE)]
    public void PowerType_DeserializesFromOcpiString(string json, PowerType expected)
    {
        var options = CreateOptions();
        JsonSerializer.Deserialize<PowerType>(json, options).Should().Be(expected);
    }

    [Fact]
    public void Status_RoundTrips()
    {
        var options = CreateOptions();
        foreach (var status in Enum.GetValues<Status>())
        {
            var json = JsonSerializer.Serialize(status, options);
            var result = JsonSerializer.Deserialize<Status>(json, options);
            result.Should().Be(status);
        }
    }

    [Fact]
    public void SessionStatus_RoundTrips()
    {
        var options = CreateOptions();
        foreach (var status in Enum.GetValues<SessionStatus>())
        {
            var json = JsonSerializer.Serialize(status, options);
            var result = JsonSerializer.Deserialize<SessionStatus>(json, options);
            result.Should().Be(status);
        }
    }

    [Fact]
    public void TokenType_Serializes()
    {
        var options = CreateOptions();
        JsonSerializer.Serialize(TokenType.RFID, options).Should().Be("\"RFID\"");
        JsonSerializer.Serialize(TokenType.AD_HOC_USER, options).Should().Be("\"AD_HOC_USER\"");
        JsonSerializer.Serialize(TokenType.EMAID, options).Should().Be("\"EMAID\"");
    }

    [Fact]
    public void CommandResultType_Serializes()
    {
        var options = CreateOptions();
        JsonSerializer.Serialize(CommandResultType.EVSE_OCCUPIED, options).Should().Be("\"EVSE_OCCUPIED\"");
        JsonSerializer
            .Serialize(CommandResultType.CANCELED_RESERVATION, options)
            .Should()
            .Be("\"CANCELED_RESERVATION\"");
    }

    [Fact]
    public void ModuleId_SerializesAsLowercase()
    {
        var options = CreateOptions();
        JsonSerializer.Serialize(ModuleId.locations, options).Should().Be("\"locations\"");
        JsonSerializer.Serialize(ModuleId.chargingprofiles, options).Should().Be("\"chargingprofiles\"");
    }

    [Fact]
    public void ChargingRateUnit_Serializes()
    {
        var options = CreateOptions();
        JsonSerializer.Serialize(ChargingRateUnit.W, options).Should().Be("\"W\"");
        JsonSerializer.Serialize(ChargingRateUnit.A, options).Should().Be("\"A\"");
    }

    [Fact]
    public void InvalidEnumValue_ThrowsJsonException()
    {
        var options = CreateOptions();
        var act = () => JsonSerializer.Deserialize<Status>("\"INVALID_STATUS\"", options);

        act.Should().Throw<JsonException>();
    }
}
