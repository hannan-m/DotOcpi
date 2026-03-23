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
    ValueTask StoreAsync(
        string tokenHash, TokenPurpose purpose, string partyId, CancellationToken ct);

    ValueTask<TokenEntry?> FindAsync(
        string tokenHash, CancellationToken ct);

    ValueTask<bool> RemoveAsync(
        string tokenHash, CancellationToken ct);
}
```

{: .important }
> `ITokenStore` stores **hashes**, not raw tokens. The raw token is never passed to or returned from the store.

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

## Registration

```csharp
builder.Services.AddDotOcpi(options => { /* ... */ })
    .AddTokenStore<SqlTokenStore>();

// Or with factory
builder.Services.AddDotOcpi(options => { /* ... */ })
    .AddTokenStore<KeyVaultTokenStore>();

// The store is registered as Singleton
```

## Performance Considerations

- `FindAsync` is on the **hot path** — called for every incoming request
- Use `ValueTask` (not `Task`) — the interface requires it for cache-friendly returns
- Consider adding a local cache with short TTL for frequently accessed hashes
- Use connection pooling for database implementations
