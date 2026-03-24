# CPO Connection Registry

The CPO Registry is the central component that tracks all connected CPOs, their negotiated versions, endpoints, credentials, and the eMSP identity used for each connection.

## Table of Contents

- [1. Data Model](#1-data-model)
- [2. CPO Identification Strategy](#2-cpo-identification-strategy)
- [3. Lookup Paths](#3-lookup-paths)
- [4. Single-Instance Architecture](#4-single-instance-architecture)
- [5. Multi-Instance Architecture](#5-multi-instance-architecture)
- [6. Connection Lifecycle](#6-connection-lifecycle)
- [7. Concurrency Control](#7-concurrency-control)
- [8. Re-Registration & Idempotency](#8-re-registration--idempotency)
- [9. Health Monitoring](#9-health-monitoring)

---

## 1. Data Model

```mermaid
classDiagram
    class CpoConnection {
        +ConnectionKey: string
        +CpoCountryCode: string
        +CpoPartyId: string
        +EmspCountryCode: string
        +EmspPartyId: string
        +Version: OcpiVersion
        +ModuleEndpoints: IReadOnlyDictionary~string, string~
        +TokenBHash: string
        +Status: ConnectionStatus
        +CpoVersionsUrl: string?
        +EmspVersionsUrl: string?
        +ConcurrencyVersion: long
        +LastHealthCheckAt: DateTimeOffset?
        +UpdatedAt: DateTimeOffset
        +CreatedAt: DateTimeOffset
    }

    class ConnectionStatus {
        <<enumeration>>
        Pending
        Connected
        Offline
        Unregistered
        Suspended
    }

    class OcpiVersion {
        <<enumeration>>
        V2_0
        V2_1_1
        V2_2
        V2_2_1
    }

    CpoConnection --> ConnectionStatus
    CpoConnection --> OcpiVersion
```

### Field Descriptions

| Field | Type | Description |
|---|---|---|
| `ConnectionKey` | string | Primary key. Deterministic composite: `"{CpoCountryCode}:{CpoPartyId}"` |
| `CpoCountryCode` | string | The CPO's `country_code` |
| `CpoPartyId` | string | The CPO's `party_id` |
| `EmspCountryCode` | string | The eMSP `country_code` presented to this CPO (multi-party support) |
| `EmspPartyId` | string | The eMSP `party_id` presented to this CPO (multi-party support) |
| `Version` | OcpiVersion | OCPI version negotiated during registration |
| `ModuleEndpoints` | Dictionary | CPO's module endpoint URLs (keyed by ModuleID) |
| `TokenBHash` | string | SHA-256 hash of Token B (authenticates CPO requests) |
| `Status` | ConnectionStatus | Current connection state |
| `CpoVersionsUrl` | string? | The CPO's OCPI versions endpoint URL |
| `EmspVersionsUrl` | string? | The eMSP's OCPI versions endpoint URL presented to this CPO |
| `ConcurrencyVersion` | long | Optimistic concurrency version stamp |
| `LastHealthCheckAt` | DateTimeOffset? | Last successful health probe |
| `UpdatedAt` | DateTimeOffset | Last registry entry modification |
| `CreatedAt` | DateTimeOffset | Initial registration timestamp |

---

## 2. CPO Identification Strategy

### Primary Key: Deterministic Composite ID

CPOs are identified by the OCPI-standard `(country_code, party_id)` tuple. The registry uses a deterministic composite as the primary key:

```
ConnectionKey = "{CpoCountryCode}:{CpoPartyId}"
// Example: "DE:ABC", "NL:XYZ"
```

**Rationale:** Using the OCPI identity as the key (not a GUID) makes re-registration naturally idempotent — if CPO "DE:ABC" re-registers, the existing entry is updated, not duplicated.

### Secondary Indexes

The registry supports three lookup paths, each optimized for a different access pattern:

```mermaid
graph TD
    subgraph "Three Lookup Paths"
        ById["By Connection Key<br/><code>FindByConnectionKey('DE:ABC')</code><br/>Used by: consumer client calls"]
        ByToken["By Token Hash<br/><code>FindByTokenHash(hash)</code><br/>Used by: inbound auth middleware"]
        ByEmsp["By eMSP Identity<br/><code>FindByEmspIdentity('NL', 'MSP')</code><br/>Used by: multi-party queries"]
    end

    ById --> Registry[(CpoConnection)]
    ByToken --> Registry
    ByEmsp --> Registry
```

| Lookup | Use Case | Key Pattern | Hot Path? |
|---|---|---|---|
| By `ConnectionKey` | Consumer calls `client.Locations.GetAllAsync(cpoId)` | `cpo:{cc}:{pid}` | Medium |
| By token hash | Auth middleware on every inbound request | `cpo:by-token:{sha256}` | **Yes** |
| By eMSP identity | Multi-party queries, find all CPOs for an eMSP | `cpo:by-emsp:{cc}:{pid}` | Low |

### Why Not GUID?

| Approach | Pros | Cons |
|---|---|---|
| GUID | Always unique | Re-registration creates duplicates; not human-readable; requires extra lookup |
| Composite `CC_PID` | Idempotent; human-readable; matches OCPI identity | Must handle party detail changes |

---

## 3. Lookup Paths

### Inbound Request (CPO → eMSP)

```mermaid
graph TD
    Req[Incoming HTTP Request] --> Extract["Extract Authorization: Token header"]
    Extract --> Hash["SHA-256 hash the raw token"]
    Hash --> Lookup["registry.FindByTokenHash(hash)"]
    Lookup --> Found{Found?}
    Found -->|No| Reject["401 + OCPI status 2002"]
    Found -->|Yes| Validate["CryptographicOperations.FixedTimeEquals()<br/>(constant-time comparison of stored vs incoming hash)"]
    Validate -->|Mismatch| Reject
    Validate -->|Match| Context["Build OcpiRequestContext:<br/>- CpoConnection<br/>- NegotiatedVersion<br/>- CpoIdentity<br/>- EmspIdentity"]
    Context --> Handler["Route to version-specific handler"]
```

### Outbound Request (eMSP → CPO)

```mermaid
graph TD
    Consumer["Consumer calls client.Locations.GetAllAsync('DE:ABC')"]
    Consumer --> Resolve["CpoConnectionContextProvider.ResolveAsync('DE:ABC')<br/><i>Cached per CPO in ConcurrentDictionary.<br/>On miss: registry lookup + token fetch.<br/>Invalidated on register/rotate/unregister.</i>"]
    Resolve --> Found{Found?}
    Found -->|No| Error["Throw: CPO not registered"]
    Found -->|Yes| Build["Build HTTP request from cached context:<br/>Authorization: Token {raw_token}<br/>X-Request-ID: {new_guid}<br/>X-Correlation-ID: {new_guid}"]
    Build --> Send["Send via IHttpClientFactory"]
```

---

## 4. Single-Instance Architecture

For single-instance deployments (development, small-scale production):

```mermaid
graph TD
    subgraph "Single Instance"
        App[ASP.NET Core App]
        InMem["InMemoryCpoRegistry<br/>(ConcurrentDictionary)"]
        TokenStore["InMemoryTokenStore<br/>(ConcurrentDictionary)"]
    end

    App --> InMem
    App --> TokenStore
```

### Interface

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

### Persistence

The `InMemoryCpoRegistry` is standalone — it does **not** load from or write to `ICpoRegistryStore`. It is a pure in-memory implementation backed by `ConcurrentDictionary`. All data is lost on restart.

The `ICpoRegistryStore` interface exists for consumers who need persistence across restarts. Consumers implement this interface to bridge their backing store (database, Redis, etc.) with a registry implementation of their choosing:

```csharp
public interface ICpoRegistryStore
{
    Task<IReadOnlyList<CpoConnection>> LoadAllAsync(CancellationToken ct);
    Task SaveAsync(CpoConnection connection, CancellationToken ct);
    Task RemoveAsync(string connectionKey, CancellationToken ct);
}
```

---

## 5. Multi-Instance Architecture

When multiple app instances run behind a load balancer, the in-memory registry diverges across instances. The library provides interfaces and patterns; consumers choose the backing infrastructure.

### Problem

```mermaid
graph TD
    LB[Load Balancer] --> I1[Instance 1]
    LB --> I2[Instance 2]
    LB --> I3[Instance 3]

    I1 --> Mem1["In-Memory Registry<br/>CPO A: connected"]
    I2 --> Mem2["In-Memory Registry<br/>(empty — missed registration)"]
    I3 --> Mem3["In-Memory Registry<br/>(empty — missed registration)"]

    CPO_A[CPO A] -->|"Registers on Instance 1"| I1

    CPO_A -->|"Pushes data, hits Instance 2"| I2
    I2 -->|"Token unknown → 401"| CPO_A
```

### Solution: Distributed Backing Store + Local Cache

```mermaid
graph TD
    subgraph "Instance 1"
        App1[App] --> Local1["Local Cache<br/>(ConcurrentDictionary)"]
        Local1 --> Shared["ICpoRegistryStore<br/>(Redis / SQL / etc.)"]
    end

    subgraph "Instance 2"
        App2[App] --> Local2["Local Cache<br/>(ConcurrentDictionary)"]
        Local2 --> Shared
    end

    subgraph "Instance 3"
        App3[App] --> Local3["Local Cache<br/>(ConcurrentDictionary)"]
        Local3 --> Shared
    end

    Shared -->|"Invalidation events"| Local1
    Shared -->|"Invalidation events"| Local2
    Shared -->|"Invalidation events"| Local3
```

### Cache Invalidation Strategy

When one instance modifies the registry, other instances must be notified:

```mermaid
sequenceDiagram
    participant I1 as Instance 1
    participant Store as Shared Store (Redis)
    participant I2 as Instance 2
    participant I3 as Instance 3

    Note over I1: CPO registers
    I1->>Store: UpsertAsync(cpoConnection)
    I1->>Store: Publish("registry:changed", "upsert:DE_ABC")

    Store-->>I2: Subscription: "upsert:DE_ABC"
    I2->>I2: Evict "DE_ABC" from local cache<br/>(next request will fetch from store)

    Store-->>I3: Subscription: "upsert:DE_ABC"
    I3->>I3: Evict "DE_ABC" from local cache
```

**Local cache TTL**: Even without invalidation events, local cache entries should expire after 60 seconds as a safety net against missed pub/sub messages.

**On reconnect**: Flush the entire local cache to prevent stale data.

### Distributed Locking for Registration

The credentials handshake must happen exactly once. In multi-instance deployments, use distributed locking:

```mermaid
sequenceDiagram
    participant I1 as Instance 1
    participant Lock as Distributed Lock
    participant I2 as Instance 2
    participant CPO as CPO

    Note over I1,I2: Both instances receive registration request

    I1->>Lock: AcquireLock("registration:DE_ABC", ttl: 2min)
    Lock-->>I1: Lock acquired

    I2->>Lock: AcquireLock("registration:DE_ABC", ttl: 2min)
    Lock-->>I2: Lock denied (in use)
    I2-->>I2: Return "registration in progress"

    I1->>CPO: Perform handshake
    CPO-->>I1: Token C

    I1->>Lock: ReleaseLock
```

### Consistency Requirements

| Operation | Consistency | Approach |
|---|---|---|
| Token hash lookup (auth) | Eventual (seconds) | Local cache + TTL + pub/sub invalidation |
| Outbound context resolve | Eventual | CpoConnectionContextProvider cache + invalidation |
| Credentials handshake | **Strong** | Distributed lock |
| Token rotation | **Strong** | Distributed lock + atomic swap |
| Status updates | Eventual | Local cache + TTL |
| Health check tracking | Eventual | Write-through to store, fire-and-forget |

> **Note:** Multi-instance cache invalidation and distributed locking are not yet provided by the library. Consumers running multiple instances should implement their own coordination layer (e.g., Redis pub/sub for cache invalidation, distributed locks for registration).

---

## 6. Connection Lifecycle

```mermaid
stateDiagram-v2
    [*] --> CONNECTED: RegisterAsync completes<br/>(Token B exchanged)

    CONNECTED --> CONNECTED: Token rotation<br/>(PUT /credentials)

    CONNECTED --> OFFLINE: Health probe fails<br/>(consecutive failures exceed threshold)

    OFFLINE --> CONNECTED: Health probe succeeds

    CONNECTED --> [*]: UnregisterAsync<br/>(DELETE /credentials, entry removed)

    OFFLINE --> [*]: UnregisterAsync or admin removal
```

### Status Transitions

| From | To | Trigger |
|---|---|---|
| - | CONNECTED | `RegistrationOrchestrator.RegisterAsync()` completes successfully |
| CONNECTED | CONNECTED | Token rotation via PUT /credentials |
| CONNECTED | OFFLINE | Health probe consecutive failures exceed threshold (default 3) |
| OFFLINE | CONNECTED | Health probe succeeds |
| Any | (removed) | `UnregisterAsync` (DELETE /credentials) removes the entry from the registry |

> **Enum values with no built-in assignment path:**
>
> - **Pending**: The enum value exists, but `RegistrationOrchestrator.RegisterAsync` creates connections with `Status = Connected` directly. There is no intermediate Pending state persisted in the registry during the handshake. Consumers may use this value in custom registration flows.
> - **Unregistered**: The enum value exists, but `UnregisterAsync` removes entries from the registry entirely rather than marking them as Unregistered. Consumers may use this value if they prefer soft-delete semantics.
> - **Suspended**: The enum value exists as an extension point for consumer-driven admin suspension. No built-in component transitions connections to this state.

---

## 7. Concurrency Control

### Optimistic Concurrency

Each `CpoConnection` has a `ConcurrencyVersion` field (monotonically increasing long). On update:

1. Read the connection (get current `Version`)
2. Modify fields
3. Write back with expected `Version`
4. If `Version` has changed since read, the write fails — retry

```mermaid
sequenceDiagram
    participant A as Instance A
    participant Store as Registry Store
    participant B as Instance B

    A->>Store: GetAsync("DE_ABC") → Version=5
    B->>Store: GetAsync("DE_ABC") → Version=5

    A->>Store: TryUpdateAsync(conn with Version=5)<br/>→ Version becomes 6
    Store-->>A: Success (Version=6)

    B->>Store: TryUpdateAsync(conn with Version=5)
    Store-->>B: Failure (version mismatch)

    B->>Store: GetAsync("DE_ABC") → Version=6
    B->>B: Re-apply changes
    B->>Store: TryUpdateAsync(conn with Version=6)
    Store-->>B: Success (Version=7)
```

### Operations Requiring Locks

| Operation | Lock Key | TTL | Reason |
|---|---|---|---|
| Credentials POST (register) | `registration:{cpoId}` | 2 min | Prevent duplicate handshakes |
| Credentials PUT (rotate) | `token-rotation:{cpoId}` | 1 min | Atomic token swap |
| Credentials DELETE | `unregister:{cpoId}` | 30 sec | Clean teardown |

---

## 8. Re-Registration & Idempotency

### Same CPO Re-Registers

When a CPO with the same `(country_code, party_id)` sends a new POST /credentials:

```mermaid
graph TD
    POST["POST /credentials from DE/ABC"] --> Check{"Already registered?<br/>(by party identity)"}
    Check -->|"Yes (status: CONNECTED)"| Reject["405 Method Not Allowed<br/>(spec requirement: use PUT instead)"]
    Check -->|"Yes (status: OFFLINE/SUSPENDED)"| Decision{"Policy decision"}
    Decision -->|"Strict (default)"| Reject
    Decision -->|"Lenient (configurable)"| Allow["Allow re-registration:<br/>1. Clean up old token indexes<br/>2. Perform new handshake<br/>3. Update existing entry (same ID)"]
    Check -->|"No"| Register["Perform registration<br/>Create new entry"]
```

### CPO Changes Party Details

If a CPO updates their credentials via PUT and their `party_id` or `country_code` changes (rare — M&A scenarios):

1. Clean up old secondary indexes (by-party, by-token)
2. Update the connection with new identity
3. The `ConnectionKey` changes since it's derived from party identity
4. Create new entry, remove old entry (atomic if possible)
5. Log the change for audit

---

## 9. Health Monitoring

### Activity Tracking

`LastHealthCheckAt` is updated by `CpoHealthMonitor` on each successful health probe. Inbound auth and outbound client calls do not currently update this field.

### Health Probing

A background service (`CpoHealthMonitor`) periodically checks all eligible connections:

```mermaid
graph TD
    Timer["Every 5 minutes (configurable)"] --> GetAll["Get ALL connections"]
    GetAll --> Filter["Skip: Unregistered and Pending"]
    Filter --> Probe["For each eligible CPO:<br/>GET CpoVersionsUrl"]
    Probe --> Success{Success?}
    Success -->|Yes| Reset["Reset failure counter<br/>Update LastHealthCheckAt<br/>If Offline → restore to Connected"]
    Success -->|No| Increment["Increment failure counter"]
    Increment --> Threshold{"Failures >= threshold?<br/>(default: 3 consecutive)"}
    Threshold -->|Yes| MarkOffline["Mark Status → OFFLINE"]
    Threshold -->|No| Wait["Wait for next cycle"]
```

There is no staleness filter — all connections that are not Unregistered or Pending are checked on every cycle. `DotOcpiOptions.StaleConnectionThreshold` is defined but not yet consumed by the health monitor; it is available for consumers who want to implement their own staleness logic. The failure threshold (default 3 consecutive failures) is configurable. Each cycle logs a summary with total checked, healthy, failed, restored, and marked-offline counts.

> **Note:** The health monitor does not use distributed locking or leader election. Each instance runs its health checks independently. In multi-instance deployments, this means multiple instances may probe the same CPO concurrently, which is safe but redundant.

### Consumer Health Check

The library provides a health check for ASP.NET Core:

```csharp
builder.Services.AddHealthChecks()
    .AddOcpiRegistryHealthCheck(name: "ocpi-registry", tags: ["ready"]);
```

Reports:
- **Healthy**: All CPOs in CONNECTED status
- **Degraded**: Some CPOs OFFLINE or SUSPENDED
- **Unhealthy**: All CPOs OFFLINE or registry unavailable

The health check response includes per-CPO data (status, version, last health check timestamp) in the `data` dictionary, enabling operators to identify exactly which CPOs are healthy and which are degraded.
