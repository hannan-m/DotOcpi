---
title: Locations
layout: default
parent: Modules
nav_order: 2
---

# Locations Module
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Overview

The Locations module manages charging station data. As an eMSP, you:
- **Receive** location pushes from CPOs (PUT/PATCH)
- **Pull** location data from CPOs (GET with pagination)

## Data Model

A Location contains EVSEs, which contain Connectors:

```
Location
├── EVSE 1
│   ├── Connector A
│   └── Connector B
└── EVSE 2
    └── Connector C
```

### V2.2.1 Location (most complete)

```csharp
using DotOcpi.Models.V2_2_1;

var location = new Location
{
    CountryCode = new CiString("DE"),
    PartyId = new CiString("CPO"),
    Id = new CiString("LOC001"),
    Publish = true,
    Name = "Downtown Charging Station",
    Address = "123 Main Street",
    City = "Berlin",
    Country = "DEU",
    Coordinates = new GeoLocation("52.5200", "13.4050"),
    TimeZone = "Europe/Berlin",
    Evses =
    [
        new Evse
        {
            Uid = new CiString("EVSE001"),
            Status = Status.AVAILABLE,
            Connectors =
            [
                new Connector
                {
                    Id = new CiString("CONN001"),
                    Standard = ConnectorType.IEC_62196_T2,
                    Format = ConnectorFormat.SOCKET,
                    PowerType = PowerType.AC_3_PHASE,
                    MaxVoltage = 400,
                    MaxAmperage = 32,
                },
            ],
        },
    ],
    LastUpdated = DateTimeOffset.UtcNow,
};
```

## Receiving Location Pushes

### PUT (Create or Replace)

CPOs send PUT requests for full location/EVSE/connector updates:

```csharp
public class MyLocationsReceiver : ILocationsReceiver
{
    public async Task<OcpiResult> OnLocationPutAsync(
        OcpiRequestContext context, string locationId, object data, CancellationToken ct)
    {
        // Full location replacement
        await _db.UpsertLocationAsync(context.CpoId, locationId, data, ct);
        return OcpiResult.Success();
    }

    public async Task<OcpiResult> OnEvsePutAsync(
        OcpiRequestContext context, string locationId, string evseUid,
        object data, CancellationToken ct)
    {
        // Full EVSE replacement within a location
        await _db.UpsertEvseAsync(context.CpoId, locationId, evseUid, data, ct);
        return OcpiResult.Success();
    }

    public async Task<OcpiResult> OnConnectorPutAsync(
        OcpiRequestContext context, string locationId, string evseUid,
        string connectorId, object data, CancellationToken ct)
    {
        // Full connector replacement within an EVSE
        await _db.UpsertConnectorAsync(context.CpoId, locationId, evseUid, connectorId, data, ct);
        return OcpiResult.Success();
    }
}
```

### PATCH (Partial Update)

CPOs send PATCH requests for partial updates. DotOcpi delivers the raw JSON:

```csharp
public async Task<OcpiResult> OnLocationPatchAsync(
    OcpiRequestContext context, string locationId, JsonElement patch, CancellationToken ct)
{
    // Example patch: { "name": "Updated Name", "last_updated": "2024-01-01T00:00:00Z" }
    var existing = await _db.GetLocationJsonAsync(context.CpoId, locationId, ct);

    if (existing is null)
        return OcpiResult.Failure(new OcpiStatusCode(2003), "Location not found");

    // Apply JSON Merge Patch (RFC 7396) — consumers handle merge in their own domain layer
    var merged = ApplyJsonMergePatch(existing.Value, patch);
    await _db.SaveLocationJsonAsync(context.CpoId, locationId, merged, ct);

    return OcpiResult.Success();
}
```

## Pulling Locations

### Get All Locations

```csharp
// Paginated — follows Link headers automatically
await foreach (var location in ocpiClient.Locations.GetAllLocationsAsync("DE:CPO"))
{
    Console.WriteLine($"{location}");
}
```

### Incremental Pull

```csharp
// Only locations updated since last sync
var since = DateTimeOffset.UtcNow.AddHours(-1);
await foreach (var location in ocpiClient.Locations.GetAllLocationsAsync(
    "DE:CPO", dateFrom: since))
{
    await ProcessUpdatedLocationAsync(location);
}
```

### Get Single Location

```csharp
var result = await ocpiClient.Locations.GetLocationAsync("DE:CPO", "LOC001");
if (result.IsSuccess)
{
    var location = result.Data;
}
```

## URL Patterns by Version

| Version | Location | EVSE | Connector |
|:--------|:---------|:-----|:----------|
| 2.0, 2.1.1 | `/locations/{location_id}` | `/locations/{location_id}/{evse_uid}` | `/locations/{location_id}/{evse_uid}/{connector_id}` |
| 2.2, 2.2.1 | `/locations/{cc}/{pid}/{location_id}` | `/locations/{cc}/{pid}/{location_id}/{evse_uid}` | `/locations/{cc}/{pid}/{location_id}/{evse_uid}/{connector_id}` |

DotOcpi routes both URL patterns to the same handler — you don't need to handle the difference.

## Version-Specific Fields

| Field | 2.0 | 2.1.1 | 2.2 | 2.2.1 |
|:------|:----|:------|:----|:------|
| `country_code` / `party_id` | - | - | Yes | Yes |
| `publish` | - | - | Yes | Yes |
| `last_updated` | - | Yes | Yes | Yes |
| `energy_mix` | - | Yes | Yes | Yes |
| `facilities` | - | Yes | Yes | Yes |
| `time_zone` | - | Yes | Yes | Yes |

## Error Handling

### Return Codes

| Scenario | Return | What the CPO sees |
|:---------|:-------|:------------------|
| Location stored successfully | `OcpiResult.Success()` | HTTP 200, OCPI 1000 |
| Location not found (GET/PATCH) | `OcpiResult.Failure(OcpiStatusCode.UnknownLocation, "...")` | HTTP 200, OCPI 2003 |
| Invalid data | `OcpiResult.Failure(OcpiStatusCode.InvalidParameters, "...")` | HTTP 200, OCPI 2001 |
| Unhandled exception in handler | Caught by middleware | HTTP 500, OCPI 3000 (no details exposed) |

{: .tip }
> OCPI always returns HTTP 200 for business-level responses. The OCPI status code inside the response body indicates success (1xxx) or error (2xxx/3xxx). HTTP 4xx/5xx is only for transport-level errors (auth, rate limiting, server crash).
