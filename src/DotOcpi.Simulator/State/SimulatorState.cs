using System.Collections.Concurrent;
using System.Text.Json;
using DotOcpi.Simulator.Models;

namespace DotOcpi.Simulator.State;

/// <summary>
/// Central thread-safe state container for all mutable simulator data.
/// All state access goes through this class to ensure consistency.
/// </summary>
internal sealed class SimulatorState : IDisposable
{
    private readonly ReaderWriterLockSlim _dataLock = new();

    public ConcurrentDictionary<string, ConnectionState> Connections { get; } = new();
    public ConcurrentDictionary<string, EvseState> Evses { get; } = new();
    public ConcurrentDictionary<string, SessionState> Sessions { get; } = new();
    public ConcurrentDictionary<string, ReservationState> Reservations { get; } = new();
    public ConcurrentDictionary<string, JsonElement> ReceivedTokens { get; } = new();
    public ConcurrentDictionary<string, object> ChargingProfiles { get; } = new();
    public ConcurrentQueue<RecordedRequest> RequestHistory { get; } = new();

    private readonly List<LocationSpec> _locations = [];
    private readonly List<TariffSpec> _tariffs = [];

    public const int MaxRequestHistory = 10_000;

    // ── Location data ─────────────────────────────────────────

    public IReadOnlyList<LocationSpec> GetLocations()
    {
        _dataLock.EnterReadLock();
        try
        {
            return [.. _locations];
        }
        finally
        {
            _dataLock.ExitReadLock();
        }
    }

    public void SetLocations(IEnumerable<LocationSpec> locations)
    {
        _dataLock.EnterWriteLock();
        try
        {
            _locations.Clear();
            _locations.AddRange(locations);
        }
        finally
        {
            _dataLock.ExitWriteLock();
        }
    }

    public LocationSpec? FindLocation(string locationId)
    {
        _dataLock.EnterReadLock();
        try
        {
            return _locations.Find(l => l.Id == locationId);
        }
        finally
        {
            _dataLock.ExitReadLock();
        }
    }

    // ── Tariff data ───────────────────────────────────────────

    public IReadOnlyList<TariffSpec> GetTariffs()
    {
        _dataLock.EnterReadLock();
        try
        {
            return [.. _tariffs];
        }
        finally
        {
            _dataLock.ExitReadLock();
        }
    }

    public void SetTariffs(IEnumerable<TariffSpec> tariffs)
    {
        _dataLock.EnterWriteLock();
        try
        {
            _tariffs.Clear();
            _tariffs.AddRange(tariffs);
        }
        finally
        {
            _dataLock.ExitWriteLock();
        }
    }

    public TariffSpec? FindTariff(string tariffId)
    {
        _dataLock.EnterReadLock();
        try
        {
            return _tariffs.Find(t => t.Id == tariffId);
        }
        finally
        {
            _dataLock.ExitReadLock();
        }
    }

    // ── Connection helpers ────────────────────────────────────

    /// <summary>
    /// Finds the connection that owns the given Token B.
    /// Uses constant-time comparison to match production security posture.
    /// </summary>
    public ConnectionState? FindConnectionByTokenB(string token)
    {
        var tokenBytes = System.Text.Encoding.UTF8.GetBytes(token);
        foreach (var conn in Connections.Values)
        {
            var issuedToken = conn.IssuedTokenB;
            if (issuedToken is null)
                continue;

            var expectedBytes = System.Text.Encoding.UTF8.GetBytes(issuedToken);
            if (System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(tokenBytes, expectedBytes))
                return conn;
        }
        return null;
    }

    /// <summary>Returns the default (first) connection, or null.</summary>
    public ConnectionState? DefaultConnection => Connections.Values.FirstOrDefault(c => c.IssuedTokenB is not null);

    // ── Request history ───────────────────────────────────────

    public void TrimRequestHistory()
    {
        while (RequestHistory.Count > MaxRequestHistory)
            RequestHistory.TryDequeue(out _);
    }

    // ── EVSE initialization ───────────────────────────────────

    public void InitializeEvses(IReadOnlyList<LocationSpec> locations)
    {
        foreach (var location in locations)
        {
            foreach (var evse in location.Evses)
            {
                var state = new EvseState
                {
                    LocationId = location.Id,
                    EvseUid = evse.Uid,
                    Connector = evse.Connector,
                };
                Evses[$"{location.Id}:{evse.Uid}"] = state;
            }
        }
    }

    public void Dispose() => _dataLock.Dispose();
}
