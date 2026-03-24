# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added
- IOcpiSyncHandler callback interface for receiving pulled OCPI data as page-level batches during sync
- SyncContext and SyncResult types for sync operation metadata
- OcpiPullSyncBackgroundService — periodic background service driving pull sync for all active CPOs
- Hierarchical PullSyncOptions with per-module (ModuleSyncOptions) and per-CPO (CpoSyncOptions) overrides; resolution order: CPO+module → CPO default → module-level → global
- PullSyncOptionsValidator — startup validation rejecting invalid intervals, jitter, and module names
- PaginationHandler.StreamPagesAsync — page-level streaming for efficient sync handler delivery
- AddPullSync() registers background service; AddSyncHandler<T>() registers consumer's IOcpiSyncHandler
- Max-page guard (10,000) in PaginationHandler to prevent infinite pagination from malicious CPOs
- OcpiRequestIdMiddleware — middleware for X-Request-ID / X-Correlation-ID header propagation
- OcpiSecurityHeadersMiddleware — sets X-Content-Type-Options: nosniff, Cache-Control: no-store, X-Frame-Options: DENY
- OcpiMetricsFilter — endpoint filter populating OcpiMetrics counters and duration histograms
- OcpiRateLimitFilter — per-CPO rate limiting as endpoint filter (runs after auth, has access to CpoConnection)
- SsrfGuard — DNS-level SSRF prevention blocking private/loopback/link-local IPs on outbound requests
- InvalidatingRegistrationClient — auto-invalidates connection cache after registration operations
- CpoConnectionContextProvider — caches resolved CPO connection + auth token per CPO
- IOcpiClient.InvalidateConnection(cpoId) and InvalidateAllConnections() for manual cache invalidation
- DotOcpi.Simulator package (renamed from DotOcpi.Testing) — full OCPI CPO simulator
- OcpiCpoSimulator: session energy progression with configurable rate and interval
- OcpiCpoSimulator: configurable session/CDR templates via Func<SessionContext, object>
- OcpiCpoSimulator: CPO push webhooks for session updates to eMSP endpoints
- OcpiCpoSimulator: POST /tokens/{uid}/authorize endpoint with configurable result
- OcpiCpoSimulator: simulates full session lifecycle (START_SESSION creates session, STOP_SESSION completes + generates CDR)
- CpoHealthMonitor: actual HTTP health checks against CPO versions endpoints with failure counting and Offline marking
- Sample dashboard: interactive Razor Pages UI with HTMX, SSE real-time updates, Swagger UI
- NuGet packaging metadata (author, license, repository URL) in Directory.Build.props

### Added (Security)
- OcpiRegistrationContext — lightweight request context for initial CPO registration (Token A), used before any CpoConnection exists
- OcpiTokenAAuthFilter — endpoint filter that validates Token A for credentials POST, rejects Token B (registration requires Token A specifically)
- OcpiRegistrationContextFilter — builds OcpiRegistrationContext from validated Token A entry
- MapCredentialsRegistrationEndpoints — maps credentials POST on a separate route group outside the auth-filtered pipeline, fixing registration denial-of-service where OcpiAuthFilter blocked Token A requests that had no CpoConnection

### Changed
- ICredentialsHandler.OnCredentialsPostAsync now accepts OcpiRegistrationContext instead of OcpiRequestContext (breaking change — registration has no CpoConnection)
- MapCredentialsEndpoints now maps only PUT/GET/DELETE (Token B auth); POST is handled by MapCredentialsRegistrationEndpoints
- DotOcpi.Testing renamed to DotOcpi.Simulator; OcpiTestCpoServer → OcpiCpoSimulator, TestCpoConfiguration → CpoSimulatorConfiguration
- IOcpiSyncService.SyncModuleFromCpoAsync returns Task<SyncResult> instead of Task
- OcpiSyncService delivers page-level batches to IOcpiSyncHandler instead of discarding pulled items
- PullSyncOptions.Modules renamed to EnabledModules; RandomJitter renamed to MaxJitter
- OcpiRequestIdFilter replaced by OcpiRequestIdMiddleware (cross-cutting, runs before endpoint routing)
- OcpiRateLimitingMiddleware replaced by OcpiRateLimitFilter (needs CpoConnection from auth pipeline)
- Client module internals refactored to cache CPO connection context per CPO, eliminating redundant per-call lookups
- OcpiHttpRequestBuilder is now stateless and no longer depends on ICpoRegistry

### Fixed
- CpoHealthMonitor registered as a hosted service by AddAspNetCoreServer(); respects EnableHealthMonitoring option
- CDR Location header now uses eMSP party identity instead of CPO's
- Sync state persisted on partial failure to prevent duplicate page re-delivery
- Simulator: JSON merge patch now correctly writes scalar replacements (RFC 7386)
- Simulator: default pagination limit is 50 instead of returning all items
- Simulator: charging profiles PUT returns standard OCPI success (no fabricated data object)
- Simulator: EVSE state Update() uses explicit clear flags instead of asymmetric null semantics
- Simulator: token PATCH correctly rejects unknown tokens instead of unreachable error path
- Simulator: UNLOCK_CONNECTOR command now clears active session and sets EVSE to Available
- Simulator: expired reservations are cleaned up during charging tick loop
- Added null-suppression justification comments to all GetOcpiContext()! calls in endpoint handlers

### Removed
- OcpiValidationFilter — dead code; validation runs inline via EndpointHelper.DeserializeOrRejectAsync
- ICacheInvalidationNotifier, IDistributedLockProvider — unintegrated extension points; will be reintroduced with proper wiring when multi-instance support is implemented
- OcpiPatchHelper — consumers handle JSON PATCH merge in their own domain layer
- NullableCiStringConverter, OcpiEnumConverterFactory, OcpiNullableDateTimeConverter — consolidated into simpler converters
- OcpiRequestIdFilter — replaced by OcpiRequestIdMiddleware
- OcpiRateLimitingMiddleware — replaced by OcpiRateLimitFilter

### Security
- SyncResult.ErrorMessage sanitized — generic message only, no raw exception details in public API
- Module ID validation in SyncModuleFromCpoAsync prevents invalid modules from reaching internal type selectors
- SSRF guard validates CPO-provided URLs post-DNS-resolution against private IP ranges

## [0.1.0] - 2026-03-14

### Added
- Core primitives: OcpiVersion, CiString, PartyIdentity, GeoLocation, OcpiStatusCode, OcpiResult<T>
- Version-specific models for OCPI 2.0, 2.1.1, 2.2, and 2.2.1 (Locations, Sessions, CDRs, Tariffs, Tokens, Commands, ChargingProfiles, Credentials)
- Source-generated JSON serialization with snake_case naming and custom converters for OCPI-specific formats
- Model validation with IOcpiValidator<T> and version-aware validators for all module types
- Token management: CSPRNG generation (64 bytes, base64url), SHA-256 hashing with stackalloc, constant-time comparison, ITokenStore abstraction with InMemoryTokenStore
- CPO connection registry: ICpoRegistry with triple-index InMemoryCpoRegistry, optimistic concurrency, ICpoRegistryStore for persistent backing
- Version discovery, negotiation, and full registration orchestrator (Token A -> B -> C lifecycle)
- ASP.NET Core server: OCPI auth filter, request ID filter, validation filter, exception middleware, rate limiting middleware, endpoint routing with both URL patterns
- Module handlers for all eMSP modules with version-dispatched strategy pattern
- HTTP client library: typed module clients, IAsyncEnumerable pagination, stream-based JSON, Polly resilience
- Pull sync service with ISyncStateStore for incremental synchronization
- Structured logging with [LoggerMessage] source generation across all subsystems
- Distributed tracing via System.Diagnostics.ActivitySource "DotOcpi"
- OcpiMetrics with request counter, duration histogram, connection gauge, and auth failure counter
- Health checks: OcpiRegistryHealthCheck (aggregate), OcpiCpoHealthCheck (per-CPO), OcpiTokenStoreHealthCheck
- DI registration via AddDotOcpi() builder pattern with options validation
- DotOcpi.Simulator: OcpiCpoSimulator with configurable module data and failure injection
- Integration tests covering registration flow, module data retrieval, and security
- CI pipeline with net8.0 x net10.0 x ubuntu x windows matrix
- BenchmarkDotNet benchmarks for serialization, token operations, and registry lookups

### Security
- All tokens generated with CSPRNG (RandomNumberGenerator), minimum 64 bytes
- Token hashes stored server-side; raw tokens only in transport headers
- Constant-time comparison via CryptographicOperations.FixedTimeEquals
- HTTPS enforcement for all OCPI endpoint URLs
- OCPI auth filter returns 401 with status code 2002 on invalid tokens
- No stack traces or raw tokens in error responses or logs
