# DotOcpi Architecture

## Table of Contents

- [1. System Context](#1-system-context)
- [2. Package Architecture](#2-package-architecture)
- [3. OCPI Version Strategy](#3-ocpi-version-strategy)
- [4. Server-Side Pipeline (Receiving from CPOs)](#4-server-side-pipeline-receiving-from-cpos)
- [5. Client-Side Pipeline (Calling CPOs)](#5-client-side-pipeline-calling-cpos)
- [5.1 Pull Synchronization](#51-pull-synchronization)
- [6. Credentials & Registration](#6-credentials--registration)
- [7. Module Architecture](#7-module-architecture)
- [8. Multi-Party Support](#8-multi-party-support)
- [9. Token Management](#9-token-management)
- [10. CPO Connection Registry](#10-cpo-connection-registry)
- [11. Error Handling](#11-error-handling)
- [12. Serialization](#12-serialization)
- [13. Pagination](#13-pagination)
- [14. Observability](#14-observability)
- [15. Project Structure](#15-project-structure)
- [16. Testing Architecture](#16-testing-architecture)

### Related Documents

| Document | Description |
|---|---|
| [Module: Locations](modules/locations.md) | Location/EVSE/Connector models per version, receiver endpoints |
| [Module: Sessions](modules/sessions.md) | Session models, CdrToken, ChargingPreferences |
| [Module: CDRs](modules/cdrs.md) | CDR models, CdrLocation, SignedData, cost breakdown |
| [Module: Tariffs](modules/tariffs.md) | Tariff models, TariffElement, PriceComponent |
| [Module: Tokens](modules/tokens.md) | Token models, real-time authorization flow |
| [Module: Commands](modules/commands.md) | Command types, async callback flow |
| [Module: Charging Profiles](modules/charging-profiles.md) | Smart charging (2.2+ only) |
| [Module: Credentials](modules/credentials.md) | Credentials object, version discovery |
| [Module: Common Types](modules/common-types.md) | Shared types, status codes, primitives |
| [CPO Registry](cpo-registry.md) | Registry design, multi-instance, identification, health monitoring |
| [Strategies](strategies.md) | Validation, .NET compat, minimal API, retry, resilience, observability, **logging**, PATCH handling, type conventions, exceptions, testing |
| [Performance](performance.md) | Source-gen JSON, memory optimization, async patterns, hot-path design, benchmarks |
| [Testing](testing.md) | Unit/integration strategy, multi-version test matrix, security tests, CI pipeline, test data |
| [Implementation Guide](implementation-guide.md) | 15-phase step-by-step build order with dependencies, acceptance criteria, and code structure |

---

## 1. System Context

DotOcpi operates as the eMSP side of the OCPI protocol. It communicates with one or more CPOs, each potentially using a different OCPI version and receiving a different eMSP identity.

```mermaid
C4Context
    title DotOcpi System Context

    Person(driver, "EV Driver", "Uses eMSP app to find chargers, start/stop sessions")

    System(emsp, "eMSP Application", "e-Mobility Service Provider platform")

    System_Ext(cpo_a, "CPO A", "Charge Point Operator (OCPI 2.2.1)")
    System_Ext(cpo_b, "CPO B", "Charge Point Operator (OCPI 2.1.1)")
    System_Ext(cpo_c, "CPO C", "Charge Point Operator (OCPI 2.0)")

    Rel(driver, emsp, "Finds chargers, starts sessions")
    BiRel(emsp, cpo_a, "OCPI 2.2.1 (HTTPS)")
    BiRel(emsp, cpo_b, "OCPI 2.1.1 (HTTPS)")
    BiRel(emsp, cpo_c, "OCPI 2.0 (HTTPS)")
```

### eMSP Dual Role

The eMSP acts as both **client** and **server** in OCPI:

```mermaid
graph LR
    subgraph "eMSP as Client (outbound)"
        A[Pull Locations from CPO]
        B[Push Tokens to CPO]
        C[Send Commands to CPO]
        D[Send ChargingProfiles to CPO]
        E[Discover Versions]
        F[POST/PUT Credentials]
    end

    subgraph "eMSP as Server (inbound)"
        G[Receive Location pushes]
        H[Receive Session pushes]
        I[Receive CDR pushes]
        J[Receive Tariff pushes]
        K[Handle authorize requests]
        L[Serve Versions endpoint]
        M[Handle Credentials requests]
        N[Serve Token list for CPO pull]
        O[Receive Command callbacks]
    end
```

---

## 2. Package Architecture

### Package Dependency Graph

```mermaid
graph TD
    subgraph "NuGet Packages"
        Core["DotOcpi<br/><i>Core models, interfaces,<br/>result types, version negotiation</i>"]
        Client["DotOcpi.Client<br/><i>HttpClient-based OCPI client<br/>for calling CPO endpoints</i>"]
        Server["DotOcpi.AspNetCore<br/><i>ASP.NET Core middleware,<br/>endpoint routing, auth pipeline</i>"]
        Testing["DotOcpi.Simulator<br/><i>In-memory test CPO server</i>"]
    end

    Client --> Core
    Server --> Core
    Testing --> Core
    Testing -.->|test dependency| Server

    subgraph "Microsoft.Extensions (no hard dependency on ASP.NET Core)"
        DI["M.E.DependencyInjection"]
        Logging["M.E.Logging"]
        Http["M.E.Http"]
        Options["M.E.Options"]
    end

    Core --> DI
    Core --> Logging
    Core --> Options
    Client --> Http

    subgraph "ASP.NET Core (only in Server package)"
        AspNet["Microsoft.AspNetCore.*"]
    end

    Server --> AspNet
```

### Package Responsibilities

| Package | Depends On | Contains |
|---|---|---|
| **DotOcpi** | `M.E.DependencyInjection`, `M.E.Logging`, `M.E.Options`, `System.Text.Json` | Version-specific models, `OcpiResult<T>`, `ITokenStore`, `ICpoRegistry`, `IModuleHandler` interfaces, version negotiation, OCPI status codes, JSON converters, `ActivitySource` |
| **DotOcpi.Client** | `DotOcpi`, `M.E.Http` | `IOcpiClient`, typed HTTP clients per module, pagination handling, request/response pipeline, `X-Request-ID`/`X-Correlation-ID` generation |
| **DotOcpi.AspNetCore** | `DotOcpi`, `Microsoft.AspNetCore.*` | OCPI auth middleware, endpoint routing, version/credentials endpoints, module receiver endpoints, request ID middleware |
| **DotOcpi.Simulator** | `DotOcpi`, `Microsoft.AspNetCore.Mvc.Testing` | `OcpiCpoSimulator`, configurable module responses, handshake simulation, failure injection |

---

## 3. OCPI Version Strategy

### Design: Separate Models, Shared Infrastructure

OCPI model shapes change substantially between versions. The library uses **completely separate model types per version** with a **strategy pattern** to select the correct handler at runtime.

```mermaid
graph TD
    subgraph "Shared Infrastructure"
        VP[Version Pipeline]
        Auth[Auth Middleware]
        TM[Token Management]
        Reg[CPO Registry]
        Result["OcpiResult&lt;T&gt;"]
        Pagination[Pagination Handler]
        Headers[Request ID / Correlation ID]
    end

    subgraph "DotOcpi.Models.V2_0"
        M20_Loc[Location]
        M20_Sess[Session]
        M20_Cdr[Cdr]
        M20_Tok[Token]
        M20_Tar[Tariff]
        M20_Cred[Credentials]
    end

    subgraph "DotOcpi.Models.V2_1_1"
        M211_Loc[Location]
        M211_Sess[Session]
        M211_Cdr[Cdr]
        M211_Tok[Token]
        M211_Tar[Tariff]
        M211_Cmd[Command]
        M211_Cred[Credentials]
    end

    subgraph "DotOcpi.Models.V2_2_1"
        M221_Loc[Location]
        M221_Sess[Session]
        M221_Cdr[Cdr]
        M221_Tok[Token]
        M221_Tar[Tariff]
        M221_Cmd[Command]
        M221_CP[ChargingProfile]
        M221_Cred["Credentials<br/>(roles array)"]
    end

    VP --> M20_Loc & M211_Loc & M221_Loc
```

### Version Negotiation Flow

```mermaid
sequenceDiagram
    participant eMSP as eMSP (DotOcpi)
    participant CPO as CPO

    eMSP->>CPO: GET /ocpi/versions
    CPO-->>eMSP: [{version: "2.2.1", url: "..."}, {version: "2.1.1", url: "..."}]

    Note over eMSP: Select highest mutually supported version

    eMSP->>CPO: GET /ocpi/versions/2.2.1/
    CPO-->>eMSP: {version: "2.2.1", endpoints: [{identifier: "locations", role: "SENDER", url: "..."},...]}

    Note over eMSP: Store endpoints, select version-specific handlers
```

### Version Handler Resolution

```mermaid
graph TD
    Request[Incoming Request / Outbound Call] --> Registry[CPO Registry Lookup]
    Registry --> Version{Negotiated Version?}
    Version -->|2.0| V20[V2_0 Module Handlers]
    Version -->|2.1.1| V211[V2_1_1 Module Handlers]
    Version -->|2.2| V22[V2_2 Module Handlers]
    Version -->|2.2.1| V221[V2_2_1 Module Handlers]

    V20 --> Serialize20["Serialize with V2_0 JsonContext"]
    V211 --> Serialize211["Serialize with V2_1_1 JsonContext"]
    V22 --> Serialize22["Serialize with V2_2 JsonContext"]
    V221 --> Serialize221["Serialize with V2_2_1 JsonContext"]
```

### Key Version Differences That Drive Separation

| Aspect | 2.0 / 2.1.1 | 2.2 / 2.2.1 |
|---|---|---|
| Receiver URL pattern | `/{object_id}` | `/{country_code}/{party_id}/{object_id}` |
| Credentials object | Flat: `party_id`, `country_code`, `business_details` at top level | `roles` array of `CredentialsRole` |
| Token model | `auth_id`, `TokenType {RFID, OTHER}` | `contract_id`, `TokenType {RFID, APP_USER, AD_HOC_USER, OTHER, EMAID}` |
| Session model | Embedded `Location` object | Separate `location_id`, `evse_uid`, `connector_id` |
| CDR model | Full `Location` object, `total_cost` as number | `CdrLocation`, `total_cost` as `Price` object, cost breakdown fields |
| Connector | `voltage`, `amperage`, `tariff_id` (singular) | `max_voltage`, `max_amperage`, `tariff_ids` (array) |
| Modules | No Commands in 2.0 | Commands, ChargingProfiles, HubClientInfo |

---

## 4. Server-Side Pipeline (Receiving from CPOs)

### Request Pipeline

```mermaid
graph TD
    subgraph "ASP.NET Core Pipeline"
        Req[Incoming HTTP Request] --> ReqId[Request ID Middleware<br/><i>Extract/generate X-Request-ID<br/>and X-Correlation-ID</i>]
        ReqId --> Auth[OCPI Auth Middleware<br/><i>Extract Authorization: Token header<br/>Constant-time hash comparison</i>]
        Auth -->|Invalid| Reject["401 + OCPI 2002<br/>(Not enough information)"]
        Auth -->|Valid| Resolve[CPO Resolution<br/><i>Look up CPO in registry<br/>by token hash</i>]
        Resolve --> Version[Version Resolution<br/><i>Determine negotiated version<br/>for this CPO</i>]
        Version --> Route[Endpoint Routing<br/><i>Route to correct<br/>module handler</i>]
        Route --> Handler[Version-Specific Handler<br/><i>Deserialize, validate,<br/>invoke consumer callback</i>]
        Handler --> Response[OCPI Response Envelope<br/><i>Wrap in standard format<br/>with status_code, timestamp</i>]
        Response --> RespHeaders[Response Headers<br/><i>Echo X-Request-ID<br/>Set X-Correlation-ID</i>]
    end
```

### Server Endpoint Registration

```mermaid
graph LR
    subgraph "Version Endpoints (always registered)"
        V1["GET /ocpi/versions"]
        V2["GET /ocpi/versions/{id}/"]
    end

    subgraph "Credentials (always registered)"
        C1["GET /ocpi/emsp/{ver}/credentials"]
        C2["POST /ocpi/emsp/{ver}/credentials"]
        C3["PUT /ocpi/emsp/{ver}/credentials"]
        C4["DELETE /ocpi/emsp/{ver}/credentials"]
    end

    subgraph "Receiver Modules (eMSP receives from CPO)"
        L1["GET /ocpi/emsp/{ver}/locations/{...}"]
        L2["PUT /ocpi/emsp/{ver}/locations/{...}"]
        L3["PATCH /ocpi/emsp/{ver}/locations/{...}"]
        S1["PUT /ocpi/emsp/{ver}/sessions/{...}"]
        S2["PATCH /ocpi/emsp/{ver}/sessions/{...}"]
        CDR1["POST /ocpi/emsp/{ver}/cdrs"]
        CDR2["GET /ocpi/emsp/{ver}/cdrs"]
        T1["PUT /ocpi/emsp/{ver}/tariffs/{...}"]
        T2["DELETE /ocpi/emsp/{ver}/tariffs/{...}"]
    end

    subgraph "Sender Modules (CPO pulls from eMSP)"
        TK1["GET /ocpi/emsp/{ver}/tokens"]
        TK2["POST /ocpi/emsp/{ver}/tokens/{uid}/authorize"]
    end

    subgraph "Callback Endpoints"
        CMD1["POST /ocpi/emsp/{ver}/commands/{uid}"]
        CP1["POST /ocpi/emsp/{ver}/chargingprofiles/{id}"]
    end
```

### Consumer Integration Point

The consumer implements interfaces to handle incoming OCPI data. The library handles protocol concerns; the consumer handles business logic:

```mermaid
graph TD
    subgraph "DotOcpi (library)"
        MW[Middleware + Routing]
        Deser[Deserialization]
        Val[Validation]
    end

    subgraph "Consumer implements"
        ILoc["ILocationsReceiver<br/>OnLocationPut()<br/>OnLocationPatch()"]
        ISess["ISessionsReceiver<br/>OnSessionPut()<br/>OnSessionPatch()"]
        ICdr["ICdrsReceiver<br/>OnCdrPost()"]
        ITar["ITariffsReceiver<br/>OnTariffPut()<br/>OnTariffDelete()"]
        ITok["ITokensAuthorizer<br/>OnAuthorize()"]
    end

    MW --> Deser --> Val --> ILoc & ISess & ICdr & ITar & ITok
```

### OcpiRequestContext

Every consumer handler receives an `OcpiRequestContext` — a read-only object carrying all protocol metadata for the current request. This is the primary way consumers access connection details without coupling to the auth/routing pipeline.

```mermaid
classDiagram
    class OcpiRequestContext {
        +RequestId: string
        +CorrelationId: string
        +CpoId: string
        +CpoIdentity: PartyIdentity
        +EmspIdentity: PartyIdentity
        +NegotiatedVersion: OcpiVersion
        +Connection: CpoConnection
        +ModuleId: string
        +HttpContext: HttpContext
        +IsFieldNotAvailable(value) bool
    }
```

| Property | Description |
|---|---|
| `RequestId` | Value of `X-Request-ID` header (generated if missing) |
| `CorrelationId` | Value of `X-Correlation-ID` header (generated if missing) |
| `CpoId` | Deterministic composite ID (`"DE_ABC"`) |
| `CpoIdentity` | CPO's `country_code` + `party_id` |
| `EmspIdentity` | eMSP identity used for this CPO connection |
| `NegotiatedVersion` | OCPI version negotiated with this CPO |
| `Connection` | Full `CpoConnection` record (endpoints, status, etc.) |
| `ModuleId` | Module being invoked (`"locations"`, `"sessions"`, etc.) |
| `HttpContext` | Underlying ASP.NET Core `HttpContext` (escape hatch) |
| `IsFieldNotAvailable()` | Helper to detect the `#NA` sentinel value |

The context is created by the auth middleware, stored in `HttpContext.Items`, and retrieved via `httpContext.GetOcpiContext()`.

---

## 5. Client-Side Pipeline (Calling CPOs)

### Outbound Request Pipeline

```mermaid
graph TD
    subgraph "Consumer Code"
        Call["client.Locations.GetAllAsync(cpoId)"]
    end

    subgraph "DotOcpi.Client Pipeline"
        Call --> Resolve[CpoConnectionContextProvider Resolve<br/><i>Cached per CPO: connection + token.<br/>Eliminates per-call registry/token lookups.</i>]
        Resolve --> Build[Request Builder<br/><i>Set Authorization header<br/>Generate X-Request-ID<br/>Set X-Correlation-ID</i>]
        Build --> Serialize[Serialize Body<br/><i>Version-specific JsonContext</i>]
        Serialize --> Send[HttpClient Send<br/><i>Via IHttpClientFactory<br/>with resilience pipeline</i>]
        Send --> Receive[Response Handler<br/><i>Check HTTP status<br/>Deserialize OCPI envelope</i>]
        Receive --> Extract["Extract OcpiResult&lt;T&gt;<br/><i>Map status_code to result</i>"]
    end

    subgraph "Pagination (for list endpoints)"
        Extract --> HasNext{Link header<br/>present?}
        HasNext -->|Yes| NextPage[Follow Link URL]
        NextPage --> Send
        HasNext -->|No| Done[Return aggregated result]
    end
```

### Client Interface Hierarchy

```mermaid
classDiagram
    class IOcpiClient {
        +Registration IRegistrationClient
        +Versions IVersionsClient
        +Locations ILocationsClient
        +Sessions ISessionsClient
        +Cdrs ICdrsClient
        +Tariffs ITariffsClient
        +Tokens ITokensClient
        +Commands ICommandsClient
        +ChargingProfiles IChargingProfilesClient
    }

    class ILocationsClient {
        +GetAllLocationsAsync(cpoId, dateFrom?, ct) IAsyncEnumerable~object~
        +GetLocationAsync(cpoId, locationId, ct) OcpiResult~object~
        +GetEvseAsync(cpoId, locationId, evseUid, ct) OcpiResult~object~
        +GetConnectorAsync(cpoId, locationId, evseUid, connectorId, ct) OcpiResult~object~
    }

    class ITokensClient {
        +PushTokenAsync(cpoId, token, ct) OcpiResult
        +PatchTokenAsync(cpoId, tokenUid, patch, ct) OcpiResult
    }

    class ICommandsClient {
        +SendStartSessionAsync(cpoId, command, ct) OcpiResult~CommandResponse~
        +SendStopSessionAsync(cpoId, command, ct) OcpiResult~CommandResponse~
        +SendReserveNowAsync(cpoId, command, ct) OcpiResult~CommandResponse~
        +SendUnlockConnectorAsync(cpoId, command, ct) OcpiResult~CommandResponse~
        +SendCancelReservationAsync(cpoId, command, ct) OcpiResult~CommandResponse~
    }

    IOcpiClient --> ILocationsClient
    IOcpiClient --> ITokensClient
    IOcpiClient --> ICommandsClient
```

---

## 5.1 Pull Synchronization

The client package includes a pull sync infrastructure that periodically fetches data from CPO endpoints and delivers it to consumers via a callback interface.

### Data Flow

```
OcpiPullSyncBackgroundService (PeriodicTimer, 30s tick)
  │
  ├── For each Connected CPO in ICpoRegistry:
  │     ├── Resolve enabled modules (PullSyncOptionsResolver)
  │     ├── Check if interval has elapsed since last sync (ISyncStateStore)
  │     └── Call IOcpiSyncService.SyncModuleFromCpoAsync()
  │
  └── OcpiSyncService.SyncModuleCoreAsync()
        ├── Resolve CpoConnectionContext (cached token + registry data)
        ├── Build initial HTTP request (PullClientHelper)
        ├── Stream pages (PaginationHandler.StreamPagesAsync)
        │     ├── Parse OCPI response → OcpiPageResult
        │     ├── Follow Link headers for next page
        │     └── Stop after MaxPages (10,000) or no NextLink
        ├── For each page with items:
        │     └── IOcpiSyncHandler.OnPageReceivedAsync(context, items)
        ├── IOcpiSyncHandler.OnSyncCompletedAsync(context, result)
        └── ISyncStateStore.SetLastSyncAsync() (only on success)
```

### Configuration Hierarchy

`PullSyncOptions` supports four levels of configuration, resolved in priority order:

1. **CPO + module specific** — `CpoOverrides["DE:ALL"].ModuleOverrides["locations"].Interval`
2. **CPO default** — `CpoOverrides["DE:ALL"].DefaultInterval`
3. **Module-level** — `ModuleOverrides["locations"].Interval`
4. **Global default** — `DefaultInterval` (1 hour)

Each CPO can also override which modules are enabled via `CpoOverrides[cpoId].EnabledModules`.

### Key Design Decisions

- **Page-level delivery**: Items are delivered to `IOcpiSyncHandler` in page-sized batches matching OCPI pagination boundaries, enabling efficient bulk persistence.
- **Handler is optional**: `IOcpiSyncHandler` is resolved via `GetService<T>()` (nullable). Without a handler, syncs still run and update timestamps — useful for manual invocation via `IOcpiSyncService`.
- **Timestamp after handler**: `ISyncStateStore` is only updated after `OnSyncCompletedAsync` succeeds. If a handler throws or the sync fails, the next run re-fetches from the same point.
- **Deterministic jitter**: `OcpiPullSyncBackgroundService` uses `HashCode.Combine(cpoId, moduleId)` for stable per-pair jitter, preventing thundering herd without timer drift.
- **Startup validation**: `PullSyncOptionsValidator` (IValidateOptions) rejects zero/negative intervals, negative jitter, and invalid module names at startup.

---

## 6. Credentials & Registration

### Full Handshake Sequence

```mermaid
sequenceDiagram
    participant Admin as Admin / Portal
    participant eMSP as eMSP (DotOcpi)
    participant Store as ITokenStore
    participant Registry as ICpoRegistry
    participant CPO as CPO

    Note over Admin,CPO: Step 0: Out-of-band setup
    Admin->>eMSP: Configure CPO with Token A + versions URL
    eMSP->>Store: Store Token A (temporary)

    Note over eMSP,CPO: Step 1: Version discovery
    eMSP->>CPO: GET /ocpi/versions<br/>Authorization: Token {TOKEN_A}
    CPO-->>eMSP: [{version: "2.2.1", url: "..."}, ...]

    Note over eMSP: Select highest mutually supported version
    eMSP->>CPO: GET /ocpi/versions/2.2.1/
    CPO-->>eMSP: {endpoints: [...]}

    Note over eMSP,CPO: Step 2: Registration
    eMSP->>eMSP: Generate TOKEN_B (CSPRNG, 64 bytes)
    eMSP->>CPO: POST /ocpi/2.2.1/credentials<br/>Authorization: Token {TOKEN_A}<br/>Body: {token: TOKEN_B, url: emsp_versions_url, roles: [...]}

    Note over CPO: CPO validates by calling eMSP
    CPO->>eMSP: GET /ocpi/versions<br/>Authorization: Token {TOKEN_B}
    eMSP-->>CPO: [supported versions]
    CPO->>eMSP: GET /ocpi/versions/2.2.1/
    eMSP-->>CPO: {endpoints: [...]}

    Note over CPO: CPO generates TOKEN_C
    CPO-->>eMSP: 200 OK<br/>{token: TOKEN_C, url: cpo_versions_url, roles: [...]}

    Note over eMSP: Step 3: Finalize
    eMSP->>Store: Store TOKEN_C hash (for eMSP→CPO calls)
    eMSP->>Store: Store TOKEN_B hash (for authenticating CPO→eMSP calls)
    eMSP->>Store: Delete TOKEN_A
    eMSP->>Registry: Store CPO entry (version, endpoints, party info, status: CONNECTED)
```

### Token Lifecycle State Machine

```mermaid
stateDiagram-v2
    [*] --> TokenA_Stored: Admin configures Token A

    TokenA_Stored --> Handshake_In_Progress: Registration initiated
    Handshake_In_Progress --> TokenB_Generated: eMSP generates Token B

    TokenB_Generated --> Registered: CPO responds with Token C
    Registered --> TokenA_Discarded: Token A deleted

    TokenA_Discarded --> Active: Connection active<br/>(Token B for inbound auth,<br/>Token C for outbound calls)

    Active --> Credential_Update: PUT credentials<br/>(rotate tokens)
    Credential_Update --> Active: New Token B/C pair

    Active --> Unregistered: DELETE credentials
    Unregistered --> [*]

    Handshake_In_Progress --> Failed: CPO rejects or timeout
    Failed --> [*]
```

### Credential Update (Token Rotation)

```mermaid
sequenceDiagram
    participant eMSP as eMSP (DotOcpi)
    participant CPO as CPO

    Note over eMSP: Generate new TOKEN_B'
    eMSP->>CPO: PUT /ocpi/2.2.1/credentials<br/>Authorization: Token {TOKEN_C}<br/>Body: {token: TOKEN_B', url: ..., roles: [...]}

    Note over CPO: Generate new TOKEN_C'
    CPO-->>eMSP: 200 OK<br/>{token: TOKEN_C', ...}

    Note over eMSP: Replace TOKEN_B→TOKEN_B',<br/>TOKEN_C→TOKEN_C'
```

### Registration Client API

Consumers initiate CPO registration via the `IOcpiClient.Registration` interface:

```csharp
public interface IRegistrationClient
{
    /// <summary>
    /// Full automated handshake: discover versions → negotiate → POST credentials → store.
    /// </summary>
    Task<OcpiResult<CpoConnection>> RegisterAsync(
        Uri cpoVersionsUrl,
        string tokenA,
        PartyIdentity? emspIdentity = null,    // null = use default from options
        OcpiVersion? preferredVersion = null,   // null = negotiate highest mutual
        CancellationToken ct = default);

    /// <summary>
    /// Rotate tokens with an already-registered CPO.
    /// </summary>
    Task<OcpiResult<CpoConnection>> RotateCredentialsAsync(
        string cpoId,
        CancellationToken ct = default);

    /// <summary>
    /// Unregister from a CPO (DELETE /credentials).
    /// </summary>
    Task<OcpiResult> UnregisterAsync(
        string cpoId,
        CancellationToken ct = default);
}

// Consumer usage:
var result = await ocpiClient.Registration.RegisterAsync(
    new Uri("https://cpo.example.com/ocpi/versions"),
    tokenA: "pre-shared-secret-from-cpo",
    emspIdentity: new PartyIdentity("DE", "ABC"));

if (result.IsSuccess)
{
    var conn = result.Data;
    Console.WriteLine($"Registered with {conn.CpoIdentity}, version {conn.NegotiatedVersion}");
}
```

For custom flows, the building blocks are available individually:

```csharp
// Manual step-by-step registration
var versions = await ocpiClient.Versions.GetAllAsync(cpoVersionsUrl, tokenA, ct);
var detail = await ocpiClient.Versions.GetDetailAsync(cpoVersionsUrl, selectedVersion, tokenA, ct);
var credentials = await ocpiClient.Credentials.PostAsync(cpoCredentialsUrl, tokenA, myCredentials, ct);
```

---

## 7. Module Architecture

### Handler Interface Pattern

Each module defines a receiver or sender interface. Version-specific implementations handle the model differences. The consumer implements the version-agnostic callback interfaces.

```mermaid
classDiagram
    class IModuleHandler {
        <<interface>>
        +ModuleId: string
        +SupportedVersions: OcpiVersion[]
    }

    class ILocationsReceiverHandler {
        <<interface>>
        +HandlePutAsync(request) OcpiResult
        +HandlePatchAsync(request) OcpiResult
        +HandleGetAsync(request) OcpiResult~object~
    }

    class LocationsReceiverHandler_V2_1_1 {
        -ILocationsReceiver _consumer
        +HandlePutAsync(request) OcpiResult
        +HandlePatchAsync(request) OcpiResult
        +HandleGetAsync(request) OcpiResult~object~
    }

    class LocationsReceiverHandler_V2_2_1 {
        -ILocationsReceiver _consumer
        +HandlePutAsync(request) OcpiResult
        +HandlePatchAsync(request) OcpiResult
        +HandleGetAsync(request) OcpiResult~object~
    }

    class ILocationsReceiver {
        <<interface - consumer implements>>
        +OnLocationPutAsync(location, cpoId, ct) Task
        +OnLocationPatchAsync(locationId, patch, cpoId, ct) Task
        +OnLocationGetAsync(locationId, cpoId, ct) Task~Location?~
    }

    IModuleHandler <|-- ILocationsReceiverHandler
    ILocationsReceiverHandler <|.. LocationsReceiverHandler_V2_1_1
    ILocationsReceiverHandler <|.. LocationsReceiverHandler_V2_2_1
    LocationsReceiverHandler_V2_1_1 --> ILocationsReceiver
    LocationsReceiverHandler_V2_2_1 --> ILocationsReceiver
```

### Version-Dispatched Consumer Models

The consumer interface receives version-specific models as `object`. The version-specific handler (selected by the strategy pattern) deserializes the request into the correct model type and passes it to the consumer. The consumer uses `OcpiRequestContext.NegotiatedVersion` and pattern matching to handle version differences.

```mermaid
graph TD
    subgraph "Library internals"
        Request["PUT /locations/..."] --> Handler["LocationsReceiverHandler_V2_2_1"]
        Handler --> Deser["Deserialize as V2_2_1.Location"]
        Deser --> Call["consumer.OnLocationPutAsync(context, locationId, location)"]
    end

    subgraph "Consumer code"
        Call --> Match{"Pattern match on model type"}
        Match -->|"V2_2_1.Location"| Handle221["Handle 2.2.1 fields"]
        Match -->|"V2_1_1.Location"| Handle211["Handle 2.1.1 fields"]
        Match -->|"V2_0.Location"| Handle20["Handle 2.0 fields"]
    end
```

**Consumer implementation:**

```csharp
public class MyLocationsHandler : ILocationsReceiver
{
    public Task<OcpiResult> OnLocationPutAsync(
        OcpiRequestContext context, string locationId, object location, CancellationToken ct)
    {
        // Option 1: Pattern match per version
        return location switch
        {
            V2_2_1.Location loc => SaveLocation(locationId, loc.Name, loc.Address,
                loc.Coordinates, loc.CountryCode, loc.PartyId, ct),
            V2_1_1.Location loc => SaveLocation(locationId, loc.Name, loc.Address,
                loc.Coordinates, countryCode: null, partyId: null, ct),
            V2_0.Location loc => SaveLocation(locationId, loc.Name, loc.Address,
                loc.Coordinates, countryCode: null, partyId: null, ct),
            _ => Task.FromResult(OcpiResult.Failure(OcpiStatusCode.GenericServerError,
                "Unsupported version"))
        };
    }
}
```

**Why `object` instead of generics or a common interface?**

| Approach | Pros | Cons |
|---|---|---|
| `object` + pattern match | Single handler, works with DI, explicit about version differences | Casting required |
| Generic `ILocationsReceiver<T>` | Type-safe | Must register N handlers for N versions, DI complexity |
| Common `ILocation` interface | Uniform access | Lowest common denominator, loses version-specific fields |

The `object` approach was chosen because:
1. Most consumers support 1-2 versions, not all 4. Pattern matching makes unsupported versions explicit.
2. DI registration is simple: one `ILocationsReceiver` per consumer, not one per version.
3. No field-level compromises — consumers see the exact model for each version.

### Module Handler Factory

```mermaid
graph TD
    subgraph "Module Handler Factory"
        Factory[ModuleHandlerFactory]
        Factory -->|Resolves| V20Handlers["V2_0 Handlers"]
        Factory -->|Resolves| V211Handlers["V2_1_1 Handlers"]
        Factory -->|Resolves| V22Handlers["V2_2 Handlers"]
        Factory -->|Resolves| V221Handlers["V2_2_1 Handlers"]
    end

    subgraph "V2_2_1 Handlers"
        V221Handlers --> V221Loc[LocationsReceiver]
        V221Handlers --> V221Sess[SessionsReceiver]
        V221Handlers --> V221Cdr[CdrsReceiver]
        V221Handlers --> V221Tar[TariffsReceiver]
        V221Handlers --> V221Tok[TokensSender]
        V221Handlers --> V221Cmd[CommandsSender]
        V221Handlers --> V221CP[ChargingProfilesSender]
    end

    Request[Request with CPO context] --> Factory
    Factory -->|"version = 2.2.1"| V221Handlers
```

### Receiver vs Sender Module Patterns

```mermaid
graph LR
    subgraph "Receiver Module (eMSP receives data)"
        direction TB
        R_Server["Server endpoints<br/>(PUT/PATCH/POST)"]
        R_Client["Client methods<br/>(GET for pull)"]
        R_Server -->|"CPO pushes data"| R_Consumer["Consumer callback"]
        R_Client -->|"eMSP pulls data"| R_Return["Returns models"]
    end

    subgraph "Sender Module (eMSP sends data)"
        direction TB
        S_Server["Server endpoints<br/>(GET for CPO pull,<br/>POST for authorize)"]
        S_Client["Client methods<br/>(PUT/PATCH for push)"]
        S_Server -->|"CPO pulls data"| S_Consumer["Consumer provides data"]
        S_Client -->|"eMSP pushes data"| S_Return["Returns result"]
    end
```

---

## 8. Multi-Party Support

The eMSP can present different identities (`country_code`/`party_id`) to different CPOs. This enables a single deployment to act as multiple eMSP entities.

```mermaid
graph TD
    subgraph "Single DotOcpi Deployment"
        subgraph "eMSP Identity: DE/ABC"
            CPO_A["CPO A (OCPI 2.2.1)<br/>Sees eMSP as DE/ABC"]
            CPO_B["CPO B (OCPI 2.1.1)<br/>Sees eMSP as DE/ABC"]
        end

        subgraph "eMSP Identity: NL/XYZ"
            CPO_C["CPO C (OCPI 2.2.1)<br/>Sees eMSP as NL/XYZ"]
        end

        subgraph "eMSP Identity: FR/QRS"
            CPO_D["CPO D (OCPI 2.0)<br/>Sees eMSP as FR/QRS"]
        end
    end

    Registry[(CPO Registry)]
    Registry -->|"CPO A → DE/ABC, v2.2.1"| CPO_A
    Registry -->|"CPO B → DE/ABC, v2.1.1"| CPO_B
    Registry -->|"CPO C → NL/XYZ, v2.2.1"| CPO_C
    Registry -->|"CPO D → FR/QRS, v2.0"| CPO_D
```

### Version Endpoints per Identity

The versions endpoint must return version information scoped to the eMSP identity. In 2.2+, the credentials `roles` array declares the identity:

```mermaid
graph TD
    subgraph "Version Endpoints"
        V["/ocpi/versions"]
        VD["/ocpi/versions/2.2.1/"]
    end

    subgraph "Credentials Registration"
        POST["POST /ocpi/2.2.1/credentials"]
        POST -->|"Body includes roles:<br/>[{role: EMSP, party_id: ABC, country_code: DE}]"| Register
        Register[Store CPO with eMSP identity DE/ABC]
    end

    subgraph "Subsequent Requests"
        Auth["Authorization: Token {TOKEN_B}"]
        Auth --> Lookup["Token hash → CPO entry → eMSP identity"]
        Lookup --> Route["Route to correct handlers<br/>with identity context"]
    end
```

---

## 9. Token Management

### Token Store Architecture

```mermaid
classDiagram
    class ITokenStore {
        <<interface>>
        +StoreTokenAsync(cpoId, tokenHash, purpose, ct) Task
        +GetTokenAsync(cpoId, purpose, ct) Task~string?~
        +RemoveTokenAsync(cpoId, purpose, ct) Task
    }

    class TokenPurpose {
        <<enumeration>>
        TokenA
        InboundTokenB
        OutboundTokenC
    }

    class TokenValidator {
        +ValidateAsync(authHeader, ct) ValueTask~TokenValidationResult~
    }

    class InMemoryTokenStore {
        -ConcurrentDictionary _tokens
        +StoreTokenAsync(...)
        +GetTokenAsync(...)
        +RemoveTokenAsync(...)
    }

    class ITokenStore_Consumer {
        <<consumer implements>>
        KeyVaultTokenStore
        DatabaseTokenStore
        etc.
    }

    ITokenStore <|.. InMemoryTokenStore : built-in
    ITokenStore <|.. ITokenStore_Consumer : consumer provides
    ITokenStore --> TokenPurpose

    class TokenValidationResult {
        +IsValid: bool
        +CpoId: string?
        +EmspIdentity: PartyIdentity?
        +NegotiatedVersion: OcpiVersion?
    }

    TokenValidator --> TokenValidationResult
    TokenValidator --> ITokenStore : uses
```

### Token Security Requirements

```mermaid
graph TD
    subgraph "Token Generation"
        Gen[RandomNumberGenerator.GetBytes(64)]
        Gen --> Encode[Base64Url encode]
        Encode --> Raw[Raw token value]
    end

    subgraph "Token Storage"
        Raw --> Hash[SHA-256 hash]
        Hash --> Store[Store hash only]
        Raw --> Transport[Use in Authorization header only]
        Transport --> Discard[Never persist raw value]
    end

    subgraph "Token Validation"
        Incoming[Incoming Authorization header] --> Extract[Extract raw token]
        Extract --> HashIncoming[SHA-256 hash incoming]
        HashIncoming --> Compare["CryptographicOperations.FixedTimeEquals()<br/>(constant-time comparison)"]
        Store --> Compare
        Compare --> Result{Match?}
    end
```

---

## 10. CPO Connection Registry

### Registry Data Model

```mermaid
classDiagram
    class ICpoRegistry {
        <<interface>>
        +GetAsync(cpoId, ct) Task~CpoConnection?~
        +GetByCpoPartyAsync(countryCode, partyId, ct) Task~CpoConnection?~
        +GetByTokenHashAsync(tokenHash, ct) Task~CpoConnection?~
        +UpsertAsync(connection, ct) Task
        +RemoveAsync(cpoId, ct) Task
        +GetAllAsync(ct) Task~IReadOnlyList~CpoConnection~~
        +UpdateStatusAsync(cpoId, status, ct) Task
        +UpdateLastActivityAsync(cpoId, ct) Task
    }

    class CpoConnection {
        +Id: string
        +CpoIdentity: PartyIdentity
        +EmspIdentity: PartyIdentity
        +NegotiatedVersion: OcpiVersion
        +ModuleEndpoints: IReadOnlyDictionary~string, Uri~
        +InboundTokenHash: string
        +OutboundTokenHash: string
        +Status: ConnectionStatus
        +BusinessDetails: BusinessDetails
        +Version: long
        +LastActivity: DateTimeOffset
        +LastUpdated: DateTimeOffset
        +CreatedAt: DateTimeOffset
    }

    class PartyIdentity {
        +CountryCode: string
        +PartyId: string
    }

    class ConnectionStatus {
        <<enumeration>>
        PENDING
        CONNECTED
        SUSPENDED
        OFFLINE
    }

    class ICpoRegistryStore {
        <<interface - consumer implements for persistence>>
        +LoadAllAsync(ct) Task~IReadOnlyList~CpoConnection~~
        +SaveAsync(connection, ct) Task
        +DeleteAsync(cpoId, ct) Task
    }

    ICpoRegistry --> CpoConnection
    CpoConnection --> PartyIdentity : CpoIdentity
    CpoConnection --> PartyIdentity : EmspIdentity
    CpoConnection --> ConnectionStatus
    ICpoRegistry ..> ICpoRegistryStore : backed by
```

### Registry Lookup Flow

```mermaid
graph TD
    subgraph "Inbound Request (CPO → eMSP)"
        IR[Request arrives] --> ExtractToken[Extract token from<br/>Authorization header]
        ExtractToken --> HashToken[Hash token]
        HashToken --> LookupToken["Registry.GetByTokenHashAsync()"]
        LookupToken --> Found{Found?}
        Found -->|No| Reject[401 Unauthorized]
        Found -->|Yes| Context[Build OcpiRequestContext<br/>with CpoConnection]
    end

    subgraph "Outbound Request (eMSP → CPO)"
        OR[Consumer calls client] --> LookupCpo["Registry.GetAsync(cpoId)"]
        LookupCpo --> GetEndpoint[Get module endpoint URL<br/>for negotiated version]
        GetEndpoint --> GetToken[Get outbound token hash<br/>→ retrieve raw from ITokenStore]
        GetToken --> BuildReq[Build HTTP request]
    end
```

---

## 11. Error Handling

### OcpiResult Design

```mermaid
classDiagram
    class OcpiResult {
        +StatusCode: OcpiStatusCode
        +StatusMessage: string?
        +IsSuccess: bool
        +Success() OcpiResult
        +Failure(code, message) OcpiResult
    }

    class OcpiResult_T {
        +Data: T?
        +StatusCode: OcpiStatusCode
        +StatusMessage: string?
        +IsSuccess: bool
        +Success(data) OcpiResult~T~
        +Failure(code, message) OcpiResult~T~
    }

    class OcpiResponse_T {
        <<wire envelope>>
        +Data: T?
        +StatusCode: int
        +StatusMessage: string?
        +Timestamp: DateTimeOffset
    }

    class OcpiStatusCode {
        <<readonly record struct>>
        +Value: int
        +IsSuccess: bool
        +IsClientError: bool
        +IsServerError: bool
        +Success: OcpiStatusCode = 1000
        +GenericClientError: OcpiStatusCode = 2000
        +InvalidParameters: OcpiStatusCode = 2001
        +NotEnoughInformation: OcpiStatusCode = 2002
        +UnknownLocation: OcpiStatusCode = 2003
        +UnknownToken: OcpiStatusCode = 2004
        +GenericServerError: OcpiStatusCode = 3000
        +UnableToUseClientApi: OcpiStatusCode = 3001
        +UnsupportedVersion: OcpiStatusCode = 3002
        +NoMatchingEndpoints: OcpiStatusCode = 3003
    }

    note for OcpiResult "OcpiResult is the internal result type.\nOcpiResponse is the wire envelope with Timestamp.\nBoth are sealed classes — no inheritance between them."
```

### Error Flow

```mermaid
graph TD
    subgraph "Client-side (calling CPO)"
        Call[Call CPO endpoint] --> HTTP{HTTP Status}
        HTTP -->|2xx| Parse[Parse OCPI envelope]
        Parse --> Check{status_code == 1000?}
        Check -->|Yes| Success["OcpiResult&lt;T&gt;.Success(data)"]
        Check -->|No| OcpiError["OcpiResult&lt;T&gt;.Failure(code, message)"]
        HTTP -->|4xx/5xx| HttpError["OcpiResult&lt;T&gt;.Failure(mapped_code, message)"]
        HTTP -->|Network error| Exception[Throw OcpiTransportException]
    end

    subgraph "Server-side (receiving from CPO)"
        Receive[Receive request] --> Validate{Valid?}
        Validate -->|No| Return4xx["Return {status_code: 2001, ...}"]
        Validate -->|Yes| Process[Invoke consumer handler]
        Process --> ConsumerResult{Handler result}
        ConsumerResult -->|Success| Return200["Return {status_code: 1000, data: ...}"]
        ConsumerResult -->|Business error| ReturnBiz["Return {status_code: 2xxx, ...}"]
        Process -->|Exception| Return500["Return {status_code: 3000, ...}"]
    end
```

### Exception Hierarchy

Exceptions are for infrastructure failures, not OCPI protocol errors. See [strategies.md — Exception Strategy](strategies.md#16-exception-strategy) for the full hierarchy and when each exception is thrown.

```mermaid
classDiagram
    class OcpiException {
        +CpoId: string?
        +Version: OcpiVersion?
    }
    class OcpiTransportException {
        +StatusCode: HttpStatusCode?
        +RequestId: string?
    }
    class OcpiRegistrationException {
        +Reason: RegistrationFailureReason
    }
    class OcpiConfigurationException
    class OcpiSerializationException {
        +ModuleId: string?
    }

    Exception <|-- OcpiException
    OcpiException <|-- OcpiTransportException
    OcpiException <|-- OcpiRegistrationException
    OcpiException <|-- OcpiConfigurationException
    OcpiException <|-- OcpiSerializationException
```

---

## 12. Serialization

### Version-Specific JSON Contexts

Each OCPI version has its own `JsonSerializerContext` for source-generated, AOT-compatible serialization.

```mermaid
graph TD
    subgraph "Serialization Strategy"
        Req[Request/Response] --> Version{OCPI Version?}
        Version -->|2.0| Ctx20["OcpiJsonContext_V2_0<br/><i>Source-generated</i>"]
        Version -->|2.1.1| Ctx211["OcpiJsonContext_V2_1_1<br/><i>Source-generated</i>"]
        Version -->|2.2| Ctx22["OcpiJsonContext_V2_2<br/><i>Source-generated</i>"]
        Version -->|2.2.1| Ctx221["OcpiJsonContext_V2_2_1<br/><i>Source-generated</i>"]
    end

    subgraph "Shared Custom Converters"
        Conv1["OcpiDateTimeConverter<br/><i>RFC 3339 format</i>"]
        Conv2["OcpiEnumConverter<br/><i>OCPI string values, e.g. UPPER_SNAKE_CASE</i>"]
        Conv3["CiStringConverter<br/><i>Case-insensitive string type</i>"]
        Conv4["PriceConverter<br/><i>2.2+ Price object vs 2.0/2.1.1 number</i>"]
    end

    Ctx20 & Ctx211 & Ctx22 & Ctx221 --> Conv1 & Conv2 & Conv3
    Ctx22 & Ctx221 --> Conv4
```

### OCPI Response Envelope

Every response is wrapped in a standard envelope. The library handles this transparently:

```json
{
    "data": "...(version-specific model)...",
    "status_code": 1000,
    "status_message": "Success",
    "timestamp": "2026-03-13T10:30:00Z"
}
```

```mermaid
classDiagram
    class OcpiResponse_T {
        +Data: T?
        +StatusCode: int
        +StatusMessage: string?
        +Timestamp: DateTimeOffset
    }

    note for OcpiResponse_T "Serialized/deserialized by the pipeline.\nConsumers never interact with this directly —\nthey work with OcpiResult&lt;T&gt; instead."
```

---

## 13. Pagination

### Client-Side Pagination (Pull)

```mermaid
sequenceDiagram
    participant Consumer as Consumer Code
    participant Client as DotOcpi.Client
    participant CPO as CPO

    Consumer->>Client: GetAllLocationsAsync(cpoId, dateFrom)

    loop Until no Link header
        Client->>CPO: GET /locations?date_from=...&offset=N&limit=M
        CPO-->>Client: 200 OK<br/>Headers: X-Total-Count, X-Limit, Link<br/>Body: {data: [...], status_code: 1000}
        Client->>Consumer: yield each item via IAsyncEnumerable
        Note over Client: Check Link header for next page URL
    end
```

### Server-Side Pagination (Serve)

```mermaid
graph TD
    subgraph "Server handles pagination for Sender modules"
        Req[GET request with offset/limit] --> Parse[Parse query params]
        Parse --> Query["Consumer.GetTokensAsync(<br/>dateFrom, dateTo, offset, limit)"]
        Query --> Count["Consumer.GetTokenCountAsync(<br/>dateFrom, dateTo)"]
        Count --> Headers["Set X-Total-Count, X-Limit"]
        Headers --> HasMore{offset + count < total?}
        HasMore -->|Yes| Link["Set Link header with next URL"]
        HasMore -->|No| NoLink[Omit Link header]
        Link --> Respond[Return OCPI response envelope]
        NoLink --> Respond
    end
```

### IAsyncEnumerable Support

For large datasets, the client exposes streaming pagination:

```csharp
// Consumer can iterate page by page without loading all into memory
await foreach (var location in client.Locations.GetAllAsAsyncEnumerable(cpoId, ct))
{
    // Process each location as it arrives
}
```

---

## 14. Observability

### Distributed Tracing with ActivitySource

```mermaid
graph TD
    subgraph "DotOcpi Instrumentation"
        Source["static ActivitySource<br/>name: 'DotOcpi'"]

        subgraph "Client Activities"
            CA1["DotOcpi.Client.GetLocations"]
            CA2["DotOcpi.Client.PushToken"]
            CA3["DotOcpi.Client.SendCommand"]
            CA4["DotOcpi.Client.Register"]
        end

        subgraph "Server Activities"
            SA1["DotOcpi.Server.HandleLocationPut"]
            SA2["DotOcpi.Server.HandleSessionPut"]
            SA3["DotOcpi.Server.HandleCdrPost"]
            SA4["DotOcpi.Server.HandleAuthorize"]
        end

        subgraph "Tags"
            T1["ocpi.version"]
            T2["ocpi.module"]
            T3["ocpi.cpo.country_code"]
            T4["ocpi.cpo.party_id"]
            T5["ocpi.status_code"]
            T6["ocpi.request_id"]
            T7["ocpi.correlation_id"]
        end

        Source --> CA1 & CA2 & CA3 & CA4
        Source --> SA1 & SA2 & SA3 & SA4
        CA1 & SA1 --> T1 & T2 & T3 & T4 & T5 & T6 & T7
    end

    subgraph "Consumer Opt-In"
        OTel["builder.Services.AddOpenTelemetry()<br/>.WithTracing(t => t.AddSource('DotOcpi'))"]
    end

    Source -.-> OTel
```

---

## 15. Project Structure

```
DotOcpi/
├── src/
│   ├── DotOcpi/
│   │   ├── DotOcpi.csproj
│   │   ├── OcpiVersion.cs
│   │   ├── OcpiResult.cs
│   │   ├── OcpiStatusCode.cs
│   │   ├── OcpiRequestContext.cs
│   │   ├── Exceptions/
│   │   │   ├── OcpiException.cs
│   │   │   ├── OcpiTransportException.cs
│   │   │   ├── OcpiRegistrationException.cs
│   │   │   ├── OcpiConfigurationException.cs
│   │   │   └── OcpiSerializationException.cs
│   │   ├── OcpiSentinel.cs
│   │   ├── CiString.cs
│   │   ├── GeoLocation.cs
│   │   ├── PaginatedResult.cs
│   │   ├── Logging/
│   │   │   ├── LogCategories.cs
│   │   │   ├── OcpiLogEvents.cs
│   │   │   ├── AuthLogMessages.cs
│   │   │   ├── RegistrationLogMessages.cs
│   │   │   ├── ClientLogMessages.cs
│   │   │   ├── ServerLogMessages.cs
│   │   │   └── DotOcpiLoggingOptions.cs
│   │   ├── Observability/
│   │   │   ├── OcpiActivitySource.cs
│   │   │   └── OcpiMetrics.cs
│   │   ├── Registration/
│   │   │   ├── IRegistrationClient.cs
│   │   │   ├── ICredentialsClient.cs
│   │   │   ├── ICredentialsHandler.cs
│   │   │   ├── IVersionDiscovery.cs
│   │   │   └── RegistrationOrchestrator.cs
│   │   ├── TokenManagement/
│   │   │   ├── ITokenStore.cs
│   │   │   ├── InMemoryTokenStore.cs
│   │   │   ├── TokenGenerator.cs
│   │   │   ├── TokenHasher.cs
│   │   │   ├── TokenValidator.cs
│   │   │   └── AuthorizationHeaderParser.cs
│   │   ├── Registry/
│   │   │   ├── ICpoRegistry.cs
│   │   │   ├── ICpoRegistryStore.cs
│   │   │   ├── ICacheInvalidationNotifier.cs
│   │   │   ├── IDistributedLockProvider.cs
│   │   │   ├── CpoConnection.cs
│   │   │   ├── CpoHealthMonitor.cs
│   │   │   ├── InMemoryCpoRegistry.cs
│   │   │   └── PartyIdentity.cs
│   │   ├── Modules/
│   │   │   ├── ILocationsReceiver.cs
│   │   │   ├── ISessionsReceiver.cs
│   │   │   ├── ICdrsReceiver.cs
│   │   │   ├── ITariffsReceiver.cs
│   │   │   ├── ITokensSender.cs
│   │   │   ├── ITokensAuthorizer.cs
│   │   │   ├── ICommandsCallback.cs
│   │   │   └── IChargingProfilesCallback.cs
│   │   ├── Models/
│   │   │   ├── V2_0/
│   │   │   │   ├── Location.cs
│   │   │   │   ├── Session.cs
│   │   │   │   ├── Cdr.cs
│   │   │   │   ├── Token.cs
│   │   │   │   ├── Tariff.cs
│   │   │   │   └── Credentials.cs
│   │   │   ├── V2_1_1/
│   │   │   │   ├── Location.cs
│   │   │   │   ├── Session.cs
│   │   │   │   ├── Cdr.cs
│   │   │   │   ├── Token.cs
│   │   │   │   ├── Tariff.cs
│   │   │   │   ├── Command.cs
│   │   │   │   └── Credentials.cs
│   │   │   ├── V2_2/
│   │   │   │   └── ... (mirrors V2_2_1 with minor differences)
│   │   │   └── V2_2_1/
│   │   │       ├── Location.cs
│   │   │       ├── Evse.cs
│   │   │       ├── Connector.cs
│   │   │       ├── Session.cs
│   │   │       ├── Cdr.cs
│   │   │       ├── CdrToken.cs
│   │   │       ├── CdrLocation.cs
│   │   │       ├── Token.cs
│   │   │       ├── Tariff.cs
│   │   │       ├── Command.cs
│   │   │       ├── ChargingProfile.cs
│   │   │       ├── Credentials.cs
│   │   │       ├── CredentialsRole.cs
│   │   │       └── AuthorizationInfo.cs
│   │   ├── Serialization/
│   │   │   ├── OcpiDateTimeConverter.cs
│   │   │   ├── OcpiStatusEnumConverter.cs
│   │   │   ├── CiStringJsonConverter.cs
│   │   │   ├── GeoLocationConverter.cs
│   │   │   ├── PriceConverter.cs
│   │   │   ├── OcpiJsonContext_V2_0.cs
│   │   │   ├── OcpiJsonContext_V2_1_1.cs
│   │   │   ├── OcpiJsonContext_V2_2.cs
│   │   │   ├── OcpiJsonContext_V2_2_1.cs
│   │   │   └── OcpiJsonOptions.cs
│   │   └── Extensions/
│   │       └── ServiceCollectionExtensions.cs
│   │
│   ├── DotOcpi.Client/
│   │   ├── DotOcpi.Client.csproj
│   │   ├── IOcpiClient.cs
│   │   ├── OcpiClient.cs
│   │   ├── Internal/
│   │   │   ├── OcpiHttpRequestBuilder.cs
│   │   │   ├── OcpiResponseParser.cs
│   │   │   ├── PaginationHandler.cs
│   │   │   ├── PullClientHelper.cs
│   │   │   ├── CpoConnectionContextProvider.cs
│   │   │   ├── SsrfGuard.cs
│   │   │   ├── InvalidatingRegistrationClient.cs
│   │   │   └── OcpiModelTypeMap.cs
│   │   ├── Sync/
│   │   │   ├── IOcpiSyncService.cs
│   │   │   ├── OcpiSyncService.cs
│   │   │   ├── IOcpiSyncHandler.cs
│   │   │   ├── OcpiPullSyncBackgroundService.cs
│   │   │   ├── PullSyncOptions.cs
│   │   │   ├── PullSyncOptionsResolver.cs
│   │   │   ├── PullSyncOptionsValidator.cs
│   │   │   ├── CpoSyncOptions.cs
│   │   │   ├── ModuleSyncOptions.cs
│   │   │   ├── SyncContext.cs
│   │   │   ├── SyncResult.cs
│   │   │   ├── ISyncStateStore.cs
│   │   │   └── InMemorySyncStateStore.cs
│   │   ├── Modules/
│   │   │   ├── LocationsClient.cs
│   │   │   ├── SessionsClient.cs
│   │   │   ├── CdrsClient.cs
│   │   │   ├── TariffsClient.cs
│   │   │   ├── TokensClient.cs
│   │   │   ├── CommandsClient.cs
│   │   │   └── ChargingProfilesClient.cs
│   │   └── Extensions/
│   │       └── DotOcpiClientExtensions.cs
│   │
│   ├── DotOcpi.AspNetCore/
│   │   ├── DotOcpi.AspNetCore.csproj
│   │   ├── Filters/
│   │   │   ├── OcpiAuthFilter.cs
│   │   │   ├── OcpiValidationFilter.cs
│   │   │   ├── OcpiMetricsFilter.cs
│   │   │   └── OcpiRateLimitFilter.cs
│   │   ├── Middleware/
│   │   │   ├── OcpiExceptionMiddleware.cs
│   │   │   ├── OcpiRequestIdMiddleware.cs
│   │   │   └── OcpiSecurityHeadersMiddleware.cs
│   │   ├── Endpoints/
│   │   │   ├── VersionsEndpoints.cs
│   │   │   ├── CredentialsEndpoints.cs
│   │   │   ├── LocationsEndpoints.cs
│   │   │   ├── SessionsEndpoints.cs
│   │   │   ├── CdrsEndpoints.cs
│   │   │   ├── TariffsEndpoints.cs
│   │   │   ├── TokensEndpoints.cs
│   │   │   ├── CommandsEndpoints.cs
│   │   │   └── ChargingProfilesEndpoints.cs
│   │   ├── Routing/
│   │   │   ├── OcpiEndpointRouteBuilder.cs
│   │   │   └── OcpiVersionRouter.cs
│   │   └── Extensions/
│   │       ├── ServiceCollectionExtensions.cs
│   │       └── EndpointRouteBuilderExtensions.cs
│   │
│   └── DotOcpi.Simulator/
│       ├── DotOcpi.Simulator.csproj
│       ├── OcpiCpoSimulator.cs
│       ├── CpoSimulatorConfiguration.cs
│       └── Handlers/
│           ├── TestVersionsHandler.cs
│           ├── TestCredentialsHandler.cs
│           ├── TestLocationsHandler.cs
│           └── ...
│
├── tests/
│   ├── DotOcpi.Tests/                    # Unit tests for core library
│   ├── DotOcpi.Client.Tests/             # Unit tests for client
│   ├── DotOcpi.AspNetCore.Tests/         # Unit + integration for server
│   ├── DotOcpi.Integration.Tests/        # Full pipeline integration tests
│   └── DotOcpi.Simulator.Tests/            # Tests for the testing package
│
├── samples/
│   └── DotOcpi.Sample/
│
├── Directory.Build.props
├── Directory.Build.targets
├── Directory.Packages.props
├── global.json
├── .editorconfig
├── nuget.config
├── DotOcpi.sln
├── CHANGELOG.md
├── README.md
└── LICENSE
```

### Consumer Usage (Minimal Example)

```csharp
// Program.cs — eMSP application
var builder = WebApplication.CreateBuilder(args);

// Register DotOcpi services
builder.Services.AddDotOcpi(options =>
{
    options.SupportedVersions = [OcpiVersion.V2_1_1, OcpiVersion.V2_2_1];
})
.AddAspNetCoreServer()
.AddClient()
.AddInMemoryTokenStore()      // or .AddTokenStore<MyKeyVaultTokenStore>()
.AddInMemoryCpoRegistry();    // or .AddCpoRegistryStore<MyDbRegistryStore>()

// Register consumer module handlers
builder.Services.AddScoped<ILocationsReceiver, MyLocationsHandler>();
builder.Services.AddScoped<ISessionsReceiver, MySessionsHandler>();
builder.Services.AddScoped<ICdrsReceiver, MyCdrsHandler>();
builder.Services.AddScoped<ITokensAuthorizer, MyTokensAuthorizer>();

var app = builder.Build();

// Map OCPI endpoints
app.MapOcpiEndpoints();

app.Run();
```

---

## 16. Testing Architecture

The `DotOcpi.Simulator` package provides an in-memory OCPI-compliant CPO server for consumer integration tests. See [strategies.md — Testing Strategy](strategies.md#17-testing-strategy-dotocpitesting) for the full API and usage examples.

```mermaid
graph TD
    subgraph "Consumer Test"
        Test["xUnit Test"]
        Factory["WebApplicationFactory<br/>(in-memory eMSP)"]
        TestCpo["OcpiCpoSimulator<br/>(in-memory CPO)"]
    end

    Factory -->|"Register with Token A"| TestCpo
    TestCpo -->|"Returns Token C"| Factory
    Factory -->|"Pull locations"| TestCpo
    TestCpo -->|"Returns test data"| Factory
    Factory -->|"Push session"| Test
    Test -->|"Assert results"| Test
```

### Test CPO Capabilities

| Capability | Description |
|---|---|
| Version negotiation | Full version discovery + detail endpoint |
| Credentials handshake | Token A/B/C exchange with configurable behavior |
| Module responses | Pre-configured data returned by GET endpoints |
| Push acceptance | Accepts PUT/PATCH/POST with configurable validation |
| Command simulation | Configurable sync response + async callback |
| Failure injection | HTTP errors, OCPI errors, timeouts, partial failures |
| Multi-version | Advertise multiple OCPI versions simultaneously |
