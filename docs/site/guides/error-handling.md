---
title: Error Handling
layout: default
parent: Guides
nav_order: 8
---

# Error Handling & Troubleshooting
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Error Channels

Errors surface through three channels:

| Channel | When | How to handle |
|:--------|:-----|:--------------|
| **`OcpiResult<T>`** | Expected OCPI errors (unknown location, invalid token) | Check `IsSuccess`, read `StatusCode` |
| **Exceptions** | Truly exceptional conditions (network failure, config error) | `try/catch` around registration and client calls |
| **Structured Logs** | All operations | Filter by log category and event ID |

---

## OCPI Status Codes

Every OCPI response includes a status code. DotOcpi maps these to `OcpiStatusCode`:

### Success (1xxx)

| Code | Constant | Meaning |
|:-----|:---------|:--------|
| 1000 | `OcpiStatusCode.Success` | Request handled successfully |

### Client Errors (2xxx)

| Code | Constant | Meaning | Common cause |
|:-----|:---------|:--------|:-------------|
| 2000 | `GenericClientError` | Generic client error | Catch-all for unclassified errors |
| 2001 | `InvalidParameters` | Invalid or missing parameters | Validation failure — check the response data for details |
| 2002 | `NotEnoughInformation` | Not enough information | Missing or invalid auth token |
| 2003 | `UnknownLocation` | Unknown location | Location ID not found |
| 2004 | `UnknownToken` | Unknown token (EV driver token) | Token UID not found |

### Server Errors (3xxx)

| Code | Constant | Meaning | Common cause |
|:-----|:---------|:--------|:-------------|
| 3000 | `GenericServerError` | Generic server error | Unhandled exception in handler |
| 3001 | `UnableToUseClientApi` | Unable to use client's API | CPO endpoint unreachable or returning invalid responses |
| 3002 | `UnsupportedVersion` | Unsupported version | No mutual OCPI version between eMSP and CPO |
| 3003 | `NoMatchingEndpoints` | No matching endpoints | CPO doesn't expose the required module endpoint |

### Using Status Codes in Handlers

```csharp
public Task<OcpiResult<object>> GetLocationAsync(
    OcpiRequestContext context, string locationId, CancellationToken ct)
{
    var location = _db.FindLocation(context.CpoId, locationId);

    if (location is null)
        return Task.FromResult(
            OcpiResult<object>.Failure(OcpiStatusCode.UnknownLocation, "Location not found"));

    return Task.FromResult(OcpiResult<object>.Success(location));
}
```

### Checking Client Results

```csharp
var result = await ocpiClient.Locations.GetLocationAsync("DE:CPO", "LOC001");

if (result.IsSuccess)
{
    var location = result.Data;
}
else
{
    _logger.LogWarning("Failed to get location: {Code} {Message}",
        result.StatusCode.Value, result.StatusMessage);

    if (result.StatusCode == OcpiStatusCode.UnknownLocation)
    {
        // Location doesn't exist at the CPO
    }
}
```

---

## Exceptions

DotOcpi exceptions are for truly exceptional conditions — not expected OCPI errors.

| Exception | When it's thrown | What to do |
|:----------|:----------------|:-----------|
| `OcpiConfigurationException` | Invalid configuration (HTTP URL where HTTPS required, missing options) | Fix your `AddDotOcpi()` configuration |
| `OcpiRegistrationException` | Registration handshake fails (no mutual version, CPO rejects credentials) | Check CPO URL, Token A, supported versions |
| `OcpiTransportException` | Network failure (timeout, DNS error, connection refused) | Check network connectivity, CPO endpoint availability |
| `OcpiSerializationException` | JSON parsing error (malformed response, missing required fields) | CPO is sending invalid OCPI JSON — log and investigate |

### Handling Registration Errors

```csharp
try
{
    var result = await registrationClient.RegisterAsync(new RegistrationRequest(
        VersionsUrl: "https://cpo.example.com/ocpi/versions",
        TokenA: tokenA,
        EmspCountryCode: "NL",
        EmspPartyId: "MSP",
        EmspVersionsUrl: "https://my-emsp.com/ocpi/versions",
        EmspBusinessName: "My eMSP"
    ));

    _logger.LogInformation("Registered with {Cpo} on {Version}", result.Connection.ConnectionKey, result.Connection.Version);
}
catch (OcpiRegistrationException ex)
{
    // No mutual version, Token A already consumed, CPO rejected credentials
    _logger.LogError(ex, "Registration failed");
}
catch (OcpiTransportException ex)
{
    // CPO unreachable — check URL, network, firewall
    _logger.LogError(ex, "Cannot reach CPO at {Url}", ex.Message);
}
```

### Exception Middleware

The `OcpiExceptionMiddleware` catches unhandled exceptions in your handlers and returns a safe OCPI response:

```json
{
  "status_code": 3000,
  "status_message": "Internal server error.",
  "timestamp": "2026-03-24T10:00:00Z"
}
```

Stack traces, internal hostnames, and file paths are **never** included in error responses. Details are logged server-side at Error level.

---

## Startup Errors

### Configuration Validation

DotOcpi validates options when they're first resolved. Invalid configuration throws `OptionsValidationException`:

| Error | Cause | Fix |
|:------|:------|:----|
| "At least one supported OCPI version must be configured." | Empty `SupportedVersions` | Add at least one version: `options.SupportedVersions = [OcpiVersion.V2_2_1]` |
| "BaseUrl must use HTTPS. Got: http" | HTTP URL for `BaseUrl` | Use `https://` scheme |
| "HealthMonitoringInterval must be positive." | Zero or negative interval | Set a positive `TimeSpan` |
| "StaleConnectionThreshold must be positive." | Zero or negative threshold | Set a positive `TimeSpan` |

### Service Validation

`DotOcpiStartupValidator` (a hosted service) checks for required services at startup:

| Check | Severity | Message |
|:------|:---------|:--------|
| `ICpoRegistry` not registered | Fatal | Throws — call `AddInMemoryCpoRegistry()` or `AddCpoRegistryStore<T>()` |
| `ITokenStore` not registered | Fatal | Throws — call `AddInMemoryTokenStore()` or `AddTokenStore<T>()` |
| `ILocationsReceiver` not registered | Warning | Logged — endpoints won't be mapped for this module |

### Token A Security Check

{: .warning }
> DotOcpi scans your configuration sources at startup for Token A values. If found, it throws immediately. Token A must **never** be in `appsettings.json` or source control.

```
Token A must NEVER be stored in appsettings.json or source-controlled configuration.
Use dotnet user-secrets for development or a secrets provider for production.
```

Scanned keys: `DotOcpi:TokenA`, `DotOcpi:Token_A`, `DotOcpi:Registration:TokenA`, `Ocpi:TokenA`, `OcpiTokenA`

---

## Authentication Errors

When a CPO sends a request with an invalid token:

| Condition | HTTP | OCPI | Message | Log Event |
|:----------|:-----|:-----|:--------|:----------|
| Missing/malformed `Authorization` header | 401 | 2002 | Missing or malformed Authorization header. | 2002 (AuthMissingToken) |
| Token hash not in store | 401 | 2002 | Invalid or unrecognized token. | 2003 (AuthInvalidToken) |
| Token valid but no CPO in registry | 401 | 2002 | No CPO connection associated with this token. | 2005 (AuthCpoNotFound) |

### Debugging Auth Failures

1. Set `DotOcpi.Auth` log level to `Debug`:
   ```json
   { "Logging": { "LogLevel": { "DotOcpi.Auth": "Debug" } } }
   ```

2. Check logs for the event ID:
   - **2002**: The CPO isn't sending the `Authorization: Token <value>` header, or it's malformed
   - **2003**: The token doesn't match any stored hash — was the registration completed? Did token rotation fail?
   - **2005**: The token is valid but the CPO connection was removed from the registry

---

## Validation Errors

When a CPO sends invalid data, the validation filter returns HTTP 400 with OCPI 2001:

```json
{
  "status_code": 2001,
  "status_message": "Invalid or missing parameters.",
  "timestamp": "2026-03-24T10:00:00Z"
}
```

Validation details are logged server-side (event 8001):

```
Validation failed for Location: Country code is required (Property: Country)
```

---

## Client-Side Errors

When your eMSP calls a CPO and something goes wrong:

### Transport Failures

```csharp
try
{
    await foreach (var loc in ocpiClient.Locations.GetAllLocationsAsync("DE:CPO"))
    {
        // process
    }
}
catch (OcpiTransportException ex)
{
    // Network error — CPO unreachable, timeout, DNS failure
    _logger.LogError(ex, "Failed to pull locations from DE:CPO");
}
```

### CPO Returns Error

The client deserializes the OCPI response and returns it as an `OcpiResult`:

```csharp
var result = await ocpiClient.Locations.GetLocationAsync("DE:CPO", "LOC001");

if (!result.IsSuccess)
{
    // result.StatusCode.Value is the OCPI status (e.g., 3000)
    // result.StatusMessage is the CPO's error message
}
```

### Circuit Breaker

If a CPO consistently fails, the resilience pipeline trips the circuit breaker. Log event 3005 (`CircuitBreakerTripped`) fires:

```
Circuit breaker tripped for CPO DE:CPO: Multiple consecutive failures
```

The circuit reopens automatically after the configured timeout.

---

## Log Event Reference

Use these event IDs to filter and alert on specific conditions:

| Range | Component | Key Events to Monitor |
|:------|:----------|:----------------------|
| 1004 | Registration | Handshake failed — investigate CPO connection |
| 2002–2005 | Auth | Authentication failures — possible token issues |
| 3003 | Client | Request to CPO failed — network or CPO issues |
| 3005 | Client | Circuit breaker tripped — CPO consistently failing |
| 4004 | Server | Your handler threw an exception — fix the bug |
| 6004 | Sync | Pull sync failed — check CPO availability |
| 7003 | Health | CPO marked offline — investigate connection |
| 8001 | Validation | Invalid data from CPO — log for investigation |

### Recommended Log Level Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "DotOcpi": "Information",
      "DotOcpi.Auth": "Warning",
      "DotOcpi.Health": "Warning",
      "DotOcpi.Validation": "Warning"
    }
  }
}
```

Set `DotOcpi.Auth` and `DotOcpi.Registration` to `Debug` when troubleshooting specific issues.

---

## Common Issues

### "No mutual OCPI version" during registration

Your `SupportedVersions` doesn't overlap with the CPO's versions. Check:
1. What versions the CPO supports: call their `/versions` endpoint
2. What versions you configured: `options.SupportedVersions`
3. Add the CPO's version to your supported list if appropriate

### "Token A already consumed"

Token A is single-use. If registration failed partway through, the CPO may have already consumed the Token A. You need a new Token A from the CPO — contact them to reset.

### Handler returns 3000 unexpectedly

Your handler threw an unhandled exception. Check logs for event 4004 (`ConsumerHandlerFailed`). The exception details are logged at Error level but never sent to the CPO.

### CPO goes offline repeatedly

Check event 7002 (`ProbeFailed`) and 7003 (`CpoMarkedOffline`). The health monitor probes the CPO's versions endpoint. If the CPO's URL changed or their TLS certificate expired, probes will fail.

### Pull sync returns zero items

The sync uses `dateFrom` from the last successful sync. If the CPO has no new data since then, zero items is expected. Check `SyncResult.ItemCount` in your `IOcpiSyncHandler.OnSyncCompletedAsync`.

---

<div style="display: flex; justify-content: space-between; margin-top: 2rem;">
  <div>← <a href="/DotOcpi/guides/working-with-data/">Working with Data</a></div>
  <div></div>
</div>
