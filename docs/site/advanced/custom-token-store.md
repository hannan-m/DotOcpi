---
title: Custom Token Store
layout: default
parent: Advanced
nav_order: 1
---

# Custom Token Store
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Why Custom?

The built-in `InMemoryTokenStore` loses all tokens on restart. For production, implement `ITokenStore` with your preferred backend.

## ITokenStore Interface

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

    // Atomic rotation (has default implementation — override for transactional behavior)
    ValueTask RotateTokenAsync(
        string oldTokenHash, string newTokenHash, TokenPurpose purpose, string partyId,
        CancellationToken cancellationToken = default);

    // Outbound CPO token storage (Token C) — protected via ITokenProtector before storage
    // Default implementations are no-op; override for persistent storage
    ValueTask StoreCpoTokenAsync(
        string cpoId, string protectedToken, CancellationToken cancellationToken = default);
    ValueTask<string?> GetCpoTokenAsync(
        string cpoId, CancellationToken cancellationToken = default);
    ValueTask<bool> RemoveCpoTokenAsync(
        string cpoId, CancellationToken cancellationToken = default);
}
```

{: .important }
> The inbound methods store **SHA-256 hashes** of Token B — raw tokens are never passed to the store. The outbound methods store **protected** (encrypted) Token C values — the library calls `ITokenProtector.Protect()` before storing and `ITokenProtector.Unprotect()` after retrieval. The CPO token methods have no-op default implementations; override them to enable automatic outbound token management.

## Example: SQL Server

```csharp
public class SqlTokenStore : ITokenStore
{
    private readonly IDbConnectionFactory _dbFactory;

    public SqlTokenStore(IDbConnectionFactory dbFactory) => _dbFactory = dbFactory;

    public async ValueTask StoreAsync(
        string tokenHash, TokenPurpose purpose, string partyId, CancellationToken ct)
    {
        using var db = await _dbFactory.CreateConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            MERGE INTO OcpiTokens AS target
            USING (SELECT @Hash AS TokenHash) AS source
            ON target.TokenHash = source.TokenHash
            WHEN MATCHED THEN
                UPDATE SET Purpose = @Purpose, PartyId = @PartyId, UpdatedAt = GETUTCDATE()
            WHEN NOT MATCHED THEN
                INSERT (TokenHash, Purpose, PartyId, CreatedAt, UpdatedAt)
                VALUES (@Hash, @Purpose, @PartyId, GETUTCDATE(), GETUTCDATE());
            """,
            new { Hash = tokenHash, Purpose = purpose.ToString(), PartyId = partyId });
    }

    public async ValueTask<TokenEntry?> FindAsync(string tokenHash, CancellationToken ct)
    {
        using var db = await _dbFactory.CreateConnectionAsync(ct);
        var row = await db.QuerySingleOrDefaultAsync<TokenRow>(
            "SELECT TokenHash, Purpose, PartyId FROM OcpiTokens WHERE TokenHash = @Hash",
            new { Hash = tokenHash });

        if (row is null) return null;

        return new TokenEntry(
            row.TokenHash,
            Enum.Parse<TokenPurpose>(row.Purpose),
            row.PartyId);
    }

    public async ValueTask<bool> RemoveAsync(string tokenHash, CancellationToken ct)
    {
        using var db = await _dbFactory.CreateConnectionAsync(ct);
        var rows = await db.ExecuteAsync(
            "DELETE FROM OcpiTokens WHERE TokenHash = @Hash",
            new { Hash = tokenHash });
        return rows > 0;
    }
}
```

## Example: Azure Key Vault

```csharp
public class KeyVaultTokenStore : ITokenStore
{
    private readonly SecretClient _client;

    public KeyVaultTokenStore(SecretClient client) => _client = client;

    public async ValueTask StoreAsync(
        string tokenHash, TokenPurpose purpose, string partyId, CancellationToken ct)
    {
        var secretName = $"ocpi-token-{tokenHash[..16]}";
        var value = JsonSerializer.Serialize(new { tokenHash, purpose, partyId });
        await _client.SetSecretAsync(secretName, value, ct);
    }

    public async ValueTask<TokenEntry?> FindAsync(string tokenHash, CancellationToken ct)
    {
        var secretName = $"ocpi-token-{tokenHash[..16]}";
        try
        {
            var secret = await _client.GetSecretAsync(secretName, cancellationToken: ct);
            var data = JsonSerializer.Deserialize<TokenData>(secret.Value.Value);
            return new TokenEntry(data!.TokenHash, data.Purpose, data.PartyId);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async ValueTask<bool> RemoveAsync(string tokenHash, CancellationToken ct)
    {
        var secretName = $"ocpi-token-{tokenHash[..16]}";
        try
        {
            await _client.StartDeleteSecretAsync(secretName, ct);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }
}
```

## Adding CPO Token Storage (SQL Example)

The CPO token methods have default no-op implementations. Override them for persistent storage:

```csharp
// Add to your SqlTokenStore class:
public async ValueTask StoreCpoTokenAsync(
    string cpoId, string protectedToken, CancellationToken ct)
{
    using var db = await _dbFactory.CreateConnectionAsync(ct);
    await db.ExecuteAsync(
        """
        MERGE INTO OcpiCpoTokens AS target
        USING (SELECT @CpoId AS CpoId) AS source
        ON target.CpoId = source.CpoId
        WHEN MATCHED THEN UPDATE SET ProtectedToken = @Token, UpdatedAt = GETUTCDATE()
        WHEN NOT MATCHED THEN INSERT (CpoId, ProtectedToken, CreatedAt, UpdatedAt)
            VALUES (@CpoId, @Token, GETUTCDATE(), GETUTCDATE());
        """,
        new { CpoId = cpoId, Token = protectedToken });
}

public async ValueTask<string?> GetCpoTokenAsync(string cpoId, CancellationToken ct)
{
    using var db = await _dbFactory.CreateConnectionAsync(ct);
    return await db.QuerySingleOrDefaultAsync<string>(
        "SELECT ProtectedToken FROM OcpiCpoTokens WHERE CpoId = @CpoId",
        new { CpoId = cpoId });
}

public async ValueTask<bool> RemoveCpoTokenAsync(string cpoId, CancellationToken ct)
{
    using var db = await _dbFactory.CreateConnectionAsync(ct);
    var rows = await db.ExecuteAsync(
        "DELETE FROM OcpiCpoTokens WHERE CpoId = @CpoId",
        new { CpoId = cpoId });
    return rows > 0;
}
```

{: .note }
> The `protectedToken` parameter is already encrypted by `ITokenProtector` before reaching the store. In ASP.NET Core deployments, this uses the Data Protection API by default. The store just persists the opaque string — no additional encryption is needed at the storage layer unless you want defense in depth.

## Registration

```csharp
builder.Services.AddDotOcpi(options => { /* ... */ })
    .AddTokenStore<SqlTokenStore>();

// Or with factory
builder.Services.AddDotOcpi(options => { /* ... */ })
    .AddTokenStore<KeyVaultTokenStore>();

// Custom token protector (optional — AddAspNetCoreServer() provides Data Protection by default)
builder.Services.AddDotOcpi(options => { /* ... */ })
    .AddTokenStore<SqlTokenStore>()
    .AddTokenProtector<MyCustomProtector>();

// The store is registered as Singleton
```

## Performance Considerations

- `FindAsync` is on the **hot path** — called for every incoming request
- Use `ValueTask` (not `Task`) — the interface requires it for cache-friendly returns
- Consider adding a local cache with short TTL for frequently accessed hashes
- Use connection pooling for database implementations
- `GetCpoTokenAsync` is cached by `CpoConnectionContextProvider` — called once per CPO until invalidated
