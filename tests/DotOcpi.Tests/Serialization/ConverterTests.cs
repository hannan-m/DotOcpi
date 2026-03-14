using System.Text.Json;
using DotOcpi.Serialization;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Serialization;

public class ConverterTests
{
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new OcpiDateTimeConverter());
        options.Converters.Add(new OcpiNullableDateTimeConverter());
        options.Converters.Add(new CiStringConverter());
        options.Converters.Add(new NullableCiStringConverter());
        options.Converters.Add(new GeoLocationConverter());
        return options;
    }

    public class DateTimeConverterTests
    {
        [Fact]
        public void Serialize_WritesUtcRfc3339()
        {
            var options = CreateOptions();
            var dt = new DateTimeOffset(2026, 3, 14, 15, 30, 0, TimeSpan.Zero);

            var json = JsonSerializer.Serialize(dt, options);

            json.Should().Be("\"2026-03-14T15:30:00Z\"");
        }

        [Fact]
        public void Serialize_ConvertsLocalToUtc()
        {
            var options = CreateOptions();
            var dt = new DateTimeOffset(2026, 3, 14, 17, 30, 0, TimeSpan.FromHours(2));

            var json = JsonSerializer.Serialize(dt, options);

            json.Should().Be("\"2026-03-14T15:30:00Z\"");
        }

        [Fact]
        public void Deserialize_ParsesUtcFormat()
        {
            var options = CreateOptions();
            var result = JsonSerializer.Deserialize<DateTimeOffset>("\"2026-03-14T15:30:00Z\"", options);

            result.Should().Be(new DateTimeOffset(2026, 3, 14, 15, 30, 0, TimeSpan.Zero));
        }

        [Fact]
        public void Deserialize_ParsesWithOffset()
        {
            var options = CreateOptions();
            var result = JsonSerializer.Deserialize<DateTimeOffset>("\"2026-03-14T17:30:00+02:00\"", options);

            result.Offset.Should().Be(TimeSpan.Zero);
            result.Should().Be(new DateTimeOffset(2026, 3, 14, 15, 30, 0, TimeSpan.Zero));
        }

        [Fact]
        public void Deserialize_InvalidFormat_ThrowsJsonException()
        {
            var options = CreateOptions();
            var act = () => JsonSerializer.Deserialize<DateTimeOffset>("\"not-a-date\"", options);

            act.Should().Throw<JsonException>();
        }

        [Fact]
        public void NullableDateTime_RoundTrips()
        {
            var options = CreateOptions();
            DateTimeOffset? value = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

            var json = JsonSerializer.Serialize(value, options);
            var result = JsonSerializer.Deserialize<DateTimeOffset?>(json, options);

            result.Should().Be(value);
        }

        [Fact]
        public void NullableDateTime_Null_SerializesAsNull()
        {
            var options = CreateOptions();
            DateTimeOffset? value = null;

            var json = JsonSerializer.Serialize(value, options);

            json.Should().Be("null");
        }
    }

    public class CiStringConverterTests
    {
        [Fact]
        public void Serialize_WritesPlainString()
        {
            var options = CreateOptions();
            CiString value = "Hello";

            var json = JsonSerializer.Serialize(value, options);

            json.Should().Be("\"Hello\"");
        }

        [Fact]
        public void Deserialize_CreatesCiString()
        {
            var options = CreateOptions();
            var result = JsonSerializer.Deserialize<CiString>("\"Test\"", options);

            ((string)result).Should().Be("Test");
        }

        [Fact]
        public void RoundTrip_PreservesCase()
        {
            var options = CreateOptions();
            CiString original = "MiXeD-CaSe";

            var json = JsonSerializer.Serialize(original, options);
            var result = JsonSerializer.Deserialize<CiString>(json, options);

            ((string)result).Should().Be("MiXeD-CaSe");
        }

        [Fact]
        public void NullableCiString_Null_SerializesAsNull()
        {
            var options = CreateOptions();
            CiString? value = null;

            var json = JsonSerializer.Serialize(value, options);

            json.Should().Be("null");
        }

        [Fact]
        public void NullableCiString_RoundTrips()
        {
            var options = CreateOptions();
            CiString? value = new CiString("TEST");

            var json = JsonSerializer.Serialize(value, options);
            var result = JsonSerializer.Deserialize<CiString?>(json, options);

            result.Should().NotBeNull();
            ((string)result!.Value).Should().Be("TEST");
        }
    }

    public class GeoLocationConverterTests
    {
        [Fact]
        public void Serialize_WritesLatLongObject()
        {
            var options = CreateOptions();
            var geo = new GeoLocation("52.364115", "4.891860");

            var json = JsonSerializer.Serialize(geo, options);

            json.Should().Contain("\"latitude\":\"52.364115\"");
            json.Should().Contain("\"longitude\":\"4.891860\"");
        }

        [Fact]
        public void Deserialize_ParsesLatLongObject()
        {
            var options = CreateOptions();
            var json = """{"latitude":"50.770774","longitude":"-126.104965"}""";

            var result = JsonSerializer.Deserialize<GeoLocation>(json, options);

            result.Latitude.Should().Be("50.770774");
            result.Longitude.Should().Be("-126.104965");
        }

        [Fact]
        public void RoundTrip_PreservesPrecision()
        {
            var options = CreateOptions();
            var original = new GeoLocation("52.3641150000", "4.8918600000");

            var json = JsonSerializer.Serialize(original, options);
            var result = JsonSerializer.Deserialize<GeoLocation>(json, options);

            result.Should().Be(original);
        }

        [Fact]
        public void Deserialize_MissingLatitude_ThrowsJsonException()
        {
            var options = CreateOptions();
            var json = """{"longitude":"4.891860"}""";

            var act = () => JsonSerializer.Deserialize<GeoLocation>(json, options);

            act.Should().Throw<JsonException>();
        }
    }
}
