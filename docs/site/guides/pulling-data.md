---
title: Pulling Data
layout: default
parent: Guides
nav_order: 3
---

# Pulling Data from CPOs
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Overview

Besides receiving pushes, your eMSP can actively **pull** data from CPOs. This is useful for:

- Initial data sync after registration
- Periodic reconciliation
- Catching up after downtime

## Client Interfaces

Each OCPI module has a dedicated client interface:

| Interface | Methods |
|:----------|:--------|
| `ILocationsClient` | `GetAllLocationsAsync`, `GetLocationAsync` |
| `ISessionsClient` | `GetAllSessionsAsync`, `PutChargingPreferencesAsync` |
| `ICdrsClient` | `GetAllCdrsAsync` |
| `ITariffsClient` | `GetAllTariffsAsync` |

All list methods return `IAsyncEnumerable<T>` with automatic pagination.

## Pulling Locations

```csharp
// Inject IOcpiClient
private readonly IOcpiClient _ocpiClient;

// Pull all locations from a CPO
await foreach (var location in _ocpiClient.Locations.GetAllLocationsAsync("DE:CPO"))
{
    await ProcessLocationAsync(location);
}

// Pull with date filter (incremental sync)
var since = DateTimeOffset.UtcNow.AddHours(-1);
await foreach (var location in _ocpiClient.Locations.GetAllLocationsAsync(
    "DE:CPO",
    dateFrom: since))
{
    await ProcessLocationAsync(location);
}

// Pull with date range
await foreach (var location in _ocpiClient.Locations.GetAllLocationsAsync(
    "DE:CPO",
    dateFrom: DateTimeOffset.UtcNow.AddDays(-7),
    dateTo: DateTimeOffset.UtcNow))
{
    await ProcessLocationAsync(location);
}

// Get a single location
var result = await _ocpiClient.Locations.GetLocationAsync("DE:CPO", "LOC001");
if (result.IsSuccess)
{
    var location = result.Data;
}
```

## Pulling Sessions

```csharp
await foreach (var session in _ocpiClient.Sessions.GetAllSessionsAsync("DE:CPO"))
{
    await ProcessSessionAsync(session);
}

// Send charging preferences (OCPI 2.2+ only)
var result = await _ocpiClient.Sessions.PutChargingPreferencesAsync(
    "DE:CPO",
    "SESSION001",
    new
    {
        profile_type = "FAST",
        departure_time = DateTimeOffset.UtcNow.AddHours(2),
        energy_need = 50.0,
    });
```

## Pulling CDRs

```csharp
await foreach (var cdr in _ocpiClient.Cdrs.GetAllCdrsAsync("DE:CPO"))
{
    await ProcessCdrAsync(cdr);
}
```

## Pulling Tariffs

```csharp
await foreach (var tariff in _ocpiClient.Tariffs.GetAllTariffsAsync("DE:CPO"))
{
    await ProcessTariffAsync(tariff);
}
```

## Automatic Pagination

All `GetAll*Async` methods handle pagination transparently:

```csharp
// This automatically follows Link headers across all pages
await foreach (var location in _ocpiClient.Locations.GetAllLocationsAsync("DE:CPO"))
{
    // You get every location, regardless of how many pages the CPO returns
    Console.WriteLine(location);
}
```

The client:
1. Sends the initial `GET` request
2. Reads the `Link` header for the next page URL
3. Follows pages until no more `Link` headers are returned
4. Yields each item as an `IAsyncEnumerable<T>` (no buffering)

## Pull Sync Service

For automated periodic synchronization, use the built-in `PullSyncService`:

```csharp
// Register during startup
builder.Services.AddDotOcpi(options => { /* ... */ })
    .AddClient()
    .AddPullSync(sync =>
    {
        sync.DefaultInterval = TimeSpan.FromHours(1);  // Sync every hour
        sync.EnabledModules = ["locations", "tariffs"];    // Which modules to sync
        sync.MaxJitter = TimeSpan.FromMinutes(5);       // Prevent thundering herd
    });
```

### PullSyncOptions

| Property | Type | Default | Description |
|:---------|:-----|:--------|:------------|
| `DefaultInterval` | `TimeSpan` | 1 hour | Time between sync cycles |
| `EnabledModules` | `List<string>` | `["locations", "tariffs"]` | Modules to sync |
| `MaxJitter` | `TimeSpan` | 5 min | Random delay added to prevent all CPOs syncing at once |
| `ModuleOverrides` | `Dictionary<string, ModuleSyncOptions>` | empty | Per-module interval overrides |
| `CpoOverrides` | `Dictionary<string, CpoSyncOptions>` | empty | Per-CPO sync overrides |

### Custom Sync State

The sync service tracks the last sync time per CPO per module using `ISyncStateStore`:

```csharp
// Built-in in-memory store (tokens lost on restart)
// For production, implement ISyncStateStore:
public class SqlSyncStateStore : ISyncStateStore
{
    public async Task<DateTimeOffset?> GetLastSyncAsync(
        string cpoId, string moduleId, CancellationToken ct)
    {
        // Read from your database
    }

    public async Task SetLastSyncAsync(
        string cpoId, string moduleId, DateTimeOffset timestamp, CancellationToken ct)
    {
        // Write to your database
    }
}
```

## Using the IOcpiClient Facade

`IOcpiClient` is the main entry point for all client operations:

```csharp
public class MyBackgroundService : BackgroundService
{
    private readonly IOcpiClient _ocpiClient;

    public MyBackgroundService(IOcpiClient ocpiClient) => _ocpiClient = ocpiClient;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Registration
        var reg = await _ocpiClient.Registration.RegisterAsync(/* ... */, stoppingToken);

        // Pull data
        await foreach (var loc in _ocpiClient.Locations.GetAllLocationsAsync(
            reg.Connection.ConnectionKey, cancellationToken: stoppingToken))
        {
            // Process location
        }

        // Send commands
        await _ocpiClient.Commands.SendStartSessionAsync(
            reg.Connection.ConnectionKey,
            new { /* ... */ },
            stoppingToken);
    }
}
```

## Cancellation

All client methods accept `CancellationToken`:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

await foreach (var location in _ocpiClient.Locations.GetAllLocationsAsync(
    "DE:CPO", cancellationToken: cts.Token))
{
    // Cancellation stops pagination and disposes the HTTP response
}
```

## Resilience

The client uses `Microsoft.Extensions.Http.Resilience` (Polly v8) for automatic retry and circuit breaking:

- **Retry**: Exponential backoff (2s base, 60s max) with jitter for transient errors (408, 429, 5xx)
- **Circuit Breaker**: Opens after 5 failures, stays open for 30 seconds
- **Timeout**: Per-request timeout (configurable)

These are configured automatically when you call `.AddClient()`. To customize:

```csharp
builder.Services.AddDotOcpi(options => { /* ... */ })
    .AddClient();

// Override resilience settings on the named HttpClient
builder.Services.ConfigureHttpClientDefaults(b =>
{
    b.AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 3;
        options.CircuitBreaker.FailureRatio = 0.5;
    });
});
```

---

<div style="display: flex; justify-content: space-between; margin-top: 2rem;">
  <div>← <a href="/DotOcpi/guides/receiving-data/">Receiving Data</a></div>
  <div><a href="/DotOcpi/guides/sending-commands/">Sending Commands</a> →</div>
</div>
