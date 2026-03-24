---
title: CPO Registry
layout: default
parent: API Reference
nav_order: 6
---

# CPO Registry
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Overview

The CPO Registry is DotOcpi's in-memory store of connected CPOs. It tracks each CPO's negotiated version, module endpoints, credential state, and connection status.

## CpoConnection

The central data model for a CPO connection.

```csharp
public sealed record CpoConnection
{
    public required string CpoCountryCode { get; init; }
    public required string CpoPartyId { get; init; }
    public required string EmspCountryCode { get; init; }
    public required string EmspPartyId { get; init; }
    public required OcpiVersion Version { get; init; }
    public required IReadOnlyDictionary<string, string> ModuleEndpoints { get; init; }
    public required string TokenBHash { get; init; }
    public required ConnectionStatus Status { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public string? CpoVersionsUrl { get; init; }
    public string? EmspVersionsUrl { get; init; }
    public DateTimeOffset? LastHealthCheckAt { get; init; }
    public long ConcurrencyVersion { get; init; }

    // Computed property
    public string ConnectionKey { get; }  // "{CpoCountryCode}:{CpoPartyId}"
}
```

### ConnectionStatus

```csharp
public enum ConnectionStatus
{
    Pending,       // Registration in progress
    Connected,     // Active and healthy
    Offline,       // Health check failures
    Unregistered,  // Disconnected
    Suspended,     // Suspended due to repeated failures
}
```

## ICpoRegistry

Three-index lookup for CPO connections.

```csharp
public interface ICpoRegistry
{
    CpoConnection? FindByConnectionKey(string connectionKey);
    CpoConnection? FindByTokenHash(string tokenBHash);
    IReadOnlyList<CpoConnection> FindByEmspIdentity(string emspCountryCode, string emspPartyId);
    IReadOnlyList<CpoConnection> GetAll();
    bool AddOrUpdate(CpoConnection connection);
    bool Remove(string connectionKey);
}
```

### Lookup Paths

| Path | Use Case | Performance |
|:-----|:---------|:------------|
| `FindByConnectionKey("DE:CPO")` | Outbound requests — look up CPO by ID | O(1) dictionary lookup |
| `FindByTokenHash(hash)` | Inbound auth — find CPO by token hash | O(1) dictionary lookup |
| `FindByEmspIdentity("NL", "MSP")` | Multi-party — find all CPOs for an eMSP identity | O(1) dictionary lookup |

### Usage

```csharp
// Look up by connection key
var connection = registry.FindByConnectionKey("DE:CPO");
if (connection is not null)
{
    Console.WriteLine($"Version: {connection.Version}");
    Console.WriteLine($"Status: {connection.Status}");
    Console.WriteLine($"Endpoints: {string.Join(", ", connection.ModuleEndpoints.Keys)}");
}

// Find all CPOs connected to a specific eMSP identity
var connections = registry.FindByEmspIdentity("NL", "MSP");
foreach (var conn in connections)
{
    Console.WriteLine($"CPO: {conn.ConnectionKey} on {conn.Version}");
}
```

### Optimistic Concurrency

The registry uses optimistic concurrency via `ConcurrencyVersion`:

```csharp
var connection = registry.FindByConnectionKey("DE:CPO");
// Modify and save — if another thread modified it, AddOrUpdate returns false
var updated = connection with { Status = ConnectionStatus.Offline };
var success = registry.AddOrUpdate(updated);
if (!success)
{
    // Retry with fresh data
}
```

## ICpoRegistryStore

Persistent backing for the CPO registry. Implement this for production deployments:

```csharp
public interface ICpoRegistryStore
{
    Task<IReadOnlyList<CpoConnection>> LoadAllAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(CpoConnection connection, CancellationToken cancellationToken = default);
    Task RemoveAsync(string connectionKey, CancellationToken cancellationToken = default);
}
```

### Usage

```csharp
dotOcpiBuilder.AddCpoRegistryStore<SqlCpoRegistryStore>();

public class SqlCpoRegistryStore : ICpoRegistryStore
{
    private readonly MyDbContext _db;

    public SqlCpoRegistryStore(MyDbContext db) => _db = db;

    public async Task<IReadOnlyList<CpoConnection>> LoadAllAsync(CancellationToken ct)
    {
        return await _db.CpoConnections.ToListAsync(ct);
    }

    public async Task SaveAsync(CpoConnection connection, CancellationToken ct)
    {
        _db.CpoConnections.Update(connection);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(string connectionKey, CancellationToken ct)
    {
        var entity = await _db.CpoConnections.FindAsync(connectionKey, ct);
        if (entity is not null)
        {
            _db.CpoConnections.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}
```

## Health Monitoring

`CpoHealthMonitor` is registered automatically by `AddAspNetCoreServer()` and respects `EnableHealthMonitoring` (default: `true`).

Configure health probing thresholds:

```csharp
builder.Services.AddDotOcpi(options =>
{
    options.EnableHealthMonitoring = true;
    options.HealthMonitoringInterval = TimeSpan.FromMinutes(5);
    options.StaleConnectionThreshold = TimeSpan.FromHours(24);
});
```

The `CpoHealthMonitor` background service:
1. Every `HealthMonitoringInterval`, probes all non-pending/non-unregistered CPO connections
2. Sends a `GET` probe to the CPO's versions endpoint
3. After consecutive failures (default: 3), marks the connection as `Offline`
4. Marks recovered connections as `Connected`
