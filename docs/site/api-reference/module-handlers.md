---
title: Module Handlers
layout: default
parent: API Reference
nav_order: 3
---

# Module Handler Interfaces
{: .no_toc }

Server-side interfaces your application implements to handle CPO pushes.
{: .fs-5 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## ILocationsReceiver

Handles location, EVSE, and connector pushes from CPOs.

```csharp
public interface ILocationsReceiver
{
    Task<OcpiResult> OnLocationPutAsync(
        OcpiRequestContext context, string locationId, object data, CancellationToken ct);

    Task<OcpiResult> OnLocationPatchAsync(
        OcpiRequestContext context, string locationId, JsonElement patch, CancellationToken ct);

    Task<OcpiResult> OnEvsePutAsync(
        OcpiRequestContext context, string locationId, string evseUid,
        object data, CancellationToken ct);

    Task<OcpiResult> OnEvsePatchAsync(
        OcpiRequestContext context, string locationId, string evseUid,
        JsonElement patch, CancellationToken ct);

    Task<OcpiResult> OnConnectorPutAsync(
        OcpiRequestContext context, string locationId, string evseUid,
        string connectorId, object data, CancellationToken ct);

    Task<OcpiResult> OnConnectorPatchAsync(
        OcpiRequestContext context, string locationId, string evseUid,
        string connectorId, JsonElement patch, CancellationToken ct);

    Task<OcpiResult<object>> GetLocationAsync(
        OcpiRequestContext context, string locationId, CancellationToken ct);
}
```

---

## ISessionsReceiver

Handles session pushes from CPOs.

```csharp
public interface ISessionsReceiver
{
    Task<OcpiResult> OnSessionPutAsync(
        OcpiRequestContext context, string sessionId, object data, CancellationToken ct);

    Task<OcpiResult> OnSessionPatchAsync(
        OcpiRequestContext context, string sessionId, JsonElement patch, CancellationToken ct);

    Task<OcpiResult<object>> GetSessionAsync(
        OcpiRequestContext context, string sessionId, CancellationToken ct);
}
```

---

## ICdrsReceiver

Handles CDR pushes from CPOs.

```csharp
public interface ICdrsReceiver
{
    Task<OcpiResult<CdrPostResult>> OnCdrPostAsync(
        OcpiRequestContext context, object data, CancellationToken ct);

    Task<OcpiResult<object>> GetCdrAsync(
        OcpiRequestContext context, string cdrId, CancellationToken ct);
}
```

### CdrPostResult

```csharp
public sealed record CdrPostResult(string CdrId, bool IsNew);
```

- `IsNew: true` &rarr; HTTP 201 Created with Location header
- `IsNew: false` &rarr; HTTP 200 OK (duplicate)

---

## ITariffsReceiver

Handles tariff pushes from CPOs.

```csharp
public interface ITariffsReceiver
{
    Task<OcpiResult> OnTariffPutAsync(
        OcpiRequestContext context, string tariffId, object data, CancellationToken ct);

    Task<OcpiResult> OnTariffPatchAsync(
        OcpiRequestContext context, string tariffId, JsonElement patch, CancellationToken ct);

    Task<OcpiResult> OnTariffDeleteAsync(
        OcpiRequestContext context, string tariffId, CancellationToken ct);

    Task<OcpiResult<object>> GetTariffAsync(
        OcpiRequestContext context, string tariffId, CancellationToken ct);
}
```

{: .note }
> `OnTariffPatchAsync` is only called for OCPI 2.0/2.1.1. In 2.2+, PATCH returns 405 automatically.

---

## ITokensSender

Provides tokens to CPOs via paginated GET.

```csharp
public interface ITokensSender
{
    Task<PaginatedResult<object>> GetTokensAsync(
        OcpiRequestContext context,
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        int offset,
        int limit,
        CancellationToken ct);
}
```

---

## ITokensAuthorizer

Handles real-time token authorization from CPOs.

```csharp
public interface ITokensAuthorizer
{
    Task<OcpiResult<object>> AuthorizeAsync(
        OcpiRequestContext context,
        string tokenUid,
        object? locationReferences,
        CancellationToken ct);
}
```

---

## ICommandsCallback

Receives async command results from CPOs.

```csharp
public interface ICommandsCallback
{
    Task<OcpiResult> OnCommandResultAsync(
        OcpiRequestContext context,
        string correlationId,
        object result,
        CancellationToken ct);
}
```

---

## IChargingProfilesCallback

Receives charging profile results from CPOs (2.2+ only).

```csharp
public interface IChargingProfilesCallback
{
    Task<OcpiResult> OnChargingProfileResultAsync(
        OcpiRequestContext context,
        string correlationId,
        object result,
        CancellationToken ct);

    Task<OcpiResult> OnActiveChargingProfileUpdateAsync(
        OcpiRequestContext context,
        string sessionId,
        object activeProfile,
        CancellationToken ct);
}
```

---

## ICredentialsHandler

Customizes the credentials endpoint behavior. Optional — DotOcpi provides a default implementation.

```csharp
public interface ICredentialsHandler
{
    // Initial registration — Token A auth, no CpoConnection exists yet
    Task<OcpiResult<object>> OnCredentialsPostAsync(
        OcpiRegistrationContext context, object credentials, CancellationToken ct);

    // Credential rotation — Token B auth, established connection
    Task<OcpiResult<object>> OnCredentialsPutAsync(
        OcpiRequestContext context, object credentials, CancellationToken ct);

    // Unregistration — Token B auth, established connection
    Task<OcpiResult> OnCredentialsDeleteAsync(
        OcpiRequestContext context, CancellationToken ct);

    // Retrieve credentials — Token B auth, established connection
    Task<OcpiResult<object>> GetCredentialsAsync(
        OcpiRequestContext context, CancellationToken ct);
}
```

`OnCredentialsPostAsync` receives `OcpiRegistrationContext` because initial registration uses Token A and no `CpoConnection` exists yet. The context provides the validated `TokenEntry` and the OCPI version from the URL path. All other methods receive `OcpiRequestContext` with the full `CpoConnection`.

---

## Handler Data Types

All PUT handlers receive `object data` — the actual type depends on the negotiated OCPI version:

| Version | Location Type | Session Type | CDR Type | Tariff Type |
|:--------|:-------------|:-------------|:---------|:------------|
| 2.0 | `Models.V2_0.Location` | `Models.V2_0.Session` | `Models.V2_0.Cdr` | `Models.V2_0.Tariff` |
| 2.1.1 | `Models.V2_1_1.Location` | `Models.V2_1_1.Session` | `Models.V2_1_1.Cdr` | `Models.V2_1_1.Tariff` |
| 2.2 | `Models.V2_2.Location` | `Models.V2_2.Session` | `Models.V2_2.Cdr` | `Models.V2_2.Tariff` |
| 2.2.1 | `Models.V2_2_1.Location` | `Models.V2_2_1.Session` | `Models.V2_2_1.Cdr` | `Models.V2_2_1.Tariff` |

Use `context.NegotiatedVersion` to determine the exact type if you need version-specific handling.
