using System.Collections.Concurrent;

namespace DotOcpi.Security;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="ITokenStore"/>.
/// Suitable for development and testing. Production deployments should
/// use a persistent implementation (database, KeyVault, etc.).
/// </summary>
public sealed class InMemoryTokenStore : ITokenStore
{
    private readonly ConcurrentDictionary<string, TokenEntry> _tokens = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _cpoTokens = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public ValueTask StoreAsync(
        string tokenHash,
        TokenPurpose purpose,
        string partyId,
        CancellationToken cancellationToken = default
    )
    {
        var entry = new TokenEntry(tokenHash, purpose, partyId);
        _tokens.AddOrUpdate(tokenHash, entry, (_, _) => entry);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<TokenEntry?> FindAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        _tokens.TryGetValue(tokenHash, out var entry);
        return ValueTask.FromResult(entry);
    }

    /// <inheritdoc />
    public ValueTask<bool> RemoveAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var removed = _tokens.TryRemove(tokenHash, out _);
        return ValueTask.FromResult(removed);
    }

    /// <inheritdoc />
    public ValueTask StoreCpoTokenAsync(
        string cpoId,
        string protectedToken,
        CancellationToken cancellationToken = default
    )
    {
        _cpoTokens[cpoId] = protectedToken;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<string?> GetCpoTokenAsync(string cpoId, CancellationToken cancellationToken = default)
    {
        _cpoTokens.TryGetValue(cpoId, out var token);
        return ValueTask.FromResult(token);
    }

    /// <inheritdoc />
    public ValueTask<bool> RemoveCpoTokenAsync(string cpoId, CancellationToken cancellationToken = default)
    {
        var removed = _cpoTokens.TryRemove(cpoId, out _);
        return ValueTask.FromResult(removed);
    }
}
