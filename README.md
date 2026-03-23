# DotOcpi

An open-source OCPI (Open Charge Point Interface) .NET library for the eMSP side. Covers both roles an eMSP plays: **client** (calling CPO endpoints) and **server** (receiving pushes from CPOs).

## Features

- **Multi-version**: OCPI 2.0, 2.1.1, 2.2, and 2.2.1 coexist in one deployment. Each CPO connection negotiates its own version independently.
- **Multi-party**: Present different eMSP identities (`country_code`/`party_id`) to different CPOs from a single deployment.
- **Storage-agnostic**: The library owns protocol logic only. Consumers provide persistence via interfaces (`ITokenStore`, `ICpoRegistryStore`).
- **Automated registration**: Version discovery, negotiation, and credentials exchange (Token A -> B -> C lifecycle).
- **Full module coverage**: Locations, Sessions, CDRs, Tariffs, Tokens, Commands, and ChargingProfiles.

## Packages

| Package | Description |
|---|---|
| `DotOcpi` | Core: version-specific models, interfaces, result types, token management, version negotiation |
| `DotOcpi.AspNetCore` | ASP.NET Core server integration: middleware, endpoint routing, OCPI auth pipeline |
| `DotOcpi.Client` | HttpClient-based OCPI client for calling CPO endpoints |
| `DotOcpi.Simulator` | In-memory OCPI-compliant test CPO server for consumer integration tests |

## Quick Start

```csharp
// Register DotOcpi services
services.AddDotOcpi(options =>
{
    options.SupportedVersions = [OcpiVersion.V2_2_1];
    options.DefaultEmspIdentity = new PartyIdentity("NL", "MSP");
    options.BaseUrl = new Uri("https://my-emsp.com/ocpi");
})
.AddInMemoryTokenStore()
.AddInMemoryCpoRegistry()
.AddClient();
```

```csharp
// Register with a CPO
var result = await registrationClient.RegisterAsync(new RegistrationRequest(
    VersionsUrl: "https://cpo.example.com/ocpi/versions",
    TokenA: "pre-shared-token-a",
    EmspCountryCode: "NL",
    EmspPartyId: "MSP",
    EmspVersionsUrl: "https://my-emsp.com/ocpi/versions",
    EmspBusinessName: "My eMSP"
));
// result.Connection contains the CPO connection details
// result.CpoToken is the token to use when calling CPO endpoints
```

```csharp
// Pull locations from a CPO
await foreach (var location in locationsClient.GetAllLocationsAsync("DE:CPO"))
{
    // Process each location
}
```

## Testing

Use the `DotOcpi.Simulator` package to run integration tests against a real OCPI-compliant test CPO:

```csharp
await using var cpo = await OcpiCpoSimulator.CreateAsync(config =>
{
    config.SupportedVersions = [OcpiVersion.V2_2_1];
    config.Locations = [new { id = "LOC1", name = "Test Location" }];
});

// cpo.BaseUrl is the test server URL
// cpo.TokenA is the pre-shared token for registration
```

## Requirements

- .NET 8.0 (LTS) or .NET 10.0 (LTS)

## Supported OCPI Versions

- 2.0
- 2.1.1
- 2.2
- 2.2.1

## License

[MIT](LICENSE)
