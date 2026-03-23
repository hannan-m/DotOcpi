using System.Collections.Concurrent;
using DotOcpi.Client;

namespace DotOcpi.Sample.Services;

/// <summary>
/// Stores raw CPO tokens received during registration.
/// In production, use a secrets manager (Azure KeyVault, HashiCorp Vault, etc.).
/// </summary>
public sealed class SampleTokenProvider : IOutboundTokenProvider
{
    private readonly ConcurrentDictionary<string, string> _tokens = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Stores a CPO token for outbound requests.
    /// </summary>
    public void StoreToken(string cpoId, string rawToken)
    {
        _tokens[cpoId] = rawToken;
    }

    public ValueTask<string> GetTokenAsync(string cpoId, CancellationToken cancellationToken = default)
    {
        if (_tokens.TryGetValue(cpoId, out var token))
        {
            return new ValueTask<string>(token);
        }

        throw new InvalidOperationException($"No token stored for CPO '{cpoId}'. Register first.");
    }
}
