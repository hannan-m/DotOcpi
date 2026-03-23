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
    ValueTask StoreAsync(
        string tokenHash, TokenPurpose purpose, string partyId, CancellationToken ct);

    ValueTask<TokenEntry?> FindAsync(
        string tokenHash, CancellationToken ct);

    ValueTask<bool> RemoveAsync(
        string tokenHash, CancellationToken ct);
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
    TokenC,  // Credential rotation token
}
```

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
        string tokenHash, TokenPurpose purpose, string partyId, CancellationToken ct)
    {
        // Store in Azure Key Vault, database, etc.
    }

    public async ValueTask<TokenEntry?> FindAsync(string tokenHash, CancellationToken ct)
    {
        // Look up by hash
    }

    public async ValueTask<bool> RemoveAsync(string tokenHash, CancellationToken ct)
    {
        // Remove token hash
    }
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
| Missing `Authorization` header | 401 | 2002 | Not enough information |
| Malformed header (not `Token xxx`) | 401 | 2002 | Invalid authorization format |
| Invalid token (hash not found) | 401 | 2002 | Invalid or expired token |
| Wrong token purpose | 401 | 2002 | Token not authorized for this operation |

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
