---
title: DI Registration
layout: default
parent: API Reference
nav_order: 2
---

# DI Registration
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Entry Point

All DotOcpi services are registered through `AddDotOcpi()`:

```csharp
// Programmatic configuration
var dotOcpiBuilder = builder.Services.AddDotOcpi(options =>
{
    options.SupportedVersions = [OcpiVersion.V2_2_1];
    options.DefaultEmspIdentity = new PartyIdentity("NL", "MSP");
    options.BaseUrl = new Uri("https://my-emsp.com/ocpi");
});

// Or bind from IConfiguration
var dotOcpiBuilder = builder.Services.AddDotOcpi(
    builder.Configuration.GetSection("DotOcpi"));
```

Both overloads return a `DotOcpiBuilder` for chaining.

## DotOcpiBuilder

The builder provides methods to register additional services:

### Token Storage

```csharp
// Development: in-memory (lost on restart)
dotOcpiBuilder.AddInMemoryTokenStore();

// Production: your implementation
dotOcpiBuilder.AddTokenStore<AzureKeyVaultTokenStore>();
```

### CPO Registry

```csharp
// Development: in-memory (lost on restart)
dotOcpiBuilder.AddInMemoryCpoRegistry();

// Production: persistent backing store
dotOcpiBuilder.AddCpoRegistryStore<SqlCpoRegistryStore>();
```

### ASP.NET Core Server

```csharp
// Register server middleware, filters, and endpoint handlers
// Requires DotOcpi.AspNetCore package
dotOcpiBuilder.AddAspNetCoreServer();
```

### Client

```csharp
// Register all OCPI client services (ILocationsClient, etc.)
// Requires DotOcpi.Client package
dotOcpiBuilder.AddClient();
```

### Token Protector

```csharp
// Custom token protector for encrypting outbound CPO tokens at rest
// (AddAspNetCoreServer() provides a Data Protection-backed one by default)
dotOcpiBuilder.AddTokenProtector<MyCustomProtector>();
```

### Pull Sync

```csharp
// Register automatic pull synchronization
dotOcpiBuilder.AddPullSync(options =>
{
    options.DefaultInterval = TimeSpan.FromHours(1);
    options.EnabledModules = ["locations", "tariffs"];
    options.MaxJitter = TimeSpan.FromMinutes(5);
});

// Register a sync handler to receive pulled data
dotOcpiBuilder.AddSyncHandler<MySyncHandler>();
```

## Full Registration Example

```csharp
builder.Services.AddDotOcpi(options =>
{
    options.SupportedVersions = [OcpiVersion.V2_2_1, OcpiVersion.V2_1_1];
    options.DefaultEmspIdentity = new PartyIdentity("NL", "MSP");
    options.BaseUrl = new Uri("https://my-emsp.com/ocpi");
    options.EnableHealthMonitoring = true;
    options.HealthMonitoringInterval = TimeSpan.FromMinutes(5);
    options.Logging.SanitizeTokensInLogs = true;
})
.AddInMemoryTokenStore()
.AddInMemoryCpoRegistry()
.AddAspNetCoreServer()
.AddClient()
.AddPullSync(sync =>
{
    sync.DefaultInterval = TimeSpan.FromHours(1);
    sync.EnabledModules = ["locations", "tariffs", "sessions"];
});

// Register module handlers
builder.Services.AddSingleton<ILocationsReceiver, MyLocationsReceiver>();
builder.Services.AddSingleton<ISessionsReceiver, MySessionsReceiver>();
builder.Services.AddSingleton<ICdrsReceiver, MyCdrsReceiver>();
builder.Services.AddSingleton<ITariffsReceiver, MyTariffsReceiver>();
builder.Services.AddSingleton<ITokensSender, MyTokensSender>();
builder.Services.AddSingleton<ITokensAuthorizer, MyTokensAuthorizer>();
builder.Services.AddSingleton<ICommandsCallback, MyCommandsCallback>();

var app = builder.Build();
app.MapAllOcpiEndpoints();
app.Run();
```

## Endpoint Mapping

After building the application, map OCPI endpoints:

```csharp
// Maps all module endpoints in one call
app.MapAllOcpiEndpoints();

// Or use MapOcpiEndpoints() for selective module registration:
var group = app.MapOcpiEndpoints();   // Middleware + auth only (no modules)
group.MapLocationsEndpoints();         // Add only the modules you need
group.MapSessionsEndpoints();
```

### Rate Limiting

Pass `OcpiRateLimitOptions` to enable per-CPO rate limiting:

```csharp
app.MapAllOcpiEndpoints(rateLimitOptions: new OcpiRateLimitOptions
{
    MaxRequestsPerWindow = 100,   // Requests per CPO per window
    Window = TimeSpan.FromMinutes(1),
});
```

| Property | Type | Default | Description |
|:---------|:-----|:--------|:------------|
| `MaxRequestsPerWindow` | `int` | 100 | Maximum requests allowed per CPO within the time window |
| `Window` | `TimeSpan` | 1 minute | The fixed time window for rate limiting |

When a CPO exceeds the limit, they receive HTTP 429 with a `Retry-After` header and OCPI status 2000.

{: .note }
> Rate limiting is partitioned by CPO connection key. Each CPO has its own independent rate limit bucket. Without passing `rateLimitOptions`, no rate limiting is applied.

`MapAllOcpiEndpoints()` registers:
- Middleware pipeline (request IDs, security headers, exception handling)
- Auth + metrics filters (rate limiting only when `rateLimitOptions` is passed)
- Module endpoints for all registered handlers (credentials, locations, sessions, CDRs, tariffs, tokens, commands, charging profiles)

Both URL patterns are registered:
- `/{module}/{object_id}` for OCPI 2.0/2.1.1
- `/{module}/{country_code}/{party_id}/{object_id}` for OCPI 2.2/2.2.1

## Services Registered

### By `AddDotOcpi()`

| Service | Lifetime | Description |
|:--------|:---------|:------------|
| `DotOcpiOptions` | Options | Configuration (via `IOptions<DotOcpiOptions>`) |
| `OcpiMetrics` | Singleton | Metrics instrumentation |
| `ITokenProtector` | Singleton | Token encryption (default: `PlaintextTokenProtector`) |
| `TimeProvider` | Singleton | Enables deterministic time in tests |
| `ICpoRegistry` | Singleton | CPO connection registry (after `AddInMemoryCpoRegistry()`) |
| `ITokenStore` | Singleton | Token hash storage (after `AddInMemoryTokenStore()`) |

### By `AddClient()`

| Service | Lifetime | Description |
|:--------|:---------|:------------|
| `IRegistrationClient` | Singleton | Full registration orchestrator |
| `IVersionDiscovery` | Singleton | Version discovery |
| `ICredentialsClient` | Singleton | Low-level credentials operations |
| `IOutboundTokenProvider` | Singleton | Retrieves Token B for outbound CPO requests (default: reads from `ITokenStore`) |
| `IOcpiClient` | Singleton | Client facade |
| `ILocationsClient` | Singleton | Locations pull client |
| `ISessionsClient` | Singleton | Sessions pull client |
| `ICdrsClient` | Singleton | CDRs pull client |
| `ITariffsClient` | Singleton | Tariffs pull client |
| `ITokensClient` | Singleton | Tokens push client |
| `ICommandsClient` | Singleton | Commands client |
| `IChargingProfilesClient` | Singleton | Charging profiles client |

## Startup Validation

DotOcpi validates configuration at startup using `IValidateOptions<DotOcpiOptions>`. Invalid configuration throws `OptionsValidationException` with a clear message:

```
At least one supported OCPI version must be configured.
BaseUrl must use HTTPS. Got: http
HealthMonitoringInterval must be positive.
```
