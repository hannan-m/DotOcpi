---
title: Token Management
layout: default
parent: Guides
nav_order: 5
---

# Token Management
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Token Lifecycle

OCPI uses three token types for authentication between parties:

```mermaid
flowchart LR
    A[Token A<br/>Pre-shared] -->|Registration| B[Token B<br/>Active]
    B -->|Rotation| C[Token C<br/>Replacement]
    C -->|Replaces| B2[Token B<br/>New Active]
```

| Token | Purpose | Lifespan |
|:------|:--------|:---------|
| **Token A** | Pre-shared between parties out-of-band. Used only for initial registration. | Single use — consumed during `POST /credentials` |
| **Token B** | Issued during credentials exchange. Used for all ongoing authenticated requests. | Until rotation or unregistration |
| **Token C** | Issued during credential rotation (`PUT /credentials`). Replaces Token B. | Immediately becomes the new Token B |

## Security Model

DotOcpi enforces strict security rules for token handling:

### Generation

Tokens are generated using cryptographically secure randomness:

```csharp
// 64 bytes of CSPRNG output, base64url-encoded (no padding)
var token = TokenGenerator.Generate();
// Example: "dGhpcyBpcyBhIHRlc3QgdG9rZW4gd2l0aCBzaXh0eS1mb3VyIGJ5dGVzIG9mIH..."
```

- Minimum 64 bytes of entropy
- Uses `RandomNumberGenerator` (platform CSPRNG)
- Base64url-encoded (URL-safe, no padding)

### Storage

**Token B (inbound):** Only SHA-256 hashes are stored. Raw tokens are never persisted:

```csharp
// DotOcpi stores this hash, not the raw token
var hash = TokenHasher.Hash(rawToken);
// Example: "a3f2b8c9d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1"
```

**Token C (outbound):** Stored encrypted at rest via `ITokenProtector`, then persisted in `ITokenStore`. The library stores and retrieves Token C automatically during registration and rotation — no consumer action required.

In ASP.NET Core deployments, `AddAspNetCoreServer()` registers a Data Protection-backed `ITokenProtector` automatically. For custom encryption, use `AddTokenProtector<T>()`.

### Validation

Incoming requests are authenticated by:

1. Parsing the `Authorization: Token <base64>` header (Span-based, zero allocation)
2. Hashing the provided token with SHA-256
3. Looking up the hash in `ITokenStore`
4. Constant-time comparison using `CryptographicOperations.FixedTimeEquals`

```
Authorization: Token dGhpcyBpcyBhIHRlc3QgdG9rZW4...
                      ↓
              SHA-256 hash
                      ↓
         ITokenStore.FindAsync(hash)
                      ↓
    FixedTimeEquals(stored, computed) → match/reject
```

{: .warning }
> DotOcpi uses **constant-time comparison** for all token validations. This prevents timing attacks that could leak information about valid tokens.

## ITokenStore

The `ITokenStore` interface abstracts token hash storage:

```csharp
public interface ITokenStore
{
    // Inbound token hash storage (Token B)
    ValueTask StoreAsync(string tokenHash, TokenPurpose purpose, string partyId,
        CancellationToken cancellationToken = default);
    ValueTask<TokenEntry?> FindAsync(string tokenHash, CancellationToken cancellationToken = default);
    ValueTask<bool> RemoveAsync(string tokenHash, CancellationToken cancellationToken = default);

    // Atomic rotation: stores new hash, then removes old (has default implementation)
    ValueTask RotateTokenAsync(string oldTokenHash, string newTokenHash, TokenPurpose purpose,
        string partyId, CancellationToken cancellationToken = default);

    // Outbound CPO token storage (Token C) — protected via ITokenProtector
    // Default implementations are no-op; override for persistent storage
    ValueTask StoreCpoTokenAsync(string cpoId, string protectedToken,
        CancellationToken cancellationToken = default);
    ValueTask<string?> GetCpoTokenAsync(string cpoId, CancellationToken cancellationToken = default);
    ValueTask<bool> RemoveCpoTokenAsync(string cpoId, CancellationToken cancellationToken = default);
}
```

### Built-in: InMemoryTokenStore

For development and testing:

```csharp
builder.Services.AddDotOcpi(options => { /* ... */ })
    .AddInMemoryTokenStore();
```

{: .warning }
> `InMemoryTokenStore` loses all tokens on application restart. Use a persistent implementation for production.

### Production Implementation

See [Custom Token Store](/DotOcpi/advanced/custom-token-store/) for implementing `ITokenStore` with Azure Key Vault, HashiCorp Vault, or a database.

## Token Rotation

Rotate credentials without disrupting service:

```csharp
var result = await registrationClient.RotateCredentialsAsync(new CredentialRotationRequest(
    ConnectionKey: "DE:CPO",
    CurrentCpoToken: currentTokenB,
    EmspBusinessName: "My eMSP"
));

// Old Token B is invalidated
// result.CpoToken is the new Token B
```

The rotation flow:

1. Generate a new Token B (replacing the current one)
2. `PUT /credentials` to the CPO with the new Token B
3. CPO validates the request using the current Token C
4. CPO responds with a new Token C
5. New Token B hash is stored, registry updated, old hash removed
6. New Token C is protected via `ITokenProtector` and stored in `ITokenStore`

## Pushing Tokens to CPOs

The **Tokens module** (not to be confused with auth tokens) lets you push EV driver tokens to CPOs:

```csharp
// Push a token to a CPO
await ocpiClient.Tokens.PushTokenAsync("DE:CPO", "TOKEN001", new
{
    uid = "TOKEN001",
    type = "RFID",
    contract_id = "NL-MSP-C001",
    issuer = "My eMSP",
    valid = true,
    whitelist = "ALLOWED",
    last_updated = DateTimeOffset.UtcNow,
});

// Patch a token
await ocpiClient.Tokens.PatchTokenAsync("DE:CPO", "TOKEN001",
    JsonSerializer.SerializeToElement(new { valid = false }));
```

## Real-Time Token Authorization

CPOs can authorize tokens in real-time by calling your `POST /tokens/{uid}/authorize` endpoint. Implement `ITokensAuthorizer`:

```csharp
public class MyTokensAuthorizer : ITokensAuthorizer
{
    public async Task<OcpiResult<object>> AuthorizeAsync(
        OcpiRequestContext context, string tokenUid, object? locationReferences, CancellationToken ct)
    {
        // Check if the token is valid for the given location
        var isAuthorized = await CheckAuthorizationAsync(tokenUid, locationReferences, ct);

        if (isAuthorized)
        {
            return OcpiResult<object>.Success(new
            {
                allowed = "ALLOWED",
                token = new { uid = tokenUid, type = "RFID" },
            });
        }

        return OcpiResult<object>.Success(new
        {
            allowed = "NOT_ALLOWED",
            token = new { uid = tokenUid, type = "RFID" },
        });
    }
}
```

## Providing Tokens for Paginated Retrieval

CPOs can also pull your tokens via `GET /tokens`. Implement `ITokensSender`:

```csharp
public class MyTokensSender : ITokensSender
{
    public async Task<PaginatedResult<object>> GetTokensAsync(
        OcpiRequestContext context,
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        int offset,
        int limit,
        CancellationToken ct)
    {
        // Query your token database with pagination
        var tokens = await QueryTokensAsync(dateFrom, dateTo, offset, limit, ct);

        return new PaginatedResult<object>
        {
            Items = tokens,
            TotalCount = await CountTokensAsync(dateFrom, dateTo, ct),
            Offset = offset,
            Limit = limit,
        };
    }
}
```

---

<div style="display: flex; justify-content: space-between; margin-top: 2rem;">
  <div>← <a href="/DotOcpi/guides/sending-commands/">Sending Commands</a></div>
  <div><a href="/DotOcpi/guides/multi-version/">Multi-Version Support</a> →</div>
</div>
