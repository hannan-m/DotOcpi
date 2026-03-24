---
title: Performance
layout: default
parent: Advanced
nav_order: 4
---

# Performance
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Design Principles

DotOcpi is designed for high-throughput, low-latency OCPI operations:

- **Zero-allocation hot paths** — Token validation, header parsing, and registry lookups avoid heap allocation
- **Source-generated JSON** — Per-version `JsonSerializerContext` for AOT-safe serialization
- **Stream-based I/O** — Request/response bodies are never buffered to strings
- **Connection pooling** — HTTP/2 with configurable connection limits

## Source-Generated Serialization

Each OCPI version has its own `JsonSerializerContext`:

```csharp
var options = OcpiJsonOptions.GetOptions(OcpiVersion.V2_2_1);
var json = JsonSerializer.Serialize(location, options);
```

Performance characteristics:
- ~1.6x faster serialization vs reflection-based
- ~2.2x faster startup (no runtime code generation)
- AOT-compatible
- Cached `JsonSerializerOptions` per version (created once, reused)

## Token Validation Path

The token validation hot path is optimized for zero allocation:

```
Authorization header
    → Span-based parsing (no string allocation)
    → SHA-256 hash (stackalloc 32 bytes)
    → Dictionary lookup (O(1))
    → CryptographicOperations.FixedTimeEquals
```

Target: **< 1 microsecond** per validation.

## Memory Optimization

| Technique | Where |
|:----------|:------|
| `readonly record struct` | `PartyIdentity`, `GeoLocation`, `OcpiStatusCode` |
| `Span<T>` parsing | Authorization header, version strings |
| `stackalloc` | SHA-256 output (32 bytes) |
| `static` lambdas | Endpoint handlers (no closures) |
| `FrozenDictionary` | Module maps, version lookups (net8.0+) |

## HTTP Client Tuning

The OCPI client uses optimized HTTP settings:

```csharp
// Configured automatically by AddClient()
// Default settings:
HttpVersion.Version20
MaxConnectionsPerServer = 10
PooledConnectionLifetime = TimeSpan.FromMinutes(5)
PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2)
```

### Stream-Based Responses

Large responses (paginated lists) are read as streams, not buffered:

```csharp
// The client uses HttpCompletionOption.ResponseHeadersRead
// Response body is read as a stream — never buffered to a string
```

## Async Patterns

| Pattern | Usage |
|:--------|:------|
| `ValueTask` | Token store lookups, registry cache hits |
| `ConfigureAwait(false)` | All library code (not middleware) |
| `IAsyncEnumerable<T>` | Paginated results (yields without buffering) |
| `[EnumeratorCancellation]` | All async enumerable parameters |
| `CancellationToken` | Every public async method |

## GC Recommendations

For production deployments:

```xml
<!-- In your hosting csproj -->
<PropertyGroup>
  <ServerGarbageCollection>true</ServerGarbageCollection>
  <ConcurrentGarbageCollection>true</ConcurrentGarbageCollection>
</PropertyGroup>
```

## Benchmarks

Run the included benchmarks:

```bash
dotnet run --project benchmarks/DotOcpi.Benchmarks -c Release --framework net8.0 -- --filter "*"
```

### Key Benchmark Targets

| Benchmark | Target |
|:----------|:-------|
| Token generation | < 10 microseconds |
| Token hash + lookup | < 1 microsecond |
| Location serialization | < 200 nanoseconds |
| Location deserialization | < 500 nanoseconds |
| Registry lookup by key | < 100 nanoseconds |
| Registry lookup by hash | < 100 nanoseconds |

### Available Benchmarks

| Class | What it measures |
|:------|:-----------------|
| `SerializationBenchmarks` | Location serialize/deserialize with V2.2.1 models |
| `TokenBenchmarks` | Token generation, hashing, and store lookup |
| `RegistryBenchmarks` | CPO registry lookups across all three indexes |
