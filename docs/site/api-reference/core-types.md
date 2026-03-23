---
title: Core Types
layout: default
parent: API Reference
nav_order: 1
---

# Core Types
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## OcpiVersion

Enum representing supported OCPI protocol versions.

```csharp
public enum OcpiVersion
{
    V2_0,
    V2_1_1,
    V2_2,
    V2_2_1,
}
```

### Extension Methods

```csharp
// Convert to version string
OcpiVersion.V2_2_1.ToVersionString();  // "2.2.1"

// Parse from string
OcpiVersionExtensions.TryParse("2.2.1", out var version);  // true

// Check URL pattern
OcpiVersion.V2_2_1.UsesPartyIdInUrls();  // true (2.2+)
OcpiVersion.V2_1_1.UsesPartyIdInUrls();  // false

// Check deprecation status
OcpiVersion.V2_2.IsDeprecated();   // true
OcpiVersion.V2_2_1.IsDeprecated(); // false
```

---

## PartyIdentity

Identifies an OCPI party (CPO or eMSP).

```csharp
public readonly record struct PartyIdentity(string CountryCode, string PartyId)
{
    public string ToCompositeId();  // Returns "{CC}_{PID}"
}
```

### Usage

```csharp
var identity = new PartyIdentity("NL", "MSP");
Console.WriteLine(identity.CountryCode); // "NL"
Console.WriteLine(identity.PartyId);     // "MSP"
Console.WriteLine(identity.ToCompositeId()); // "NL_MSP"
```

---

## CiString

Case-insensitive string type for OCPI string fields.

```csharp
public readonly record struct CiString(string Value)
{
    public bool IsEmpty { get; }
    public bool IsWithinMaxLength(int maxLength);
}
```

### Usage

```csharp
var a = new CiString("Hello");
var b = new CiString("HELLO");
Console.WriteLine(a == b);  // true (case-insensitive comparison)

// Implicit conversion from string
CiString cs = "test";
string s = cs.Value;
```

---

## GeoLocation

Geographic coordinates.

```csharp
public readonly record struct GeoLocation(string Latitude, string Longitude);
```

{: .note }
> `GeoLocation` stores coordinates as strings to preserve wire-format precision. OCPI specifies decimal degree strings, and converting to `decimal` may lose precision.

### Usage

```csharp
var coords = new GeoLocation("52.5200", "13.4050");  // Berlin
```

---

## OcpiStatusCode

OCPI protocol status code.

```csharp
public readonly record struct OcpiStatusCode(int Value)
{
    // Predefined codes
    public static readonly OcpiStatusCode Success = new(1000);
    public static readonly OcpiStatusCode GenericClientError = new(2000);
    public static readonly OcpiStatusCode InvalidParameters = new(2001);
    public static readonly OcpiStatusCode NotEnoughInformation = new(2002);
    public static readonly OcpiStatusCode UnknownLocation = new(2003);
    public static readonly OcpiStatusCode UnknownToken = new(2004);
    public static readonly OcpiStatusCode GenericServerError = new(3000);
    public static readonly OcpiStatusCode UnableToUseClientApi = new(3001);
    public static readonly OcpiStatusCode UnsupportedVersion = new(3002);
    public static readonly OcpiStatusCode NoMatchingEndpoints = new(3003);

    // Range checks
    public bool IsSuccess { get; }      // 1xxx
    public bool IsClientError { get; }  // 2xxx
    public bool IsServerError { get; }  // 3xxx
}
```

### Usage

```csharp
var code = new OcpiStatusCode(1000);
Console.WriteLine(code.IsSuccess);     // true

var error = OcpiStatusCode.InvalidParameters;
Console.WriteLine(error.Value);        // 2001
Console.WriteLine(error.IsClientError); // true
```

---

## OcpiResult and OcpiResult&lt;T&gt;

Discriminated result types for OCPI operations. No exceptions for expected OCPI errors.

```csharp
public sealed class OcpiResult
{
    public bool IsSuccess { get; }
    public OcpiStatusCode StatusCode { get; }
    public string? StatusMessage { get; }

    public static OcpiResult Success(string? message = null);
    public static OcpiResult Failure(OcpiStatusCode code, string message);
}

public sealed class OcpiResult<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public OcpiStatusCode StatusCode { get; }
    public string? StatusMessage { get; }

    public static OcpiResult<T> Success(T data, string? message = null);
    public static OcpiResult<T> Failure(OcpiStatusCode code, string message);
}
```

### Usage

```csharp
// Success
var result = OcpiResult.Success();
var resultWithData = OcpiResult<Location>.Success(location);

// Failure
var error = OcpiResult.Failure(new OcpiStatusCode(2001), "Invalid location ID");
var errorWithType = OcpiResult<Location>.Failure(
    OcpiStatusCode.UnknownLocation, "Location not found");

// Check result
if (result.IsSuccess)
{
    var data = resultWithData.Data;
}
else
{
    Console.WriteLine($"Error {result.StatusCode.Value}: {result.StatusMessage}");
}
```

---

## OcpiResponse&lt;T&gt;

Wire envelope for OCPI HTTP responses.

```csharp
public sealed class OcpiResponse<T>
{
    public T? Data { get; }
    public int StatusCode { get; }
    public string? StatusMessage { get; }
    public DateTimeOffset Timestamp { get; }
}
```

Every OCPI HTTP response is wrapped in this envelope:

```json
{
  "data": { /* ... */ },
  "status_code": 1000,
  "status_message": "Success",
  "timestamp": "2024-01-15T10:00:00Z"
}
```

---

## PaginatedResult&lt;T&gt;

Result for paginated list endpoints.

```csharp
public sealed class PaginatedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Offset { get; init; }
    public required int Limit { get; init; }
}
```

### Usage

```csharp
return new PaginatedResult<object>
{
    Items = tokens.ToList(),
    TotalCount = 150,
    Offset = 0,
    Limit = 50,
};
// DotOcpi sets X-Total-Count: 150, X-Limit: 50, Link: <next-page-url>
```

---

## OcpiRequestContext

Context passed to every module handler with request details.

```csharp
public sealed class OcpiRequestContext
{
    public CpoConnection Connection { get; }
    public string RequestId { get; }            // X-Request-ID
    public string CorrelationId { get; }        // X-Correlation-ID
    public string CpoId { get; }                // "DE:CPO"
    public PartyIdentity CpoIdentity { get; }
    public PartyIdentity EmspIdentity { get; }
    public OcpiVersion NegotiatedVersion { get; }
    public string ModuleId { get; }             // "locations", "sessions", etc.

    public static bool IsFieldNotAvailable(string? value);  // Check for "#NA" sentinel
}
```

---

## OcpiSentinel

OCPI "not available" sentinel value.

```csharp
public static class OcpiSentinel
{
    public const string NotAvailable = "#NA";
    public static bool IsNotAvailable(string? value);
}
```

Some OCPI fields use `"#NA"` to indicate the value is not available rather than null:

```csharp
if (OcpiSentinel.IsNotAvailable(location.Name))
{
    // Field explicitly marked as not available
}
```

---

## JSON PATCH Merge

The library delivers raw `JsonElement` patches to consumer handlers (e.g., `ILocationsReceiver.OnLocationPatchAsync`). Consumers are responsible for applying JSON Merge Patch (RFC 7396) in their own domain layer using `System.Text.Json` or a library of their choice.

---

## Exceptions

All DotOcpi exceptions inherit from `OcpiException`:

| Exception | When |
|:----------|:-----|
| `OcpiException` | Abstract base class |
| `OcpiConfigurationException` | Invalid configuration (HTTP URL, missing options) |
| `OcpiRegistrationException` | Handshake failure (no mutual version, Token A consumed) |
| `OcpiTransportException` | Network failure (timeout, DNS error, connection refused) |
| `OcpiSerializationException` | JSON parsing error (invalid format, missing required fields) |

{: .tip }
> Exceptions are for truly exceptional conditions. Expected OCPI errors (invalid token, unknown location) are returned as `OcpiResult` with the appropriate status code.
