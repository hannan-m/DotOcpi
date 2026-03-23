---
title: Testing
layout: default
parent: Advanced
nav_order: 5
---

# Testing with DotOcpi.Simulator
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Overview

`DotOcpi.Simulator` provides `OcpiCpoSimulator` — an in-memory OCPI-compliant CPO server that runs during your tests. It supports:

- Version discovery and negotiation
- Credentials exchange (Token A &rarr; B lifecycle)
- Configurable module data (locations, sessions, CDRs, tariffs)
- Failure injection (timeouts, errors, rejection)

## Installation

```bash
dotnet add package DotOcpi.Simulator
```

Add to your test project only.

## Basic Usage

```csharp
using DotOcpi.Simulator;

public class MyIntegrationTests : IAsyncLifetime
{
    private OcpiCpoSimulator _server = null!;

    public async Task InitializeAsync()
    {
        _server = await OcpiCpoSimulator.CreateAsync(config =>
        {
            config.SupportedVersions = [OcpiVersion.V2_2_1];
            config.CpoIdentity = new PartyIdentity("DE", "CPO");

            config.Locations =
            [
                new
                {
                    id = "LOC1",
                    name = "Test Station",
                    address = "123 Test St",
                    city = "Berlin",
                    country = "DEU",
                },
            ];
        });
    }

    public async Task DisposeAsync()
    {
        await _server.DisposeAsync();
    }

    [Fact]
    public async Task CanPullLocations()
    {
        using var client = new HttpClient { BaseAddress = _server.BaseUrl };
        var response = await client.GetAsync("/ocpi/locations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

## OcpiCpoSimulator

### Creation

```csharp
// Default configuration
var server = await OcpiCpoSimulator.CreateAsync();

// Custom configuration
var server = await OcpiCpoSimulator.CreateAsync(config =>
{
    config.SupportedVersions = [OcpiVersion.V2_2_1, OcpiVersion.V2_1_1];
    config.CpoIdentity = new PartyIdentity("DE", "CPO");
    config.Locations = [ /* ... */ ];
    config.Tariffs = [ /* ... */ ];
    config.Sessions = [ /* ... */ ];
    config.Cdrs = [ /* ... */ ];
});
```

### Properties

| Property | Type | Description |
|:---------|:-----|:------------|
| `TokenA` | `string` | Pre-shared Token A for registration |
| `BaseUrl` | `Uri` | Server's base URL (random port) |

### Methods

| Method | Description |
|:-------|:------------|
| `GetIssuedTokenB()` | Returns the last Token B issued during registration |
| `GetReceivedTokenC()` | Returns the last Token C received from the eMSP |
| `DisposeAsync()` | Stops the server and releases resources |

## CpoSimulatorConfiguration

| Property | Type | Default | Description |
|:---------|:-----|:--------|:------------|
| `SupportedVersions` | `IReadOnlyList<OcpiVersion>` | `[V2_2_1]` | Versions the test CPO supports |
| `CpoIdentity` | `PartyIdentity` | `("DE", "CPO")` | CPO's party identity |
| `Locations` | `List<object>` | `[]` | Location data to return |
| `Tariffs` | `List<object>` | `[]` | Tariff data to return |
| `Sessions` | `List<object>` | `[]` | Session data to return |
| `Cdrs` | `List<object>` | `[]` | CDR data to return |
| `RejectRegistration` | `bool` | `false` | Force registration rejection |
| `ForceStatusCode` | `OcpiStatusCode?` | `null` | Force specific OCPI status code |
| `ResponseDelay` | `TimeSpan?` | `null` | Delay all responses |
| `SimulateTimeout` | `bool` | `false` | Simulate request timeouts |

## Endpoints Provided

| Endpoint | Method | Description |
|:---------|:-------|:------------|
| `/ocpi/versions` | GET | Version discovery |
| `/ocpi/versions/{id}` | GET | Version detail with endpoint URLs |
| `/ocpi/credentials` | POST | Initial registration |
| `/ocpi/credentials` | PUT | Credential rotation |
| `/ocpi/credentials` | DELETE | Unregistration |
| `/ocpi/locations` | GET | Paginated location data |
| `/ocpi/tariffs` | GET | Paginated tariff data |
| `/ocpi/sessions` | GET | Paginated session data |
| `/ocpi/cdrs` | GET | Paginated CDR data |

## Failure Injection

### Reject Registration

```csharp
var server = await OcpiCpoSimulator.CreateAsync(config =>
{
    config.RejectRegistration = true;
});

// POST /credentials will return OCPI error
```

### Force Status Code

```csharp
var server = await OcpiCpoSimulator.CreateAsync(config =>
{
    config.ForceStatusCode = new OcpiStatusCode(3000); // Server error
});

// All responses will include status_code: 3000
```

### Response Delay

```csharp
var server = await OcpiCpoSimulator.CreateAsync(config =>
{
    config.ResponseDelay = TimeSpan.FromSeconds(2);
});

// All responses delayed by 2 seconds
```

### Simulate Timeout

```csharp
var server = await OcpiCpoSimulator.CreateAsync(config =>
{
    config.SimulateTimeout = true;
});

// Requests will time out (server delays indefinitely)
```

## Testing Registration Flow

```csharp
[Fact]
public async Task FullRegistrationHandshake()
{
    var server = await OcpiCpoSimulator.CreateAsync();

    using var httpClient = new HttpClient();
    var credentialsClient = new CredentialsClient(httpClient);

    var response = await credentialsClient.PostCredentialsAsync(
        $"{server.BaseUrl}ocpi/credentials",
        server.TokenA,
        OcpiVersion.V2_2_1,
        new
        {
            token = TokenGenerator.Generate(),
            url = "https://emsp.example.com/ocpi/versions",
            roles = new[]
            {
                new
                {
                    role = "EMSP",
                    business_details = new { name = "Test eMSP" },
                    party_id = "MSP",
                    country_code = "NL",
                },
            },
        });

    response.Token.Should().NotBeNullOrEmpty();
    server.GetIssuedTokenB().Should().Be(response.Token);
}
```

## Testing with WebApplicationFactory

```csharp
public class MyAppTests : IAsyncLifetime
{
    private OcpiCpoSimulator _cpo = null!;
    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        _cpo = await OcpiCpoSimulator.CreateAsync(c =>
        {
            c.Locations = [new { id = "LOC1", name = "Test" }];
        });

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddDotOcpi(options =>
                    {
                        options.SupportedVersions = [OcpiVersion.V2_2_1];
                        options.DefaultEmspIdentity = new PartyIdentity("NL", "MSP");
                        options.BaseUrl = new Uri("https://test.example.com/ocpi");
                    })
                    .AddInMemoryTokenStore()
                    .AddInMemoryCpoRegistry()
                    .AddClient();
                });
            });
    }

    public async Task DisposeAsync()
    {
        _factory.Dispose();
        await _cpo.DisposeAsync();
    }

    [Fact]
    public async Task CanRegisterAndPullLocations()
    {
        using var scope = _factory.Services.CreateScope();
        var registrationClient = scope.ServiceProvider.GetRequiredService<IRegistrationClient>();

        var result = await registrationClient.RegisterAsync(new RegistrationRequest(
            VersionsUrl: $"{_cpo.BaseUrl}ocpi/versions",
            TokenA: _cpo.TokenA,
            EmspCountryCode: "NL",
            EmspPartyId: "MSP",
            EmspVersionsUrl: "https://test.example.com/ocpi/versions",
            EmspBusinessName: "Test eMSP"));

        result.Connection.Status.Should().Be(ConnectionStatus.Connected);
    }
}
```

## Multi-Version Testing

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
public async Task Registration_Works_ForAllVersions(OcpiVersion version)
{
    var server = await OcpiCpoSimulator.CreateAsync(c =>
    {
        c.SupportedVersions = [version];
    });

    using var httpClient = new HttpClient();
    var discovery = new VersionDiscovery(httpClient);
    var versions = await discovery.GetVersionsAsync(
        $"{server.BaseUrl}ocpi/versions", server.TokenA);

    versions.Should().ContainSingle()
        .Which.Version.Should().Be(version.ToVersionString());

    await server.DisposeAsync();
}
```

## DI Registration

For tests that use the DI container:

```csharp
builder.Services.AddTestCpoServer(config =>
{
    config.Locations = [new { id = "LOC1", name = "Test" }];
});
```

This registers `OcpiCpoSimulator` as a singleton and starts it automatically.
