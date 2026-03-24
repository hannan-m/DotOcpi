---
title: Observability
layout: default
parent: API Reference
nav_order: 7
---

# Observability
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Structured Logging

DotOcpi uses `[LoggerMessage]` source-generated methods for zero-allocation logging.

### Log Categories

Configure per-component log levels in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "DotOcpi": "Information",
      "DotOcpi.Auth": "Warning",
      "DotOcpi.Registration": "Debug",
      "DotOcpi.Client": "Information",
      "DotOcpi.Client.Versions": "Information",
      "DotOcpi.Client.Credentials": "Information",
      "DotOcpi.Client.Locations": "Debug",
      "DotOcpi.Client.Sessions": "Information",
      "DotOcpi.Client.Cdrs": "Information",
      "DotOcpi.Client.Tariffs": "Information",
      "DotOcpi.Client.Tokens": "Information",
      "DotOcpi.Client.Commands": "Information",
      "DotOcpi.Client.ChargingProfiles": "Information",
      "DotOcpi.Server": "Information",
      "DotOcpi.Server.Locations": "Information",
      "DotOcpi.Server.Sessions": "Information",
      "DotOcpi.Server.Cdrs": "Information",
      "DotOcpi.Server.Tariffs": "Information",
      "DotOcpi.Server.Tokens": "Information",
      "DotOcpi.Server.Commands": "Information",
      "DotOcpi.Server.ChargingProfiles": "Information",
      "DotOcpi.TokenStore": "Warning",
      "DotOcpi.CpoRegistry": "Information",
      "DotOcpi.Sync": "Information",
      "DotOcpi.Health": "Warning",
      "DotOcpi.Validation": "Warning"
    }
  }
}
```

### Event IDs

| Range | Component | Events |
|:------|:----------|:-------|
| 1001–1006 | Registration | HandshakeStarted, VersionNegotiated, RegistrationComplete, HandshakeFailed, TokenRotationComplete, Unregistered |
| 2001–2005 | Auth | AuthSuccess, AuthMissingToken, AuthInvalidToken, AuthTokenExpired, AuthCpoNotFound |
| 3001–3005 | Client | RequestSent, ResponseReceived, RequestFailed, PaginationPage, CircuitBreakerTripped |
| 4001–4004 | Server | InboundRequestReceived, InboundRequestProcessed, InboundRequestRejected, ConsumerHandlerFailed |
| 5001–5006 | Registry | CpoAdded, CpoUpdated, CpoRemoved, CacheHit, CacheMiss, CacheInvalidated |
| 6001–6004 | Sync | SyncStarted, SyncPageFetched, SyncComplete, SyncFailed |
| 7001–7004 | Health | ProbeSuccess, ProbeFailed, CpoMarkedOffline, CpoRecovered |
| 8001–8003 | Validation | ValidationFailed, VersionMismatch, BodyPathMismatch |

### Logging Options

```csharp
options.Logging.EnableRequestBodyLogging = false;  // Log request bodies
options.Logging.EnableResponseBodyLogging = false;  // Log response bodies
options.Logging.MaxBodyLogLength = 4096;            // Truncate at 4 KB
options.Logging.SanitizeTokensInLogs = true;        // Always true in production
```

{: .warning }
> Raw tokens are **never** logged regardless of settings. The `SanitizeTokensInLogs` option controls whether token hashes are redacted in log output.

---

## Distributed Tracing

DotOcpi creates spans using `System.Diagnostics.ActivitySource`:

```csharp
// Source name: "DotOcpi"
// Consumers opt-in via OpenTelemetry:
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("DotOcpi");  // Enable DotOcpi spans
    });
```

### Span Types

| Span | Description | Tags |
|:-----|:------------|:-----|
| `OCPI {method} {module}` | Outbound HTTP request | `ocpi.direction`, `ocpi.cpo.id`, `ocpi.module`, `http.method`, `ocpi.version`, `ocpi.status_code`, `http.status_code` |
| `OCPI {method} {path}` | Inbound HTTP request | `ocpi.direction`, `ocpi.cpo.id`, `http.method`, `http.route`, `ocpi.version`, `ocpi.status_code`, `http.status_code` |
| `OCPI Registration` | Registration handshake | `ocpi.cpo.id`, `ocpi.version`, `ocpi.status_code`, `http.status_code` |

### Zero-Cost When Disabled

If no listener is attached to the `"DotOcpi"` `ActivitySource`, no spans are created and there is zero performance overhead.

---

## Metrics

DotOcpi emits metrics using `System.Diagnostics.Metrics`:

```csharp
// Meter name: "DotOcpi"
// Consumers opt-in via OpenTelemetry:
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("DotOcpi");  // Enable DotOcpi metrics
    });
```

### Available Metrics

| Metric | Type | Tags | Description |
|:-------|:-----|:-----|:------------|
| `dotocpi.requests.total` | Counter | `direction`, `module`, `version`, `status` | Total OCPI requests |
| `dotocpi.request.duration` | Histogram | `direction`, `module`, `version` | Request duration (seconds) |
| `dotocpi.connections.active` | UpDownCounter | `status` | Active CPO connections |
| `dotocpi.auth.failures` | Counter | `reason` | Authentication failures |

### Usage in Grafana/Prometheus

```promql
# Request rate by module (metric: dotocpi.requests.total)
rate(dotocpi_requests_total{direction="inbound"}[5m])

# Average request duration
histogram_quantile(0.95, rate(dotocpi_request_duration_seconds_bucket[5m]))

# Active connections
dotocpi_connections_active

# Auth failure rate
rate(dotocpi_auth_failures_total[5m])
```

---

## Health Checks

DotOcpi provides ASP.NET Core health checks:

```csharp
builder.Services.AddHealthChecks()
    .AddOcpiRegistryHealthCheck()
    .AddOcpiTokenStoreHealthCheck();

// Per-CPO health check
builder.Services.AddHealthChecks()
    .AddOcpiCpoHealthCheck("DE:CPO");
```

### Health Check Types

| Check | What it verifies |
|:------|:-----------------|
| `OcpiRegistryHealthCheck` | At least one CPO is `Connected` |
| `OcpiTokenStoreHealthCheck` | Token store is reachable and operational |
| `OcpiCpoHealthCheck` | Specific CPO connection is healthy |

### Health Check Responses

```json
{
  "status": "Healthy",
  "results": {
    "ocpi-registry": {
      "status": "Healthy",
      "description": "3 CPOs connected, 0 offline"
    },
    "ocpi-tokens": {
      "status": "Healthy"
    }
  }
}
```

---

## Correlation

Every OCPI request includes tracking headers:

| Header | Purpose | Set By |
|:-------|:--------|:-------|
| `X-Request-ID` | Unique per request | DotOcpi (server generates, client generates) |
| `X-Correlation-ID` | Consistent across request chain | DotOcpi (server echoes, client generates) |

These are available in your handlers via `OcpiRequestContext`:

```csharp
public async Task<OcpiResult> OnLocationPutAsync(
    OcpiRequestContext context, string locationId, object data, CancellationToken ct)
{
    _logger.LogInformation(
        "Processing location {LocationId} (request: {RequestId}, correlation: {CorrelationId})",
        locationId,
        context.RequestId,
        context.CorrelationId);

    return OcpiResult.Success();
}
```
