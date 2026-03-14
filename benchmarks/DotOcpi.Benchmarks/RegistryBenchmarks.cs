using BenchmarkDotNet.Attributes;
using DotOcpi.Registry;

namespace DotOcpi.Benchmarks;

[MemoryDiagnoser]
public class RegistryBenchmarks
{
    private InMemoryCpoRegistry _registry = null!;
    private string _targetKey = null!;
    private string _targetTokenHash = null!;

    [GlobalSetup]
    public void Setup()
    {
        _registry = new InMemoryCpoRegistry();

        for (var i = 0; i < 50; i++)
        {
            var conn = new CpoConnection
            {
                CpoCountryCode = "XX",
                CpoPartyId = $"P{i:D3}",
                EmspCountryCode = "NL",
                EmspPartyId = "MSP",
                Version = OcpiVersion.V2_2_1,
                ModuleEndpoints = new Dictionary<string, string>
                {
                    ["locations"] = $"https://cpo{i}.example.com/ocpi/locations",
                },
                TokenBHash = $"hash_{i:D3}",
                Status = ConnectionStatus.Connected,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            _registry.AddOrUpdate(conn);
        }

        _targetKey = "XX:P025";
        _targetTokenHash = "hash_025";
    }

    [Benchmark]
    public CpoConnection? FindByConnectionKey()
    {
        return _registry.FindByConnectionKey(_targetKey);
    }

    [Benchmark]
    public CpoConnection? FindByTokenHash()
    {
        return _registry.FindByTokenHash(_targetTokenHash);
    }

    [Benchmark]
    public IReadOnlyList<CpoConnection> GetAll()
    {
        return _registry.GetAll();
    }
}
