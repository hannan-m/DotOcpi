---
title: Multi-Version Support
layout: default
parent: Guides
nav_order: 6
---

# Multi-Version Support
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Overview

DotOcpi supports four OCPI versions simultaneously:

| Version | Status | URL Pattern |
|:--------|:-------|:------------|
| 2.0 | Supported | `/{object_id}` |
| 2.1.1 | Supported | `/{object_id}` |
| 2.2 | Supported (deprecated) | `/{country_code}/{party_id}/{object_id}` |
| 2.2.1 | **Recommended** | `/{country_code}/{party_id}/{object_id}` |

Each CPO connection independently negotiates its own version. You can have CPO-A on OCPI 2.0 and CPO-B on OCPI 2.2.1 running side-by-side.

## How Version Negotiation Works

During registration, DotOcpi negotiates the **highest mutually supported version**:

```csharp
builder.Services.AddDotOcpi(options =>
{
    // Your eMSP supports these versions
    options.SupportedVersions = [
        OcpiVersion.V2_2_1,
        OcpiVersion.V2_1_1,
        OcpiVersion.V2_0
    ];
});
```

If the CPO supports 2.2.1 and 2.1.1, the negotiator selects **2.2.1**.

### Deprecation Handling

OCPI 2.1 and 2.2 are deprecated intermediaries:
- If both 2.1 and **2.1.1** are available, 2.1.1 is preferred
- If both 2.2 and **2.2.1** are available, 2.2.1 is preferred
- If only the deprecated version is available, it will still be used

## Separate Models Per Version

Each version has its own model namespace with distinct types:

```
DotOcpi.Models.V2_0.Location
DotOcpi.Models.V2_1_1.Location
DotOcpi.Models.V2_2.Location
DotOcpi.Models.V2_2_1.Location
```

This prevents nullable-field pollution and brittle conditional logic. Each model exactly matches the OCPI spec for that version.

### Key Differences Between Versions

| Feature | 2.0 | 2.1.1 | 2.2 | 2.2.1 |
|:--------|:----|:------|:----|:------|
| URL format | `/{id}` | `/{id}` | `/{cc}/{pid}/{id}` | `/{cc}/{pid}/{id}` |
| Credentials | Flat structure | Flat structure | Roles array | Roles array |
| EnergyMix | - | Yes | Yes | Yes |
| ChargingProfiles | - | - | Yes | Yes |
| CancelReservation | - | - | - | Yes |
| Tariff PATCH | Yes | Yes | No (405) | No (405) |
| Token fields | `auth_id` | `auth_id` | `contract_id` | `contract_id` |
| `last_updated` | - | Yes | Yes | Yes |
| `publish` flag | - | - | Yes | Yes |

## Version-Aware Handlers

Your module handlers receive data deserialized into the correct version-specific type:

```csharp
public async Task<OcpiResult> OnLocationPutAsync(
    OcpiRequestContext context, string locationId, object data, CancellationToken ct)
{
    // Check the negotiated version if you need version-specific handling
    switch (context.NegotiatedVersion)
    {
        case OcpiVersion.V2_2_1:
            var loc221 = (DotOcpi.Models.V2_2_1.Location)data;
            // V2.2.1 has Publish, EnergyMix, etc.
            break;

        case OcpiVersion.V2_1_1:
            var loc211 = (DotOcpi.Models.V2_1_1.Location)data;
            // V2.1.1 has LastUpdated, EnergyMix, but no Publish
            break;

        case OcpiVersion.V2_0:
            var loc20 = (DotOcpi.Models.V2_0.Location)data;
            // V2.0 has basic fields only
            break;
    }

    return OcpiResult.Success();
}
```

{: .tip }
> In most cases, you don't need to switch on version. Store the data generically and let the version-specific serialization handle the differences.

## Version-Specific Serialization

DotOcpi uses source-generated `JsonSerializerContext` per version for maximum performance:

```csharp
// Get the correct serializer options for a version
var options = OcpiJsonOptions.GetOptions(OcpiVersion.V2_2_1);

// Serialize with version-specific context
var json = JsonSerializer.Serialize(location, options);

// Deserialize with version-specific context
var location = JsonSerializer.Deserialize<Location>(json, options);
```

Each version context handles:
- Snake_case property naming
- RFC 3339 DateTime formatting
- OCPI-specific enum string values
- Version-specific field presence/absence

## Version Helper Methods

```csharp
// Convert enum to string
OcpiVersion.V2_2_1.ToVersionString();  // "2.2.1"

// Parse string to enum
OcpiVersionExtensions.TryParse("2.2.1", out var version);  // true, V2_2_1

// Check URL pattern
OcpiVersion.V2_2_1.UsesPartyIdInUrls();  // true
OcpiVersion.V2_1_1.UsesPartyIdInUrls();  // false

// Check deprecation
OcpiVersion.V2_2.IsDeprecated();  // true
OcpiVersion.V2_2_1.IsDeprecated();  // false
```

## Running Multiple Versions

```csharp
// Configure to support multiple versions
builder.Services.AddDotOcpi(options =>
{
    options.SupportedVersions = [
        OcpiVersion.V2_2_1,
        OcpiVersion.V2_1_1,
    ];
});

// Register with CPO A (negotiates 2.2.1)
var cpoA = await registrationClient.RegisterAsync(new RegistrationRequest(
    VersionsUrl: "https://cpo-a.com/ocpi/versions",
    TokenA: "token-a-for-cpo-a",
    EmspCountryCode: "NL",
    EmspPartyId: "MSP",
    EmspVersionsUrl: "https://my-emsp.com/ocpi/versions",
    EmspBusinessName: "My eMSP"
));

// Register with CPO B (only supports 2.1.1 — negotiates 2.1.1)
var cpoB = await registrationClient.RegisterAsync(new RegistrationRequest(
    VersionsUrl: "https://cpo-b.com/ocpi/versions",
    TokenA: "token-a-for-cpo-b",
    EmspCountryCode: "NL",
    EmspPartyId: "MSP",
    EmspVersionsUrl: "https://my-emsp.com/ocpi/versions",
    EmspBusinessName: "My eMSP"
));

Console.WriteLine(cpoA.Connection.Version); // V2_2_1
Console.WriteLine(cpoB.Connection.Version); // V2_1_1

// Pull from both — the client uses the correct version automatically
await foreach (var loc in ocpiClient.Locations.GetAllLocationsAsync(cpoA.Connection.ConnectionKey))
{
    // Gets V2_2_1 Location objects
}

await foreach (var loc in ocpiClient.Locations.GetAllLocationsAsync(cpoB.Connection.ConnectionKey))
{
    // Gets V2_1_1 Location objects
}
```

## Multi-Version Testing

Use `[Theory]` with `[MemberData]` to test across all versions:

```csharp
public static IEnumerable<object[]> AllVersions =>
[
    [OcpiVersion.V2_0],
    [OcpiVersion.V2_1_1],
    [OcpiVersion.V2_2],
    [OcpiVersion.V2_2_1],
];

[Theory]
[MemberData(nameof(AllVersions))]
public async Task Registration_Succeeds_ForAllVersions(OcpiVersion version)
{
    var server = await OcpiCpoSimulator.CreateAsync(c =>
    {
        c.SupportedVersions = [version];
    });

    // Test registration against each version
}
```

---

<div style="display: flex; justify-content: space-between; margin-top: 2rem;">
  <div>← <a href="/DotOcpi/guides/token-management/">Token Management</a></div>
  <div><a href="/DotOcpi/guides/working-with-data/">Working with Data</a> →</div>
</div>
