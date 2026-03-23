using System.Collections.Concurrent;

namespace DotOcpi.Registry;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="ICpoRegistry"/> using
/// three ConcurrentDictionary indexes for O(1) lookup by connection key,
/// token hash, and eMSP identity. Uses optimistic concurrency via
/// <see cref="CpoConnection.ConcurrencyVersion"/>.
/// </summary>
public sealed class InMemoryCpoRegistry : ICpoRegistry
{
    private readonly ConcurrentDictionary<string, CpoConnection> _byConnectionKey = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly ConcurrentDictionary<string, CpoConnection> _byTokenHash = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, CpoConnection>> _byEmspIdentity = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly object _writeLock = new();

    /// <inheritdoc />
    public CpoConnection? FindByConnectionKey(string connectionKey)
    {
        _byConnectionKey.TryGetValue(connectionKey, out var connection);
        return connection;
    }

    /// <inheritdoc />
    public CpoConnection? FindByTokenHash(string tokenBHash)
    {
        _byTokenHash.TryGetValue(tokenBHash, out var connection);
        return connection;
    }

    /// <inheritdoc />
    public IReadOnlyList<CpoConnection> FindByEmspIdentity(string emspCountryCode, string emspPartyId)
    {
        var key = EmspKey(emspCountryCode, emspPartyId);
        if (_byEmspIdentity.TryGetValue(key, out var connections))
        {
            return connections.Values.ToArray();
        }

        return [];
    }

    /// <inheritdoc />
    public IReadOnlyList<CpoConnection> GetAll() => _byConnectionKey.Values.ToArray();

    /// <inheritdoc />
    public bool AddOrUpdate(CpoConnection connection)
    {
        lock (_writeLock)
        {
            var connectionKey = connection.ConnectionKey;

            if (_byConnectionKey.TryGetValue(connectionKey, out var existing))
            {
                // Optimistic concurrency: reject if version doesn't match
                if (connection.ConcurrencyVersion != existing.ConcurrencyVersion)
                {
                    return false;
                }

                // Remove old indexes before updating
                _byTokenHash.TryRemove(existing.TokenBHash, out _);
                RemoveFromEmspIndex(existing);
            }

            var updated = connection with { ConcurrencyVersion = connection.ConcurrencyVersion + 1 };

            _byConnectionKey[connectionKey] = updated;
            _byTokenHash[updated.TokenBHash] = updated;
            AddToEmspIndex(updated);

            return true;
        }
    }

    /// <inheritdoc />
    public bool Remove(string connectionKey)
    {
        lock (_writeLock)
        {
            if (!_byConnectionKey.TryRemove(connectionKey, out var removed))
            {
                return false;
            }

            _byTokenHash.TryRemove(removed.TokenBHash, out _);
            RemoveFromEmspIndex(removed);
            return true;
        }
    }

    private void AddToEmspIndex(CpoConnection connection)
    {
        var emspKey = EmspKey(connection.EmspCountryCode, connection.EmspPartyId);
        var connections = _byEmspIdentity.GetOrAdd(
            emspKey,
            _ => new ConcurrentDictionary<string, CpoConnection>(StringComparer.OrdinalIgnoreCase)
        );
        connections[connection.ConnectionKey] = connection;
    }

    private void RemoveFromEmspIndex(CpoConnection connection)
    {
        var emspKey = EmspKey(connection.EmspCountryCode, connection.EmspPartyId);
        if (_byEmspIdentity.TryGetValue(emspKey, out var connections))
        {
            connections.TryRemove(connection.ConnectionKey, out _);
        }
    }

    private static string EmspKey(string countryCode, string partyId) => $"{countryCode}:{partyId}";
}
