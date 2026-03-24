---
title: Home
layout: home
nav_order: 1
---

# DotOcpi

**The open-source OCPI .NET library for eMSP developers.**
{: .fs-6 .fw-300 }

Build OCPI-compliant eMSP applications with multi-version support, automated CPO registration, and enterprise-grade security — all with a clean, idiomatic .NET API.
{: .fs-5 .fw-300 }

[Get Started](/DotOcpi/getting-started/installation){: .btn .btn-primary .fs-5 .mb-4 .mb-md-0 .mr-2 }
[View on GitHub](https://github.com/hannan-m/DotOcpi){: .btn .fs-5 .mb-4 .mb-md-0 }

---

## What is DotOcpi?

DotOcpi is a .NET library that implements the [OCPI protocol](https://github.com/ocpi/ocpi) for **eMSP (e-Mobility Service Provider)** applications. It handles both roles an eMSP plays:

- **Client** — calling CPO endpoints to pull locations, send commands, push tokens
- **Server** — receiving pushes from CPOs for locations, sessions, CDRs, tariffs

## Key Features

### Multi-Version Support
{: .d-inline-block }
OCPI 2.0 — 2.2.1
{: .label .label-green }

Each CPO connection independently negotiates its own OCPI version. Run 2.0 and 2.2.1 connections side-by-side in the same deployment.

### Multi-Party
Operate as multiple eMSP identities from a single deployment. Present different `country_code`/`party_id` to different CPOs.

### Automated Registration
Version discovery, negotiation, and credentials exchange (Token A → B → C lifecycle) handled automatically.

### Storage-Agnostic
The library owns protocol logic only. Bring your own persistence via `ITokenStore` and `ICpoRegistryStore` interfaces.

### Enterprise Security
CSPRNG token generation, SHA-256 hashing with constant-time comparison, HTTPS enforcement, and zero raw tokens in logs or storage.

### Full Module Coverage
All eMSP-side OCPI modules: Locations, Sessions, CDRs, Tariffs, Tokens, Commands, and ChargingProfiles.

---

## Packages

| Package | Description |
|:--------|:------------|
| [`DotOcpi`](https://github.com/hannan-m/DotOcpi/tree/main/src/DotOcpi) | Core: models, interfaces, token management, version negotiation |
| [`DotOcpi.AspNetCore`](https://github.com/hannan-m/DotOcpi/tree/main/src/DotOcpi.AspNetCore) | ASP.NET Core server: middleware, endpoint routing, auth pipeline |
| [`DotOcpi.Client`](https://github.com/hannan-m/DotOcpi/tree/main/src/DotOcpi.Client) | HttpClient-based OCPI client for CPO endpoints |
| [`DotOcpi.Simulator`](https://github.com/hannan-m/DotOcpi/tree/main/src/DotOcpi.Simulator) | In-memory test CPO server for integration tests |

## Quick Example

```csharp
// 1. Register services
services.AddDotOcpi(options =>
{
    options.SupportedVersions = [OcpiVersion.V2_2_1];
    options.DefaultEmspIdentity = new PartyIdentity("NL", "MSP");
    options.BaseUrl = new Uri("https://my-emsp.com/ocpi");
})
.AddInMemoryTokenStore()
.AddInMemoryCpoRegistry()
.AddAspNetCoreServer()
.AddClient();

// 2. Register with a CPO
var result = await registrationClient.RegisterAsync(new RegistrationRequest(
    VersionsUrl: "https://cpo.example.com/ocpi/versions",
    TokenA: "pre-shared-token-a",
    EmspCountryCode: "NL",
    EmspPartyId: "MSP",
    EmspVersionsUrl: "https://my-emsp.com/ocpi/versions",
    EmspBusinessName: "My eMSP"
));

// 3. Pull locations from the CPO
await foreach (var location in ocpiClient.Locations.GetAllLocationsAsync("DE:CPO"))
{
    Console.WriteLine($"Location: {location}");
}
```

## Supported OCPI Versions

| Version | Status | URL Pattern |
|:--------|:-------|:------------|
| 2.0 | Supported | `/{object_id}` |
| 2.1.1 | Supported | `/{object_id}` |
| 2.2 | Supported (deprecated) | `/{country_code}/{party_id}/{object_id}` |
| 2.2.1 | **Recommended** | `/{country_code}/{party_id}/{object_id}` |

## Requirements

- .NET 8.0 (LTS) or .NET 10.0 (LTS)
