---
title: Sync
layout: default
parent: API Reference
nav_order: 8
---

# Pull Sync API
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Overview

DotOcpi's pull sync system automatically fetches data from CPOs on a configurable schedule. It handles pagination, incremental sync (using `dateFrom`), and delivers results to your `IOcpiSyncHandler`.

```
Timer → IOcpiSyncService → CPO GET /locations?date_from=... → Paginate
    → IOcpiSyncHandler.OnPageReceivedAsync (per page)
    → IOcpiSyncHandler.OnSyncCompletedAsync (when done)
    → ISyncStateStore.SetLastSyncAsync (checkpoint)
```

## Registration

```csharp
builder.Services.AddDotOcpi(options => { /* ... */ })
    .AddClient()
    .AddSyncHandler<MySyncHandler>()
    .AddPullSync(sync =>
    {
        sync.DefaultInterval = TimeSpan.FromHours(1);
        sync.EnabledModules = ["locations", "tariffs"];
    });
```

---

## IOcpiSyncHandler

Implement this interface to receive pulled data. Register it with `AddSyncHandler<T>()`.

```csharp
public interface IOcpiSyncHandler
{
    Task OnPageReceivedAsync(
        SyncContext context,
        IReadOnlyList<object> items,
        CancellationToken cancellationToken = default);

    Task OnSyncCompletedAsync(
        SyncContext context,
        SyncResult result,
        CancellationToken cancellationToken = default);
}
```

### OnPageReceivedAsync

Called once per page of results fetched from a CPO. Pages with zero items are not delivered.

- `items` contains version-specific model objects (e.g., `Models.V2_2_1.Location`). Cast based on `context.Version`.
- Called multiple times per sync cycle if the CPO returns paginated results.

### OnSyncCompletedAsync

Called once after all pages for a CPO+module sync cycle are successfully fetched and delivered.

- **Not** called if the sync fails or is cancelled.
- Use this for post-sync tasks: logging, metrics, cache invalidation, event broadcasting.

### Example Implementation

```csharp
public class MySyncHandler : IOcpiSyncHandler
{
    private readonly MyDatabase _db;
    private readonly ILogger<MySyncHandler> _logger;

    public MySyncHandler(MyDatabase db, ILogger<MySyncHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task OnPageReceivedAsync(
        SyncContext context,
        IReadOnlyList<object> items,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            // Items are version-specific models — store or process them
            switch (context.ModuleId)
            {
                case "locations":
                    await _db.UpsertLocationAsync(context.CpoId, item, cancellationToken);
                    break;
                case "tariffs":
                    await _db.UpsertTariffAsync(context.CpoId, item, cancellationToken);
                    break;
            }
        }
    }

    public Task OnSyncCompletedAsync(
        SyncContext context,
        SyncResult result,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Synced {Module} from {CpoId}: {Items} items, {Pages} pages in {Duration}",
            context.ModuleId, context.CpoId,
            result.ItemCount, result.PageCount, result.Duration);

        return Task.CompletedTask;
    }
}
```

---

## SyncContext

Context passed to your sync handler with details about the current sync operation.

```csharp
public sealed class SyncContext
{
    public required string CpoId { get; init; }              // CPO connection key (e.g., "DE:CPO")
    public required string ModuleId { get; init; }            // Module being synced (e.g., "locations")
    public required OcpiVersion Version { get; init; }        // Negotiated OCPI version
    public DateTimeOffset? DateFrom { get; init; }            // Sync window start, or null for full pull
    public required DateTimeOffset SyncStartedAt { get; init; } // When this sync cycle began (UTC)
}
```

---

## SyncResult

Outcome of a completed sync cycle, passed to `OnSyncCompletedAsync`.

```csharp
public sealed class SyncResult
{
    public required int ItemCount { get; init; }      // Total items received across all pages
    public required int PageCount { get; init; }      // Number of pages fetched
    public required TimeSpan Duration { get; init; }  // Wall-clock duration
    public required bool IsSuccess { get; init; }     // True if sync completed without errors
    public string? ErrorMessage { get; init; }        // Error message if sync failed
}
```

---

## IOcpiSyncService

Service for triggering manual sync operations. Registered automatically by `AddPullSync()`.

```csharp
public interface IOcpiSyncService
{
    Task SyncFromCpoAsync(
        string cpoId,
        DateTimeOffset? since = null,
        CancellationToken cancellationToken = default);

    Task<SyncResult> SyncModuleFromCpoAsync(
        string cpoId,
        string moduleId,
        DateTimeOffset? since = null,
        CancellationToken cancellationToken = default);
}
```

### Manual Sync

Resolve `IOcpiSyncService` from DI to trigger sync on demand:

```csharp
app.MapPost("/api/sync/{cpoId}", async (string cpoId, IOcpiSyncService syncService) =>
{
    await syncService.SyncFromCpoAsync(cpoId);
    return Results.Ok();
});

app.MapPost("/api/sync/{cpoId}/{module}", async (
    string cpoId, string module, IOcpiSyncService syncService) =>
{
    var result = await syncService.SyncModuleFromCpoAsync(cpoId, module);
    return Results.Ok(new { result.ItemCount, result.PageCount, result.Duration });
});
```

---

## PullSyncOptions

```csharp
public sealed class PullSyncOptions
{
    public TimeSpan DefaultInterval { get; set; } = TimeSpan.FromHours(1);
    public List<string> EnabledModules { get; set; } = ["locations", "tariffs"];
    public TimeSpan MaxJitter { get; set; } = TimeSpan.FromMinutes(5);
    public Dictionary<string, ModuleSyncOptions> ModuleOverrides { get; set; } = new();
    public Dictionary<string, CpoSyncOptions> CpoOverrides { get; set; } = new();
}
```

| Property | Default | Description |
|:---------|:--------|:------------|
| `DefaultInterval` | 1 hour | Time between sync cycles |
| `EnabledModules` | `["locations", "tariffs"]` | Valid: `"locations"`, `"sessions"`, `"cdrs"`, `"tariffs"` |
| `MaxJitter` | 5 minutes | Random delay to prevent thundering herd |
| `ModuleOverrides` | empty | Per-module interval overrides |
| `CpoOverrides` | empty | Per-CPO overrides (interval, modules, per-module) |

### Resolution Order

The sync interval for a given CPO + module is resolved in this order:

1. `CpoOverrides["DE:CPO"].ModuleOverrides["locations"].Interval`
2. `CpoOverrides["DE:CPO"].DefaultInterval`
3. `ModuleOverrides["locations"].Interval`
4. `DefaultInterval`

### Per-Module Overrides

```csharp
.AddPullSync(sync =>
{
    sync.DefaultInterval = TimeSpan.FromHours(1);
    sync.EnabledModules = ["locations", "sessions", "tariffs"];

    // Sync sessions more frequently than locations
    sync.ModuleOverrides["sessions"] = new ModuleSyncOptions
    {
        Interval = TimeSpan.FromMinutes(15)
    };
});
```

### Per-CPO Overrides

```csharp
.AddPullSync(sync =>
{
    sync.DefaultInterval = TimeSpan.FromHours(1);
    sync.EnabledModules = ["locations", "tariffs"];

    // This CPO has frequent session updates — sync more often
    sync.CpoOverrides["DE:CPO"] = new CpoSyncOptions
    {
        DefaultInterval = TimeSpan.FromMinutes(30),
        EnabledModules = ["locations", "sessions", "tariffs"],
        ModuleOverrides =
        {
            ["sessions"] = new ModuleSyncOptions { Interval = TimeSpan.FromMinutes(5) }
        }
    };
});
```

---

## ISyncStateStore

Tracks the last successful sync timestamp per CPO per module. Used for incremental pulls (`dateFrom`).

```csharp
public interface ISyncStateStore
{
    Task<DateTimeOffset?> GetLastSyncAsync(
        string cpoId, string moduleId, CancellationToken cancellationToken = default);

    Task SetLastSyncAsync(
        string cpoId, string moduleId, DateTimeOffset timestamp,
        CancellationToken cancellationToken = default);
}
```

The built-in `InMemorySyncStateStore` loses state on restart (full re-sync on next startup). For production, implement `ISyncStateStore` with a database:

```csharp
public class SqlSyncStateStore : ISyncStateStore
{
    private readonly IDbConnectionFactory _dbFactory;

    public async Task<DateTimeOffset?> GetLastSyncAsync(
        string cpoId, string moduleId, CancellationToken cancellationToken)
    {
        using var db = await _dbFactory.CreateConnectionAsync(cancellationToken);
        return await db.QuerySingleOrDefaultAsync<DateTimeOffset?>(
            "SELECT LastSync FROM SyncState WHERE CpoId = @CpoId AND ModuleId = @Module",
            new { CpoId = cpoId, Module = moduleId });
    }

    public async Task SetLastSyncAsync(
        string cpoId, string moduleId, DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        using var db = await _dbFactory.CreateConnectionAsync(cancellationToken);
        await db.ExecuteAsync(
            """
            MERGE INTO SyncState AS t
            USING (SELECT @CpoId AS CpoId, @Module AS ModuleId) AS s
            ON t.CpoId = s.CpoId AND t.ModuleId = s.ModuleId
            WHEN MATCHED THEN UPDATE SET LastSync = @Timestamp
            WHEN NOT MATCHED THEN INSERT (CpoId, ModuleId, LastSync) VALUES (@CpoId, @Module, @Timestamp);
            """,
            new { CpoId = cpoId, Module = moduleId, Timestamp = timestamp });
    }
}
```
