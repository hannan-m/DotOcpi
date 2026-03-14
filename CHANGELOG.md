# Changelog

All notable changes to this project will be documented in this file.

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
- DotOcpi.Testing: OcpiTestCpoServer with configurable module data and failure injection
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
