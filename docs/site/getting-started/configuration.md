---
title: Configuration
layout: default
parent: Getting Started
nav_order: 3
---

# Configuration
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## DotOcpiOptions

All configuration starts with `AddDotOcpi()`:

```csharp
builder.Services.AddDotOcpi(options =>
{
    // Required: which OCPI versions your eMSP supports
    options.SupportedVersions = [OcpiVersion.V2_2_1, OcpiVersion.V2_1_1];

    // Required: your default eMSP identity
    options.DefaultEmspIdentity = new PartyIdentity("NL", "MSP");

    // Required: your eMSP's public base URL (must be HTTPS in production)
    options.BaseUrl = new Uri("https://my-emsp.com/ocpi");

    // Optional: health monitoring
    options.EnableHealthMonitoring = true;
    options.HealthMonitoringInterval = TimeSpan.FromMinutes(5);
    options.StaleConnectionThreshold = TimeSpan.FromHours(24);

    // Optional: logging
    options.Logging.EnableRequestBodyLogging = false;
    options.Logging.EnableResponseBodyLogging = false;
    options.Logging.MaxBodyLogLength = 4096;
    options.Logging.SanitizeTokensInLogs = true; // always true in production
});
```

### Options Reference

| Property | Type | Default | Description |
|:---------|:-----|:--------|:------------|
| `SupportedVersions` | `IReadOnlyList<OcpiVersion>` | — | OCPI versions your eMSP supports |
| `DefaultEmspIdentity` | `PartyIdentity?` | — | Default country_code/party_id |
| `BaseUrl` | `Uri?` | — | Public base URL for OCPI endpoints |
| `EnableHealthMonitoring` | `bool` | `true` | Enable background CPO health probing |
| `HealthMonitoringInterval` | `TimeSpan` | 5 min | How often to probe stale connections |
| `StaleConnectionThreshold` | `TimeSpan` | 24 hours | Mark connections stale after this idle time |

### Logging Options

| Property | Type | Default | Description |
|:---------|:-----|:--------|:------------|
| `EnableRequestBodyLogging` | `bool` | `false` | Log incoming request bodies |
| `EnableResponseBodyLogging` | `bool` | `false` | Log outgoing response bodies |
| `MaxBodyLogLength` | `int` | `4096` | Truncate logged bodies to this length |
| `SanitizeTokensInLogs` | `bool` | `true` | Redact tokens in log output |

{: .warning }
> Never set `SanitizeTokensInLogs = false` in production. Raw tokens in logs are a security risk.

## Configuration via appsettings.json

You can bind from configuration instead of code:

```csharp
builder.Services.AddDotOcpi(builder.Configuration.GetSection("DotOcpi"));
```

```json
{
  "DotOcpi": {
    "SupportedVersions": ["V2_2_1", "V2_1_1"],
    "DefaultEmspIdentity": {
      "CountryCode": "NL",
      "PartyId": "MSP"
    },
    "BaseUrl": "https://my-emsp.com/ocpi",
    "EnableHealthMonitoring": true,
    "Logging": {
      "EnableRequestBodyLogging": false,
      "SanitizeTokensInLogs": true
    }
  }
}
```

## Builder Extensions

The `AddDotOcpi()` call returns a `DotOcpiBuilder` that chains further registration:

```csharp
builder.Services.AddDotOcpi(options => { /* ... */ })
    // Token storage
    .AddInMemoryTokenStore()          // Development only
    // .AddTokenStore<MyTokenStore>() // Production: your ITokenStore implementation

    // CPO registry
    .AddInMemoryCpoRegistry()             // Development only
    // .AddCpoRegistryStore<MyStore>()    // Production: your ICpoRegistryStore

    // Server endpoints (requires DotOcpi.AspNetCore)
    .AddAspNetCoreServer()

    // Client services (requires DotOcpi.Client)
    .AddClient()

    // Pull sync (optional, requires DotOcpi.Client)
    .AddPullSync(sync =>
    {
        sync.DefaultInterval = TimeSpan.FromHours(1);
        sync.EnabledModules = ["locations", "tariffs"];
        sync.MaxJitter = TimeSpan.FromMinutes(5);
    });
```

## Token Store

DotOcpi needs somewhere to store token hashes. The built-in `InMemoryTokenStore` is for development only — tokens are lost on restart.

For production, implement `ITokenStore`:

```csharp
.AddTokenStore<AzureKeyVaultTokenStore>()
```

See [Custom Token Store](/DotOcpi/advanced/custom-token-store/) for a full implementation guide.

## CPO Registry

The CPO registry tracks all connected CPOs, their negotiated versions, and module endpoints. The built-in `InMemoryCpoRegistry` is for development only.

For production, implement `ICpoRegistryStore`:

```csharp
.AddCpoRegistryStore<SqlCpoRegistryStore>()
```

See [Custom Registry Store](/DotOcpi/advanced/custom-registry-store/) for a full implementation guide.

## Server Endpoint Mapping

After building the app, map OCPI endpoints:

```csharp
var app = builder.Build();
app.MapAllOcpiEndpoints();
app.Run();
```

This registers all OCPI module endpoints with the correct URL patterns for each version:

| Version | URL Pattern | Example |
|:--------|:------------|:--------|
| 2.0, 2.1.1 | `/{object_id}` | `/ocpi/locations/LOC001` |
| 2.2, 2.2.1 | `/{country_code}/{party_id}/{object_id}` | `/ocpi/locations/DE/CPO/LOC001` |

## Module Handler Registration

You must register implementations for the module interfaces you want to handle. Missing registrations produce a clear startup error:

```csharp
// Required for receiving location pushes
builder.Services.AddSingleton<ILocationsReceiver, MyLocationsReceiver>();

// Required for receiving session pushes
builder.Services.AddSingleton<ISessionsReceiver, MySessionsReceiver>();

// Required for receiving CDR pushes
builder.Services.AddSingleton<ICdrsReceiver, MyCdrsReceiver>();

// Required for receiving tariff pushes
builder.Services.AddSingleton<ITariffsReceiver, MyTariffsReceiver>();

// Required for token authorization (real-time auth)
builder.Services.AddSingleton<ITokensAuthorizer, MyTokensAuthorizer>();

// Required for providing tokens to CPOs
builder.Services.AddSingleton<ITokensSender, MyTokensSender>();

// Optional: command callback handling
builder.Services.AddSingleton<ICommandsCallback, MyCommandsCallback>();

// Optional: charging profile callback handling (2.2+ only)
builder.Services.AddSingleton<IChargingProfilesCallback, MyChargingProfilesCallback>();
```

## Multi-Party Configuration

To present different eMSP identities to different CPOs:

```csharp
// During registration, specify the eMSP identity for this CPO connection
var result = await registrationClient.RegisterAsync(new RegistrationRequest(
    VersionsUrl: "https://cpo-a.com/ocpi/versions",
    TokenA: "token-a-for-cpo-a",
    EmspCountryCode: "NL",      // Different identity per CPO
    EmspPartyId: "MSP",
    EmspVersionsUrl: "https://my-emsp.com/ocpi/versions",
    EmspBusinessName: "My eMSP (NL)"
));

var result2 = await registrationClient.RegisterAsync(new RegistrationRequest(
    VersionsUrl: "https://cpo-b.com/ocpi/versions",
    TokenA: "token-a-for-cpo-b",
    EmspCountryCode: "DE",      // Different identity
    EmspPartyId: "EMS",
    EmspVersionsUrl: "https://my-emsp.com/ocpi/versions",
    EmspBusinessName: "My eMSP (DE)"
));
```

Each CPO connection is independently configured with its own eMSP identity, tokens, and endpoints.

## Log Level Configuration

Configure per-component log levels in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "DotOcpi": "Information",
      "DotOcpi.Auth": "Warning",
      "DotOcpi.Client": "Debug",
      "DotOcpi.Client.Locations": "Information",
      "DotOcpi.Server": "Information",
      "DotOcpi.Registration": "Debug",
      "DotOcpi.Sync": "Information",
      "DotOcpi.Health": "Warning"
    }
  }
}
```

See [Observability](/DotOcpi/api-reference/observability/) for the full list of log categories and event IDs.

## Startup Validation

DotOcpi validates your configuration at startup:

- `SupportedVersions` must not be empty
- `BaseUrl` must use HTTPS (except in Development environment)
- `DefaultEmspIdentity` must be set
- Required module handler interfaces must be registered

If validation fails, you get a clear `OcpiConfigurationException` at startup with a specific message explaining what to fix.
