using BenchmarkDotNet.Attributes;
using DotOcpi.Security;

namespace DotOcpi.Benchmarks;

[MemoryDiagnoser]
public class TokenBenchmarks
{
    private string _token = null!;
    private string _tokenHash = null!;
    private InMemoryTokenStore _store = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _token = TokenGenerator.Generate();
        _tokenHash = TokenHasher.Hash(_token);

        _store = new InMemoryTokenStore();
        await _store.StoreAsync(_tokenHash, TokenPurpose.TokenB, "DE:CPO");

        // Pre-populate with additional tokens for realistic lookup
        for (var i = 0; i < 100; i++)
        {
            var t = TokenGenerator.Generate();
            await _store.StoreAsync(TokenHasher.Hash(t), TokenPurpose.TokenB, $"XX:P{i:D3}");
        }
    }

    [Benchmark]
    public string GenerateToken()
    {
        return TokenGenerator.Generate();
    }

    [Benchmark]
    public string HashToken()
    {
        return TokenHasher.Hash(_token);
    }

    [Benchmark]
    public ValueTask<TokenEntry?> LookupToken()
    {
        return _store.FindAsync(_tokenHash);
    }
}
