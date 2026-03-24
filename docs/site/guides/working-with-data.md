---
title: Working with Data
layout: default
parent: Guides
nav_order: 7
---

# Working with OCPI Data
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## How Data Arrives

OCPI data reaches your code through two paths:

| Path | Interface | Data parameter | When |
|:-----|:----------|:---------------|:-----|
| **Push** | `ILocationsReceiver`, etc. | `object data` or `JsonElement patch` | CPO sends PUT/PATCH to your endpoints |
| **Pull** | `IOcpiSyncHandler` | `IReadOnlyList<object> items` | Background sync fetches from CPO |

In both cases, the `object` parameter contains a **version-specific model** — the actual runtime type depends on the OCPI version negotiated with that CPO.

---

## Casting to Version-Specific Models

The `object data` parameter in your handlers is already deserialized to the correct version-specific model. Cast it using the negotiated version from the context:

```csharp
public Task<OcpiResult> OnLocationPutAsync(
    OcpiRequestContext context, string locationId, object data, CancellationToken ct)
{
    switch (context.NegotiatedVersion)
    {
        case OcpiVersion.V2_2_1:
            var loc221 = (DotOcpi.Models.V2_2_1.Location)data;
            // loc221.CountryCode, loc221.PartyId, loc221.Publish, etc.
            break;

        case OcpiVersion.V2_1_1:
            var loc211 = (DotOcpi.Models.V2_1_1.Location)data;
            // No CountryCode/PartyId/Publish in 2.1.1
            break;
    }

    return Task.FromResult(OcpiResult.Success());
}
```

### Version-Agnostic Storage

If you don't need version-specific fields, store the `object` directly and defer deserialization:

```csharp
public Task<OcpiResult> OnLocationPutAsync(
    OcpiRequestContext context, string locationId, object data, CancellationToken ct)
{
    // Store the raw object — it's a version-specific model under the hood
    _store[Key(context.CpoId, locationId)] = data;
    return Task.FromResult(OcpiResult.Success());
}
```

This is the pattern the sample project uses. The object retains its version-specific type and can be cast later when needed.

---

## JSON Serialization

### OcpiJsonOptions

Use `OcpiJsonOptions.GetOptions(version)` to get pre-configured `JsonSerializerOptions` for any OCPI version:

```csharp
using DotOcpi.Serialization;

// Serialize to OCPI wire format (snake_case)
var options = OcpiJsonOptions.GetOptions(OcpiVersion.V2_2_1);
var json = JsonSerializer.Serialize(locationObject, options);
// {"id":"LOC001","name":"Downtown Station","country_code":"DE",...}

// Deserialize from wire format
var location = JsonSerializer.Deserialize<Models.V2_2_1.Location>(json, options);
```

Each version's options include:
- `PropertyNamingPolicy = SnakeCaseLower` (OCPI wire format)
- `DefaultIgnoreCondition = WhenWritingNull` (omit null fields)
- `MaxDepth = 32` (security limit)
- Custom converters for `DateTimeOffset` (RFC 3339), `CiString`, and `GeoLocation`

### Converting Stored Objects to JSON

When you need to return stored data as JSON (e.g., for an API endpoint):

```csharp
// Get the version from the CPO's connection
var connection = registry.FindByConnectionKey("DE:CPO");
var options = OcpiJsonOptions.GetOptions(connection!.Version);

// Serialize the stored object to OCPI-compliant JSON
var json = JsonSerializer.Serialize(storedLocation, options);
```

### Working with JsonElement

For inspecting data without knowing the model type:

```csharp
// Convert any object to JsonElement for property access
var element = JsonSerializer.SerializeToElement(data);

// Access properties (snake_case wire format)
if (element.TryGetProperty("id", out var id))
{
    var locationId = id.GetString();
}

// Or PascalCase (if serialized with default options)
if (element.TryGetProperty("Id", out var idPascal))
{
    // Handle CiString which serializes as { "Value": "..." }
    var value = idPascal.ValueKind == JsonValueKind.String
        ? idPascal.GetString()
        : idPascal.GetProperty("Value").GetString();
}
```

---

## Handling PATCH Requests

PATCH handlers receive `JsonElement` — raw JSON representing the partial update. You're responsible for applying the merge:

```csharp
public Task<OcpiResult> OnLocationPatchAsync(
    OcpiRequestContext context, string locationId, JsonElement patch, CancellationToken ct)
{
    var key = Key(context.CpoId, locationId);
    if (!_store.TryGetValue(key, out var existing))
        return Task.FromResult(OcpiResult.Failure(OcpiStatusCode.UnknownLocation, "Not found"));

    // Option 1: Serialize existing to JSON, merge, deserialize back
    var options = OcpiJsonOptions.GetOptions(context.NegotiatedVersion);
    var existingJson = JsonSerializer.SerializeToElement(existing, options);
    var merged = ApplyJsonMergePatch(existingJson, patch);

    // Deserialize merged JSON back to the correct model type
    var updatedLocation = context.NegotiatedVersion switch
    {
        OcpiVersion.V2_2_1 => (object)merged.Deserialize<Models.V2_2_1.Location>(options)!,
        OcpiVersion.V2_1_1 => merged.Deserialize<Models.V2_1_1.Location>(options)!,
        // ... other versions
        _ => throw new InvalidOperationException()
    };

    _store[key] = updatedLocation;
    return Task.FromResult(OcpiResult.Success());
}
```

{: .note }
> DotOcpi does not include a JSON Merge Patch (RFC 7396) implementation. Use `System.Text.Json` to merge `JsonElement` values, or use a library like `Json.More.Net`.

---

## Serving Data Back to CPOs

When a CPO sends a GET request, your handler must return the stored data:

```csharp
public Task<OcpiResult<object>> GetLocationAsync(
    OcpiRequestContext context, string locationId, CancellationToken ct)
{
    var key = Key(context.CpoId, locationId);

    if (_store.TryGetValue(key, out var location))
        return Task.FromResult(OcpiResult<object>.Success(location));

    return Task.FromResult(
        OcpiResult<object>.Failure(OcpiStatusCode.UnknownLocation, "Location not found"));
}
```

DotOcpi serializes the returned `object` using the correct version-specific JSON options automatically.

---

## Pull Sync Data

When using pull sync, data arrives through `IOcpiSyncHandler`:

```csharp
public class MySyncHandler : IOcpiSyncHandler
{
    private readonly MyDatabase _db;

    public async Task OnPageReceivedAsync(
        SyncContext context,
        IReadOnlyList<object> items,
        CancellationToken cancellationToken = default)
    {
        // items contains version-specific models
        // context.Version tells you which version
        // context.ModuleId tells you which module ("locations", "sessions", etc.)

        foreach (var item in items)
        {
            var id = ExtractId(item);
            if (id is not null)
                await _db.UpsertAsync(context.ModuleId, context.CpoId, id, item, cancellationToken);
        }
    }

    public Task OnSyncCompletedAsync(
        SyncContext context,
        SyncResult result,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Synced {Module} from {CpoId}: {Items} items, {Pages} pages in {Duration}",
            context.ModuleId, context.CpoId,
            result.ItemCount, result.PageCount, result.Duration);

        return Task.CompletedTask;
    }
}
```

Register your handler:

```csharp
builder.Services.AddDotOcpi(options => { /* ... */ })
    .AddClient()
    .AddSyncHandler<MySyncHandler>()
    .AddPullSync(sync =>
    {
        sync.DefaultInterval = TimeSpan.FromHours(1);
        sync.EnabledModules = ["locations", "tariffs"];
    });
```

See [Sync API Reference](/DotOcpi/api-reference/sync/) for the full `SyncContext` and `SyncResult` types.

---

## Common Patterns

### Extracting IDs from Raw Objects

When you need to extract an ID without casting to a specific model:

```csharp
public static string? ExtractId(object data)
{
    var json = JsonSerializer.SerializeToElement(data);

    // Check both snake_case (OCPI wire format) and PascalCase
    if (json.TryGetProperty("id", out var id) || json.TryGetProperty("Id", out id))
    {
        return id.ValueKind switch
        {
            JsonValueKind.String => id.GetString(),
            // CiString serializes as { "Value": "..." } with default options
            JsonValueKind.Object when id.TryGetProperty("Value", out var v) => v.GetString(),
            _ => id.ToString(),
        };
    }

    return null;
}
```

### Composite Storage Keys

Store data by CPO + object ID to keep CPO data isolated:

```csharp
// Pattern: "{cpoId}:{objectId}"
var key = $"{context.CpoId}:{locationId}";
_store[key] = data;
```

### Version-Aware Querying

When you need to query stored data and the version matters:

```csharp
public IReadOnlyList<object> GetLocationsForCpo(string cpoId, OcpiVersion version)
{
    var prefix = $"{cpoId}:";
    return _store
        .Where(kvp => kvp.Key.StartsWith(prefix))
        .Select(kvp => kvp.Value)
        .ToList();
}
```

---

## Source-Generated Serialization

DotOcpi uses source-generated `JsonSerializerContext` classes for each OCPI version. This means:

- No runtime reflection for JSON serialization
- AOT-compatible (works with Native AOT / trimming)
- Faster startup and serialization

The contexts are wired automatically into `OcpiJsonOptions`. You don't need to reference them directly unless building custom serialization logic.

| Context | Namespace |
|:--------|:----------|
| `OcpiJsonContext_V2_0` | `DotOcpi.Serialization` |
| `OcpiJsonContext_V2_1_1` | `DotOcpi.Serialization` |
| `OcpiJsonContext_V2_2` | `DotOcpi.Serialization` |
| `OcpiJsonContext_V2_2_1` | `DotOcpi.Serialization` |

---

<div style="display: flex; justify-content: space-between; margin-top: 2rem;">
  <div>← <a href="/DotOcpi/guides/multi-version/">Multi-Version Support</a></div>
  <div><a href="/DotOcpi/guides/error-handling/">Error Handling</a> →</div>
</div>
