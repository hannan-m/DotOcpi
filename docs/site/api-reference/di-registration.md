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

### Pull Sync

```csharp
// Register automatic pull synchronization
dotOcpiBuilder.AddPullSync(options =>
{
    options.DefaultInterval = TimeSpan.FromHours(1);
    options.EnabledModules = ["locations", "tariffs"];
    options.MaxJitter = TimeSpan.FromMinutes(5);
});
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
app.MapOcpiEndpoints();
app.Run();
```

## Endpoint Mapping

After building the application, map OCPI endpoints:

```csharp
app.MapOcpiEndpoints();
```

This registers:
- `/ocpi/versions` — Version discovery
- `/ocpi/versions/{id}` — Version detail
- `/ocpi/credentials` — Credentials endpoint (POST/PUT/DELETE/GET)
- Module endpoints for all registered handlers (locations, sessions, CDRs, tariffs, tokens, commands, charging profiles)

Both URL patterns are registered:
- `/{module}/{object_id}` for OCPI 2.0/2.1.1
- `/{module}/{country_code}/{party_id}/{object_id}` for OCPI 2.2/2.2.1

## Services Registered

After calling `AddDotOcpi()`, these services are available via DI:

| Service | Lifetime | Description |
|:--------|:---------|:------------|
| `IRegistrationClient` | Scoped | Full registration orchestrator |
| `IVersionDiscovery` | Singleton | Version discovery |
| `ICredentialsClient` | Scoped | Low-level credentials operations |
| `ICpoRegistry` | Singleton | CPO connection registry |
| `ITokenStore` | Singleton | Token hash storage |
| `IOutboundTokenProvider` | *Consumer-provided* | Retrieves raw Token B for outbound CPO requests. Consumers must register their own implementation. |
| `IOcpiClient` | Singleton | Client facade |
| `ILocationsClient` | Singleton | Locations pull client |
| `ISessionsClient` | Singleton | Sessions pull client |
| `ICdrsClient` | Singleton | CDRs pull client |
| `ITariffsClient` | Singleton | Tariffs pull client |
| `ITokensClient` | Singleton | Tokens push client |
| `ICommandsClient` | Singleton | Commands client |
| `IChargingProfilesClient` | Singleton | Charging profiles client |

## Startup Validation

DotOcpi validates configuration at startup. Missing or invalid configuration throws `OcpiConfigurationException` with a clear message:

```
DotOcpi configuration error: SupportedVersions must contain at least one version.
DotOcpi configuration error: BaseUrl must use HTTPS scheme.
DotOcpi configuration error: No ILocationsReceiver registered. Register an implementation or remove the locations module.
```
