---
title: Tariffs
layout: default
parent: Modules
nav_order: 5
---

# Tariffs Module
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Overview

The Tariffs module manages pricing information for charging stations. CPOs push tariff data to your eMSP so you can display prices to EV drivers.

## Receiving Tariff Pushes

```csharp
public class MyTariffsReceiver : ITariffsReceiver
{
    public async Task<OcpiResult> OnTariffPutAsync(
        OcpiRequestContext context, string tariffId, object data, CancellationToken ct)
    {
        await _db.UpsertTariffAsync(context.CpoId, tariffId, data, ct);
        return OcpiResult.Success();
    }

    public async Task<OcpiResult> OnTariffPatchAsync(
        OcpiRequestContext context, string tariffId, JsonElement patch, CancellationToken ct)
    {
        // Only called for OCPI 2.0 and 2.1.1
        // OCPI 2.2+ returns 405 Method Not Allowed automatically
        var existing = await _db.GetTariffJsonAsync(context.CpoId, tariffId, ct);
        if (existing is null)
            return OcpiResult.Failure(new OcpiStatusCode(2003), "Tariff not found");

        // Apply JSON Merge Patch (RFC 7396) — consumers handle merge
        var merged = ApplyJsonMergePatch(existing.Value, patch);
        await _db.SaveTariffJsonAsync(context.CpoId, tariffId, merged, ct);
        return OcpiResult.Success();
    }

    public async Task<OcpiResult> OnTariffDeleteAsync(
        OcpiRequestContext context, string tariffId, CancellationToken ct)
    {
        await _db.DeleteTariffAsync(context.CpoId, tariffId, ct);
        return OcpiResult.Success();
    }

    public async Task<OcpiResult<object>> GetTariffAsync(
        OcpiRequestContext context, string tariffId, CancellationToken ct)
    {
        var tariff = await _db.GetTariffAsync(context.CpoId, tariffId, ct);
        if (tariff is null)
            return OcpiResult<object>.Failure(new OcpiStatusCode(2003), "Tariff not found");

        return OcpiResult<object>.Success(tariff);
    }
}
```

## Pulling Tariffs

```csharp
await foreach (var tariff in ocpiClient.Tariffs.GetAllTariffsAsync("DE:CPO"))
{
    Console.WriteLine($"Tariff: {tariff}");
}
```

## PATCH Behavior by Version

{: .important }
> Tariff PATCH is only supported in OCPI 2.0 and 2.1.1. In OCPI 2.2 and 2.2.1, PATCH requests to tariff endpoints return **405 Method Not Allowed** automatically. CPOs must use PUT for full replacement in 2.2+.

| Version | PUT | PATCH | DELETE |
|:--------|:----|:------|:-------|
| 2.0 | Yes | Yes | Yes |
| 2.1.1 | Yes | Yes | Yes |
| 2.2 | Yes | **405** | Yes |
| 2.2.1 | Yes | **405** | Yes |

## Tariff Structure

A tariff contains pricing elements with restrictions:

```json
{
  "id": "TAR001",
  "currency": "EUR",
  "elements": [
    {
      "price_components": [
        {
          "type": "ENERGY",
          "price": 0.25,
          "step_size": 1
        },
        {
          "type": "TIME",
          "price": 0.05,
          "step_size": 60
        }
      ],
      "restrictions": {
        "start_time": "08:00",
        "end_time": "20:00",
        "day_of_week": ["MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY"]
      }
    }
  ],
  "last_updated": "2024-01-15T10:00:00Z"
}
```

### Price Component Types

| Type | Description |
|:-----|:------------|
| `ENERGY` | Per kWh |
| `FLAT` | Fixed fee per session |
| `PARKING_TIME` | Per time unit while parked |
| `TIME` | Per time unit while charging |

## Error Handling

| Scenario | Return |
|:---------|:-------|
| Tariff stored/updated | `OcpiResult.Success()` |
| Tariff deleted | `OcpiResult.Success()` |
| Unknown tariff (GET/PATCH/DELETE) | `OcpiResult.Failure(OcpiStatusCode.UnknownLocation, "Tariff not found")` |
| PATCH on 2.2+ (not supported) | DotOcpi returns HTTP 405 automatically |
