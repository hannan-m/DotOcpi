---
title: Validation
layout: default
parent: API Reference
nav_order: 9
---

# Validation
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Three-Layer Validation

DotOcpi validates data at three layers:

| Layer | What | Who handles it |
|:------|:-----|:---------------|
| **Transport** | JSON syntax, required fields, types | `System.Text.Json` deserialization |
| **Protocol** | OCPI semantics (coordinate ranges, enum values, URL consistency) | Built-in `IOcpiValidator<T>` implementations |
| **Business** | Your domain rules (authorization, capacity, billing) | Your handler code |

The library handles the first two layers. You handle the third.

---

## IOcpiValidator&lt;T&gt;

The validation interface for OCPI models:

```csharp
public interface IOcpiValidator<in T> : IOcpiValidator
{
    OcpiValidationResult Validate(T model);
}

// Non-generic base for runtime dispatch
public interface IOcpiValidator
{
    OcpiValidationResult Validate(object model);
}
```

### OcpiValidationResult

```csharp
public sealed class OcpiValidationResult
{
    public IReadOnlyList<OcpiValidationError> Errors { get; }
    public bool IsValid { get; }  // true when Errors is empty

    public static OcpiValidationResult Valid();
    public static OcpiValidationResult Failed(IReadOnlyList<OcpiValidationError> errors);
    public static OcpiValidationResult Failed(OcpiValidationError error);
}
```

### OcpiValidationError

Each error includes a machine-readable code, human-readable message, and fix suggestion:

```csharp
public sealed record OcpiValidationError(
    string Code,        // e.g., "LOCATION_MISSING_COUNTRY"
    string Message,     // e.g., "Country code is required"
    string Suggestion   // e.g., "Set the Country property to an ISO 3166-1 alpha-3 code"
)
{
    public string? PropertyPath { get; init; }  // e.g., "Evses[0].Connectors[1].MaxVoltage"
}
```

---

## Built-in Validators

DotOcpi includes validators for every OCPI module. Each validates all supported versions:

| Validator | Models | Key rules |
|:----------|:-------|:----------|
| `LocationValidator` | `Location` (all versions) | Coordinate ranges, country codes (alpha-2/alpha-3), EVSE UID uniqueness, publish consistency (2.2+) |
| `SessionValidator` | `Session` (all versions) | Required timestamps, kWh non-negative, valid session status |
| `CdrValidator` | `Cdr` (all versions) | Start/end time ordering, total cost non-negative, charging periods present |
| `TariffValidator` | `Tariff` (all versions) | Price components, tariff element structure |
| `TokenValidator` | `Token` (all versions) | Token type, whitelist type, contract ID format |
| `CommandValidator` | Command models (all versions) | Response URL present, expiry dates, location references |
| `CredentialsValidator` | `Credentials` (all versions) | Token present, URL valid, roles structure (2.2+) |
| `ChargingProfileValidator` | Charging profile models | Profile periods, power limits |
| `PatchValidator` | `JsonElement` patches | Non-empty patch, no `id` field modification |

### Validation Pipeline

Validation runs inline via `EndpointHelper.DeserializeOrRejectAsync()` at the start of each endpoint handler:

```
Request → EndpointHelper.DeserializeOrRejectAsync() → Your Handler
              ↓ JSON parse      ↓ DataAnnotations + IOcpiValidator<T>
              ↓ (if invalid)    ↓ (if invalid)
              HTTP 400 + OCPI 2001
```

If validation fails, the CPO receives:

```json
{
  "status_code": 2001,
  "status_message": "Invalid or missing parameters.",
  "timestamp": "2026-03-24T10:00:00Z"
}
```

Validation error details are logged server-side at Warning level (event 8001).

---

## Custom Validators

To add your own validation rules, implement `IOcpiValidator<T>` and register it:

```csharp
public class MyLocationValidator : IOcpiValidator<Models.V2_2_1.Location>
{
    public OcpiValidationResult Validate(Models.V2_2_1.Location model)
    {
        var errors = new List<OcpiValidationError>();

        // Custom rule: reject locations outside your service area
        if (model.Coordinates is { } coords)
        {
            if (!IsInServiceArea(coords))
            {
                errors.Add(new OcpiValidationError(
                    Code: "LOCATION_OUTSIDE_SERVICE_AREA",
                    Message: "Location coordinates are outside the supported service area",
                    Suggestion: "Verify the latitude/longitude values"
                ) { PropertyPath = "Coordinates" });
            }
        }

        // Custom rule: require energy mix for 2.2.1 locations
        if (model.EnergyMix is null)
        {
            errors.Add(new OcpiValidationError(
                Code: "LOCATION_MISSING_ENERGY_MIX",
                Message: "Energy mix is required for all locations",
                Suggestion: "Include the energy_mix field"
            ) { PropertyPath = "EnergyMix" });
        }

        return errors.Count == 0
            ? OcpiValidationResult.Valid()
            : OcpiValidationResult.Failed(errors);
    }

    private static bool IsInServiceArea(GeoLocation coords) =>
        // Your logic here
        true;
}
```

### Registration

```csharp
// Register your validator — it runs in addition to the built-in LocationValidator
builder.Services.AddSingleton<IOcpiValidator<Models.V2_2_1.Location>, MyLocationValidator>();
```

{: .note }
> Custom validators complement the built-in ones. If you register a custom `IOcpiValidator<T>` for a type that already has a built-in validator, your validator replaces it. To keep the built-in rules, call the built-in validator from your custom one.

---

## Manual Validation

You can also validate models manually in your handler code:

```csharp
public class MyLocationsReceiver : ILocationsReceiver
{
    private readonly IOcpiValidator<Models.V2_2_1.Location> _validator;

    public MyLocationsReceiver(IOcpiValidator<Models.V2_2_1.Location> validator)
    {
        _validator = validator;
    }

    public Task<OcpiResult> OnLocationPutAsync(
        OcpiRequestContext context, string locationId, object data, CancellationToken ct)
    {
        if (data is Models.V2_2_1.Location location)
        {
            var result = _validator.Validate(location);
            if (!result.IsValid)
            {
                var firstError = result.Errors[0];
                return Task.FromResult(
                    OcpiResult.Failure(OcpiStatusCode.InvalidParameters, firstError.Message));
            }
        }

        // ... store the location
        return Task.FromResult(OcpiResult.Success());
    }
}
```
