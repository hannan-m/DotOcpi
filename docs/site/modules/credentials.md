---
title: Credentials
layout: default
parent: Modules
nav_order: 1
---

# Credentials Module
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Overview

The Credentials module handles the OCPI registration handshake between your eMSP and CPOs. It manages the Token A &rarr; B &rarr; C lifecycle.

## Client Side

### Register (POST)

```csharp
var result = await registrationClient.RegisterAsync(new RegistrationRequest(
    VersionsUrl: "https://cpo.example.com/ocpi/versions",
    TokenA: "pre-shared-token-a",
    EmspCountryCode: "NL",
    EmspPartyId: "MSP",
    EmspVersionsUrl: "https://my-emsp.com/ocpi/versions",
    EmspBusinessName: "My eMSP"
));
```

### Rotate (PUT)

```csharp
var result = await registrationClient.RotateCredentialsAsync(new CredentialRotationRequest(
    ConnectionKey: "DE:CPO",
    CurrentCpoToken: currentTokenB,
    EmspBusinessName: "My eMSP"
));
```

### Unregister (DELETE)

```csharp
await registrationClient.UnregisterAsync(new UnregisterRequest(
    ConnectionKey: "DE:CPO",
    CurrentCpoToken: currentTokenB
));
```

## Server Side

When a CPO initiates registration with your eMSP, DotOcpi handles the credentials endpoint automatically. You can customize behavior by implementing `ICredentialsHandler`:

```csharp
public class MyCredentialsHandler : ICredentialsHandler
{
    public async Task<OcpiResult<object>> OnCredentialsPostAsync(
        OcpiRegistrationContext context, object credentials, CancellationToken ct)
    {
        // Called when a CPO POSTs credentials to register (Token A auth).
        // No CpoConnection exists yet — context.TokenAEntry gives you
        // the validated Token A hash and associated party ID.
        // Return your eMSP's credentials.
        return OcpiResult<object>.Success(new
        {
            token = TokenGenerator.Generate(),
            url = "https://my-emsp.com/ocpi/versions",
            roles = new[]
            {
                new
                {
                    role = "EMSP",
                    business_details = new { name = "My eMSP" },
                    party_id = "MSP",
                    country_code = "NL",
                },
            },
        });
    }

    public async Task<OcpiResult<object>> OnCredentialsPutAsync(
        OcpiRequestContext context, object credentials, CancellationToken ct)
    {
        // Called when a CPO PUTs credentials to rotate (Token B auth)
        return OcpiResult<object>.Success(/* new credentials */);
    }

    public async Task<OcpiResult> OnCredentialsDeleteAsync(
        OcpiRequestContext context, CancellationToken ct)
    {
        // Called when a CPO DELETEs credentials to unregister (Token B auth)
        return OcpiResult.Success();
    }

    public async Task<OcpiResult<object>> GetCredentialsAsync(
        OcpiRequestContext context, CancellationToken ct)
    {
        // Called when a CPO GETs your credentials (Token B auth)
        return OcpiResult<object>.Success(/* your credentials */);
    }
}
```

Note that `OnCredentialsPostAsync` receives `OcpiRegistrationContext` (not `OcpiRequestContext`) because initial registration uses Token A and no CPO connection exists yet. The other methods receive `OcpiRequestContext` which includes the established `CpoConnection`.

## Version Differences

| Feature | 2.0 / 2.1.1 | 2.2 / 2.2.1 |
|:--------|:-------------|:-------------|
| Credentials structure | Flat (single party) | Roles array (multiple parties) |
| Token field | `token` | `token` |
| URL field | `url` | `url` |
| Business details | Top-level | Inside each role |

### 2.0 / 2.1.1 Credentials

```json
{
  "token": "abc123...",
  "url": "https://emsp.com/ocpi/versions",
  "business_name": "My eMSP",
  "party_id": "MSP",
  "country_code": "NL"
}
```

### 2.2 / 2.2.1 Credentials

```json
{
  "token": "abc123...",
  "url": "https://emsp.com/ocpi/versions",
  "roles": [
    {
      "role": "EMSP",
      "business_details": { "name": "My eMSP" },
      "party_id": "MSP",
      "country_code": "NL"
    }
  ]
}
```

DotOcpi handles the format difference automatically during registration and serialization.
