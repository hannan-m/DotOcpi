---
title: CDRs
layout: default
parent: Modules
nav_order: 4
---

# CDRs Module
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Overview

CDRs (Charge Detail Records) are immutable billing records sent by CPOs after a session completes. They contain the final costs, energy consumed, and charging periods.

## Receiving CDR Pushes

CDRs use **POST** (not PUT) because they're immutable — once created, they don't change:

```csharp
public class MyCdrsReceiver : ICdrsReceiver
{
    public async Task<OcpiResult<CdrPostResult>> OnCdrPostAsync(
        OcpiRequestContext context, object data, CancellationToken ct)
    {
        // Extract CDR ID and check for duplicates
        var cdrId = ExtractCdrId(data);
        var exists = await _db.CdrExistsAsync(context.CpoId, cdrId, ct);

        if (exists)
        {
            // Idempotent: return success but indicate it's not new
            return OcpiResult<CdrPostResult>.Success(
                new CdrPostResult(CdrId: cdrId, IsNew: false));
        }

        await _db.StoreCdrAsync(context.CpoId, cdrId, data, ct);

        return OcpiResult<CdrPostResult>.Success(
            new CdrPostResult(CdrId: cdrId, IsNew: true));
    }

    public async Task<OcpiResult<object>> GetCdrAsync(
        OcpiRequestContext context, string cdrId, CancellationToken ct)
    {
        var cdr = await _db.GetCdrAsync(context.CpoId, cdrId, ct);
        if (cdr is null)
            return OcpiResult<object>.Failure(new OcpiStatusCode(2003), "CDR not found");

        return OcpiResult<object>.Success(cdr);
    }
}
```

## Idempotent POST

CPOs may retry POST requests (e.g., after a network timeout). DotOcpi handles idempotency through `CdrPostResult`:

- `IsNew: true` &rarr; HTTP 201 Created with `Location` header
- `IsNew: false` &rarr; HTTP 200 OK (duplicate, already received)

The `Location` header format differs by version:
- 2.0/2.1.1: `/ocpi/cdrs/{cdr_id}`
- 2.2/2.2.1: `/ocpi/cdrs/{country_code}/{party_id}/{cdr_id}`

## Pulling CDRs

```csharp
// All CDRs
await foreach (var cdr in ocpiClient.Cdrs.GetAllCdrsAsync("DE:CPO"))
{
    await ProcessCdrAsync(cdr);
}

// CDRs from a specific date range
await foreach (var cdr in ocpiClient.Cdrs.GetAllCdrsAsync(
    "DE:CPO",
    dateFrom: DateTimeOffset.UtcNow.AddDays(-7),
    dateTo: DateTimeOffset.UtcNow))
{
    await ProcessCdrAsync(cdr);
}
```

## CDR Data Structure

A CDR contains:

| Field | Description |
|:------|:------------|
| `id` | Unique CDR identifier |
| `start_date_time` | Session start |
| `end_date_time` / `stop_date_time` | Session end (field name varies by version) |
| `total_cost` | Total cost in the specified currency |
| `total_energy` | Total energy consumed (kWh) |
| `total_time` | Total session duration |
| `currency` | ISO 4217 currency code |
| `charging_periods` | Detailed breakdown of charging periods |
| `location` / `cdr_location` | Reference to the charging location |

## Version Differences

| Feature | 2.0 | 2.1.1 | 2.2 | 2.2.1 |
|:--------|:----|:------|:----|:------|
| End time field | `stop_date_time` | `stop_date_time` | `end_date_time` | `end_date_time` |
| Location reference | Embedded | Embedded | `CdrLocation` | `CdrLocation` |
| `last_updated` | - | Yes | Yes | Yes |
| `signed_data` | - | - | Yes | Yes |
| `credit` | - | - | - | Yes |

## Error Handling

CDRs are immutable — they're POSTed once and never updated. The `CdrPostResult` distinguishes between new and duplicate CDRs:

| Scenario | Return |
|:---------|:-------|
| New CDR stored | `OcpiResult<CdrPostResult>.Success(CdrPostResult.Created)` → HTTP 201 |
| Duplicate CDR (already exists) | `OcpiResult<CdrPostResult>.Success(CdrPostResult.AlreadyExists)` → HTTP 200 |
| Invalid CDR data | `OcpiResult<CdrPostResult>.Failure(OcpiStatusCode.InvalidParameters, "...")` |

{: .tip }
> Always check for duplicates before storing. A CPO may retry a POST if it didn't receive your response. Return `CdrPostResult.AlreadyExists` rather than an error.
