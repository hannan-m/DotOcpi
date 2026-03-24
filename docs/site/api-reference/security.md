---
title: Security
layout: default
parent: API Reference
nav_order: 5
---

# Security APIs
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## TokenGenerator

Generates cryptographically secure tokens for OCPI authentication.

```csharp
public static class TokenGenerator
{
    public const int MinTokenBytes = 64;

    public static string Generate(int byteLength = MinTokenBytes);
}
```

### Security Properties

- Uses `RandomNumberGenerator` (platform CSPRNG)
- Minimum 64 bytes of entropy
- Base64url-encoded (URL-safe, no padding)
- Each call produces a unique, unpredictable token

### Usage

```csharp
var token = TokenGenerator.Generate();
// "dGhpcyBpcyBhIHRlc3QgdG9rZW4gd2l0aCBzaXh0eS1mb3VyIGJ5dGVzIG9m..."
// 86+ characters, base64url-encoded
```

---

## TokenHasher

Computes SHA-256 hashes of tokens for secure storage.

```csharp
public static class TokenHasher
{
    public static string Hash(string token);
}
```

### Security Properties

- SHA-256 with `stackalloc` (no heap allocation for hash output)
- Hex-encoded output (64 characters)
- Deterministic: same input always produces the same hash

### Usage

```csharp
var raw = TokenGenerator.Generate();
var hash = TokenHasher.Hash(raw);
// Store hash in database; raw is only in transport headers
```

---

## AuthorizationHeaderParser

Parses `Authorization: Token <value>` headers with zero allocation.

```csharp
public static class AuthorizationHeaderParser
{
    public static bool TryParse(ReadOnlySpan<char> headerValue, out string token);
}
```

### Security Properties

- Span-based parsing (zero heap allocation)
- Rejects malformed headers (missing "Token" prefix, empty value)
- Trims whitespace

### Usage

```csharp
if (AuthorizationHeaderParser.TryParse(authHeader, out var token))
{
    var hash = TokenHasher.Hash(token);
    var entry = await tokenStore.FindAsync(hash, ct);
}
```

---

## ITokenStore

Abstraction for secure token hash storage.

```csharp
public interface ITokenStore
{
    // Inbound token hash storage (Token B)
    ValueTask StoreAsync(
        string tokenHash, TokenPurpose purpose, string partyId,
        CancellationToken cancellationToken = default);

    ValueTask<TokenEntry?> FindAsync(
        string tokenHash, CancellationToken cancellationToken = default);

    ValueTask<bool> RemoveAsync(
        string tokenHash, CancellationToken cancellationToken = default);

    // Atomic rotation: stores new hash, then removes old (default implementation)
    ValueTask RotateTokenAsync(
        string oldTokenHash, string newTokenHash, TokenPurpose purpose, string partyId,
        CancellationToken cancellationToken = default);

    // Outbound CPO token storage (Token C) — protected via ITokenProtector
    // Default implementations are no-op; override for persistent storage
    ValueTask StoreCpoTokenAsync(
        string cpoId, string protectedToken, CancellationToken cancellationToken = default);
    ValueTask<string?> GetCpoTokenAsync(
        string cpoId, CancellationToken cancellationToken = default);
    ValueTask<bool> RemoveCpoTokenAsync(
        string cpoId, CancellationToken cancellationToken = default);
}
```

### TokenEntry

```csharp
public sealed record TokenEntry(
    string TokenHash,
    TokenPurpose Purpose,
    string PartyId);
```

### TokenPurpose

```csharp
public enum TokenPurpose
{
    TokenA,  // Pre-shared registration token
    TokenB,  // Ongoing communication token
}
```

{: .note }
> Token C (credential rotation) reuses `TokenPurpose.TokenB` since it replaces Token B and serves the same purpose once the rotation is complete.

### OcpiTokenValidator

Validates incoming request tokens against the store using constant-time comparison:

```csharp
public sealed class OcpiTokenValidator
{
    public OcpiTokenValidator(ITokenStore tokenStore);
    public ValueTask<TokenValidationResult> ValidateAsync(
        string rawToken, CancellationToken cancellationToken = default);
}

public sealed record TokenValidationResult
{
    public bool IsValid { get; }
    public TokenEntry? Entry { get; }
    public string? Error { get; }

    public static TokenValidationResult Valid(TokenEntry entry);
    public static TokenValidationResult Failed(string error);
}
```

### ITokenProtector

Encrypts outbound CPO tokens for at-rest storage:

```csharp
public interface ITokenProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedData);
}
```

`AddAspNetCoreServer()` registers a Data Protection-backed implementation automatically. The default `PlaintextTokenProtector` stores tokens unencrypted (development only). Use `AddTokenProtector<T>()` for a custom implementation.

### Built-in Implementation

```csharp
// In-memory (development only)
dotOcpiBuilder.AddInMemoryTokenStore();
```

### Custom Implementation

```csharp
// Register your implementation
dotOcpiBuilder.AddTokenStore<MyTokenStore>();

public class MyTokenStore : ITokenStore
{
    public async ValueTask StoreAsync(
        string tokenHash, TokenPurpose purpose, string partyId,
        CancellationToken cancellationToken)
    {
        // Store in Azure Key Vault, database, etc.
    }

    public async ValueTask<TokenEntry?> FindAsync(
        string tokenHash, CancellationToken cancellationToken)
    {
        // Look up by hash
    }

    public async ValueTask<bool> RemoveAsync(
        string tokenHash, CancellationToken cancellationToken)
    {
        // Remove token hash
    }

    // Override RotateTokenAsync for transactional rotation in database-backed stores
    // Override StoreCpoTokenAsync/GetCpoTokenAsync/RemoveCpoTokenAsync for persistent outbound token storage
}
```

---

## Authentication Pipeline

DotOcpi authenticates every incoming CPO request:

```
Request → Extract Authorization header → Parse "Token {value}"
       → SHA-256 hash → ITokenStore.FindAsync(hash)
       → FixedTimeEquals comparison → Accept/Reject
```

### Rejection Responses

| Condition | HTTP | OCPI Status | Message |
|:----------|:-----|:------------|:--------|
| Missing or malformed `Authorization` header | 401 | 2002 | Missing or malformed Authorization header. |
| Invalid token (hash not found) | 401 | 2002 | Invalid or unrecognized token. |
| Token valid but no CPO connection in registry | 401 | 2002 | No CPO connection associated with this token. |

---

## Security Rules

DotOcpi enforces these security rules:

| Rule | Enforcement |
|:-----|:------------|
| CSPRNG for all tokens | `RandomNumberGenerator`, never `Random` |
| Constant-time comparison | `CryptographicOperations.FixedTimeEquals` |
| Hash-only storage | Raw tokens never persisted; only SHA-256 hashes |
| HTTPS enforcement | HTTP URLs rejected during registration |
| No tokens in logs | `SanitizeTokensInLogs` defaults to `true` |
| No stack traces in responses | Exception middleware strips internals |
| Minimum token entropy | 64 bytes (512 bits) |
| Token A single-use | Consumed during POST /credentials |

---

## HTTPS Enforcement

All OCPI endpoint URLs must use HTTPS:

```csharp
// This throws OcpiConfigurationException
options.BaseUrl = new Uri("http://my-emsp.com/ocpi");

// This works
options.BaseUrl = new Uri("https://my-emsp.com/ocpi");
```

During registration, CPO-provided endpoint URLs are also validated for HTTPS.

{: .tip }
> In the `Development` environment, HTTP is allowed for local testing.
