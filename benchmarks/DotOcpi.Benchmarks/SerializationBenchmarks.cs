using System.Text.Json;
using BenchmarkDotNet.Attributes;
using DotOcpi.Models.V2_2_1;
using DotOcpi.Serialization;

namespace DotOcpi.Benchmarks;

[MemoryDiagnoser]
public class SerializationBenchmarks
{
    private byte[] _locationJson = null!;
    private Location _location = null!;

    [GlobalSetup]
    public void Setup()
    {
        _location = new Location
        {
            CountryCode = new CiString("DE"),
            PartyId = new CiString("CPO"),
            Id = new CiString("LOC001"),
            Publish = true,
            Name = "Downtown Charging Station",
            Address = "123 Main Street",
            City = "Berlin",
            Country = "DEU",
            Coordinates = new GeoLocation("52.5200", "13.4050"),
            TimeZone = "Europe/Berlin",
            Evses =
            [
                new Evse
                {
                    Uid = new CiString("EVSE001"),
                    EvseId = new CiString("DE*CPO*E001"),
                    Status = Status.AVAILABLE,
                    Connectors =
                    [
                        new Connector
                        {
                            Id = new CiString("1"),
                            Standard = ConnectorType.IEC_62196_T2,
                            Format = ConnectorFormat.SOCKET,
                            PowerType = PowerType.AC_3_PHASE,
                            MaxVoltage = 400,
                            MaxAmperage = 32,
                            LastUpdated = DateTimeOffset.UtcNow,
                        },
                    ],
                    LastUpdated = DateTimeOffset.UtcNow,
                },
            ],
            LastUpdated = DateTimeOffset.UtcNow,
        };

        _locationJson = JsonSerializer.SerializeToUtf8Bytes(_location, OcpiJsonOptions.GetOptions(OcpiVersion.V2_2_1));
    }

    [Benchmark]
    public byte[] Serialize()
    {
        return JsonSerializer.SerializeToUtf8Bytes(_location, OcpiJsonOptions.GetOptions(OcpiVersion.V2_2_1));
    }

    [Benchmark]
    public Location? Deserialize()
    {
        return JsonSerializer.Deserialize<Location>(_locationJson, OcpiJsonOptions.GetOptions(OcpiVersion.V2_2_1));
    }
}
