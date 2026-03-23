---
title: Design Decisions
layout: default
parent: Architecture
nav_order: 1
---

# Design Decisions
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Separate Models Per Version

**Decision:** Each OCPI version has its own model namespace (`Models.V2_0`, `Models.V2_1_1`, `Models.V2_2`, `Models.V2_2_1`).

**Why not shared models?** OCPI model shapes change substantially between versions:
- `Location` in 2.2 has `country_code`/`party_id` and `publish` — 2.1.1 doesn't
- Session location is embedded in 2.0/2.1.1 but a reference ID in 2.2+
- CDR end time is `stop_date_time` in 2.0/2.1.1 but `end_date_time` in 2.2+
- Token ID is `auth_id` in 2.0/2.1.1 but `contract_id` in 2.2+

Sharing models across versions leads to nullable-field pollution (`string? CountryCode` that's required in 2.2 but absent in 2.1.1) and conditional serialization logic that's error-prone and hard to test.

**Trade-off:** More code, but each model exactly matches the OCPI spec for its version. Bug surface is smaller. Tests are version-specific and straightforward.

## Result Type Pattern

**Decision:** Use `OcpiResult<T>` instead of exceptions for expected OCPI errors.

**Why?** OCPI status codes (2001 "invalid parameters", 2003 "unknown location") are expected protocol responses, not exceptional conditions. Using exceptions for these:
- Forces callers into try/catch for normal control flow
- Obscures the distinction between protocol errors and real failures
- Incurs unnecessary performance overhead

`OcpiResult<T>` makes success/failure explicit:

```csharp
var result = await handler.GetLocationAsync(context, "LOC1", ct);
if (result.IsSuccess)
    return result.Data;
else
    return Error(result.StatusCode, result.StatusMessage);
```

Exceptions (`OcpiRegistrationException`, `OcpiTransportException`) are reserved for truly exceptional conditions — network failures, serialization bugs, configuration errors.

## Storage-Agnostic Core

**Decision:** The core library owns protocol logic only. No database dependencies.

**Why?** Every eMSP has different storage requirements. Forcing a specific database would:
- Create unnecessary dependencies
- Limit deployment flexibility
- Make the library opinionated about infrastructure

Instead, consumers implement `ITokenStore` and `ICpoRegistryStore` for their backend. Built-in in-memory implementations exist for development.

## Strategy Pattern for Version Dispatch

**Decision:** Module handlers use the strategy pattern to select version-specific implementations at runtime.

```
Incoming Request → Auth → Extract Version → Select Handler → Deserialize → Dispatch
```

Each CPO connection has a negotiated version. When a request arrives, DotOcpi:
1. Identifies the CPO by token
2. Looks up the negotiated version
3. Selects the correct handler and serialization context
4. Deserializes with the version-specific model
5. Calls your handler with the typed data

This happens transparently — your handler code doesn't need to know which version is active.

## ASP.NET Core First, But Decoupled

**Decision:** Server integration is ASP.NET Core-specific, but core logic is framework-independent.

**Package structure:**
- `DotOcpi` — Core models, interfaces, token management (no ASP.NET Core dependency)
- `DotOcpi.AspNetCore` — Server middleware, endpoint routing
- `DotOcpi.Client` — HTTP client (uses `IHttpClientFactory`, not ASP.NET Core)

This lets consumers use DotOcpi models and the client in non-web contexts (background workers, console apps) without pulling in ASP.NET Core.

## Peer-to-Peer Only

**Decision:** No OCPI Hub routing support.

**Why?** Hub routing adds significant complexity (party routing, trust chains, credential proxying) and the vast majority of OCPI deployments use direct peer-to-peer connections. Adding hub support would:
- Complicate the registration flow
- Add routing logic to every request
- Require additional configuration and testing

If hub support is needed in the future, it can be added as a separate package without changing the core.

## Endpoint Filters Over Middleware

**Decision:** OCPI authentication runs as endpoint filters, not middleware.

**Why?** Endpoint filters:
- Run after routing, so they know which endpoint was matched
- Can access endpoint metadata (version, module)
- Don't run for non-OCPI endpoints (health checks, etc.)
- Fail fast before the handler executes

Middleware would run for all requests and need to distinguish OCPI vs non-OCPI paths.

## Source-Generated JSON

**Decision:** Use `System.Text.Json` with source-generated `JsonSerializerContext` per version.

**Why?**
- ~1.6x faster serialization vs reflection
- ~2.2x faster startup
- AOT-compatible
- No runtime code generation

Each version has its own context because the models are different types with different shapes.

## Token Hash Storage

**Decision:** Store SHA-256 hashes of tokens, never raw tokens.

**Why?** If the token store is compromised, the attacker gets hashes — not usable tokens. This follows the same principle as password hashing. Combined with CSPRNG generation (64 bytes / 512 bits of entropy), brute-forcing the hash is computationally infeasible.

## Constant-Time Comparison

**Decision:** All security-sensitive comparisons use `CryptographicOperations.FixedTimeEquals`.

**Why?** Standard string equality (`==`) short-circuits on the first difference, leaking information about which characters match. An attacker can measure response times to guess valid tokens character by character. Constant-time comparison takes the same time regardless of input, preventing timing attacks.
