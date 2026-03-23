---
title: Charging Profiles
layout: default
parent: Modules
nav_order: 8
---

# Charging Profiles Module
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Overview

The ChargingProfiles module enables smart charging. Your eMSP can set, update, and delete charging profiles on CPO charge points to control the charging power over time.

{: .important }
> ChargingProfiles are only available in **OCPI 2.2 and 2.2.1**.

## Client Operations

### Set Charging Profile

```csharp
var result = await ocpiClient.ChargingProfiles.SetChargingProfileAsync(
    "DE:CPO",
    "SESSION001",
    new
    {
        response_url = "https://my-emsp.com/ocpi/charging-profiles/callback",
        charging_profile = new
        {
            start_date_time = DateTimeOffset.UtcNow,
            duration = 3600, // seconds
            charging_rate_unit = "W",
            min_charging_rate = 0,
            charging_profile_period = new[]
            {
                new { start_period = 0, limit = 11000.0 },     // 11 kW for first 30 min
                new { start_period = 1800, limit = 7400.0 },   // 7.4 kW after 30 min
            },
        },
    });
```

### Get Active Charging Profile

```csharp
var result = await ocpiClient.ChargingProfiles.GetActiveChargingProfileAsync(
    "DE:CPO", "SESSION001");
```

### Delete Charging Profile

```csharp
var result = await ocpiClient.ChargingProfiles.DeleteChargingProfileAsync(
    "DE:CPO", "SESSION001");
```

## Handling Callbacks

Like Commands, ChargingProfiles use async callbacks:

```csharp
public class MyChargingProfilesCallback : IChargingProfilesCallback
{
    public async Task<OcpiResult> OnChargingProfileResultAsync(
        OcpiRequestContext context, string correlationId, object result, CancellationToken ct)
    {
        // CPO reports whether the profile was accepted/rejected
        return OcpiResult.Success();
    }

    public async Task<OcpiResult> OnActiveChargingProfileUpdateAsync(
        OcpiRequestContext context, string sessionId, object activeProfile, CancellationToken ct)
    {
        // CPO pushes the active charging profile to your eMSP
        return OcpiResult.Success();
    }
}

// Register
builder.Services.AddSingleton<IChargingProfilesCallback, MyChargingProfilesCallback>();
```

## Use Cases

- **Load balancing** — Limit charging power during peak grid demand
- **Green charging** — Schedule charging during periods of renewable energy availability
- **Cost optimization** — Shift charging to off-peak tariff periods
- **Grid services** — Participate in demand response programs
