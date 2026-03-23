---
title: Client Interfaces
layout: default
parent: API Reference
nav_order: 4
---

# Client Interfaces
{: .no_toc }

Client-side interfaces for pulling data from and sending commands to CPOs.
{: .fs-5 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## IOcpiClient

Facade that provides access to all client operations.

```csharp
public interface IOcpiClient
{
    IRegistrationClient Registration { get; }
    IVersionDiscovery Versions { get; }
    ILocationsClient Locations { get; }
    ISessionsClient Sessions { get; }
    ICdrsClient Cdrs { get; }
    ITariffsClient Tariffs { get; }
    ITokensClient Tokens { get; }
    ICommandsClient Commands { get; }
    IChargingProfilesClient ChargingProfiles { get; }

    void InvalidateConnection(string cpoId);
    void InvalidateAllConnections();
}
```

### Cache Invalidation

The client caches CPO connection context (registry data + auth token) per CPO to eliminate redundant per-call lookups. The cache is automatically invalidated after `RegisterAsync`, `RotateCredentialsAsync`, and `UnregisterAsync`.

If you modify the CPO registry or rotate tokens outside the library's registration flow, call `InvalidateConnection(cpoId)` to force the next outbound call to re-resolve from the registry and token provider. Call `InvalidateAllConnections()` to clear the entire cache.

### Usage

```csharp
public class MyService
{
    private readonly IOcpiClient _ocpi;
    public MyService(IOcpiClient ocpi) => _ocpi = ocpi;

    public async Task DoWork(CancellationToken ct)
    {
        await foreach (var loc in _ocpi.Locations.GetAllLocationsAsync("DE:CPO", ct: ct))
        {
            // ...
        }
    }
}
```

---

## IRegistrationClient

Orchestrates the OCPI credentials handshake.

```csharp
public interface IRegistrationClient
{
    Task<RegistrationResult> RegisterAsync(
        RegistrationRequest request, CancellationToken ct = default);

    Task<RegistrationResult> RotateCredentialsAsync(
        CredentialRotationRequest request, CancellationToken ct = default);

    Task UnregisterAsync(
        UnregisterRequest request, CancellationToken ct = default);
}
```

### Request Types

```csharp
public sealed record RegistrationRequest(
    string VersionsUrl,
    string TokenA,
    string EmspCountryCode,
    string EmspPartyId,
    string EmspVersionsUrl,
    string EmspBusinessName,
    IReadOnlySet<OcpiVersion>? SupportedVersions = null);

public sealed record CredentialRotationRequest(
    string ConnectionKey,
    string CurrentCpoToken,
    string EmspBusinessName);

public sealed record UnregisterRequest(
    string ConnectionKey,
    string CurrentCpoToken);

public sealed record RegistrationResult(
    CpoConnection Connection,
    string CpoToken);
```

---

## IVersionDiscovery

Low-level version and endpoint discovery.

```csharp
public interface IVersionDiscovery
{
    Task<IReadOnlyList<VersionInfo>> GetVersionsAsync(
        string versionsUrl, string token, CancellationToken ct = default);

    Task<VersionDetailInfo> GetVersionDetailAsync(
        string versionDetailUrl, string token, CancellationToken ct = default);
}
```

### Response Types

```csharp
public sealed record VersionInfo(string Version, string Url);
public sealed record VersionDetailInfo(string Version, IReadOnlyList<EndpointInfo> Endpoints);
public sealed record EndpointInfo(string Identifier, string Role, string Url);
```

---

## ILocationsClient

Pull location data from CPOs.

```csharp
public interface ILocationsClient
{
    IAsyncEnumerable<object> GetAllLocationsAsync(
        string cpoId,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        [EnumeratorCancellation] CancellationToken ct = default);

    Task<OcpiResult<object>> GetLocationAsync(
        string cpoId, string locationId, CancellationToken ct = default);
}
```

### Usage

```csharp
// Stream all locations (auto-paginated)
await foreach (var location in client.Locations.GetAllLocationsAsync("DE:CPO"))
{
    Console.WriteLine(location);
}

// Incremental pull
await foreach (var location in client.Locations.GetAllLocationsAsync(
    "DE:CPO", dateFrom: DateTimeOffset.UtcNow.AddHours(-1)))
{
    // Only locations updated in the last hour
}

// Single location
var result = await client.Locations.GetLocationAsync("DE:CPO", "LOC001");
```

---

## ISessionsClient

Pull session data and send charging preferences.

```csharp
public interface ISessionsClient
{
    IAsyncEnumerable<object> GetAllSessionsAsync(
        string cpoId,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        [EnumeratorCancellation] CancellationToken ct = default);

    Task<OcpiResult<object>> PutChargingPreferencesAsync(
        string cpoId, string sessionId, object preferences, CancellationToken ct = default);
}
```

---

## ICdrsClient

Pull CDR data from CPOs.

```csharp
public interface ICdrsClient
{
    IAsyncEnumerable<object> GetAllCdrsAsync(
        string cpoId,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        [EnumeratorCancellation] CancellationToken ct = default);
}
```

---

## ITariffsClient

Pull tariff data from CPOs.

```csharp
public interface ITariffsClient
{
    IAsyncEnumerable<object> GetAllTariffsAsync(
        string cpoId,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        [EnumeratorCancellation] CancellationToken ct = default);
}
```

---

## ITokensClient

Push EV driver tokens to CPOs.

```csharp
public interface ITokensClient
{
    Task<OcpiResult> PushTokenAsync(
        string cpoId, string tokenUid, object token, CancellationToken ct = default);

    Task<OcpiResult> PatchTokenAsync(
        string cpoId, string tokenUid, JsonElement patch, CancellationToken ct = default);
}
```

---

## ICommandsClient

Send commands to CPOs.

```csharp
public interface ICommandsClient
{
    Task<OcpiResult<object>> SendStartSessionAsync(
        string cpoId, object command, CancellationToken ct = default);

    Task<OcpiResult<object>> SendStopSessionAsync(
        string cpoId, object command, CancellationToken ct = default);

    Task<OcpiResult<object>> SendReserveNowAsync(
        string cpoId, object command, CancellationToken ct = default);

    Task<OcpiResult<object>> SendUnlockConnectorAsync(
        string cpoId, object command, CancellationToken ct = default);

    Task<OcpiResult<object>> SendCancelReservationAsync(
        string cpoId, object command, CancellationToken ct = default);
}
```

{: .note }
> `SendCancelReservationAsync` is only available for OCPI 2.2.1 connections.

---

## IChargingProfilesClient

Manage charging profiles (2.2+ only).

```csharp
public interface IChargingProfilesClient
{
    Task<OcpiResult<object>> SetChargingProfileAsync(
        string cpoId, string sessionId, object profile, CancellationToken ct = default);

    Task<OcpiResult<object>> DeleteChargingProfileAsync(
        string cpoId, string sessionId, CancellationToken ct = default);

    Task<OcpiResult<object>> GetActiveChargingProfileAsync(
        string cpoId, string sessionId, CancellationToken ct = default);
}
```

---

## ICredentialsClient

Low-level credentials operations. Used internally by `IRegistrationClient`.

```csharp
public interface ICredentialsClient
{
    Task<CredentialsResponse> PostCredentialsAsync(
        string url, string token, OcpiVersion version, object credentials,
        CancellationToken ct = default);

    Task<CredentialsResponse> PutCredentialsAsync(
        string url, string token, OcpiVersion version, object credentials,
        CancellationToken ct = default);

    Task DeleteCredentialsAsync(
        string url, string token, CancellationToken ct = default);
}
```

---

## Pagination Behavior

All `GetAll*Async` methods return `IAsyncEnumerable<T>` and handle pagination automatically:

1. Send initial GET request
2. Parse the `Link` header for the next page URL
3. Yield each item from the current page
4. Follow the next page link
5. Repeat until no more pages

Items are yielded one at a time — no buffering of entire result sets. This is memory-efficient for large datasets.

```csharp
// This works even if the CPO has millions of locations across thousands of pages
await foreach (var location in client.Locations.GetAllLocationsAsync("DE:CPO"))
{
    // Each location is yielded as it's received
    await ProcessAsync(location);
}
```

---

## IOutboundTokenProvider

Consumers must register an implementation of `IOutboundTokenProvider` to supply raw Token B values for authenticating outbound requests to CPOs. The client's internal connection context cache calls this once per CPO (until the cache is invalidated), so implementations can safely call into slow backing stores (Key Vault, database) without impacting per-call latency.

```csharp
public interface IOutboundTokenProvider
{
    ValueTask<string> GetTokenAsync(string cpoId, CancellationToken cancellationToken = default);
}
```

### Usage

```csharp
public class KeyVaultTokenProvider : IOutboundTokenProvider
{
    private readonly SecretClient _secretClient;

    public KeyVaultTokenProvider(SecretClient secretClient) => _secretClient = secretClient;

    public async ValueTask<string> GetTokenAsync(string cpoId, CancellationToken cancellationToken = default)
    {
        var secret = await _secretClient.GetSecretAsync($"ocpi-token-{cpoId}", cancellationToken: cancellationToken);
        return secret.Value.Value;
    }
}

// Registration
builder.Services.AddSingleton<IOutboundTokenProvider, KeyVaultTokenProvider>();
```
