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

## Multi-Version Testing

```csharp
[Theory]
[InlineData(OcpiVersion.V2_0)]
[InlineData(OcpiVersion.V2_1_1)]
[InlineData(OcpiVersion.V2_2)]
[InlineData(OcpiVersion.V2_2_1)]
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

There is also an async variant:

```csharp
await builder.Services.AddTestCpoServerAsync(config =>
{
    config.Locations = [new { id = "LOC1", name = "Test" }];
});
```

## Testing Push Scenarios

To test how your eMSP handles incoming pushes from a CPO, send requests directly to your endpoints:

```csharp
[Fact]
public async Task LocationPush_Stores_Location()
{
    // Arrange: register a CPO first
    await RegisterTestCpoAsync();

    var client = _factory.CreateClient();

    // The auth token must match a stored Token B
    var tokenB = _cpo.GetIssuedTokenB();

    // Act: simulate CPO pushing a location
    var request = new HttpRequestMessage(HttpMethod.Put,
        "/ocpi/locations/DE/CPO/LOC001");
    request.Headers.Authorization = new AuthenticationHeaderValue("Token", tokenB);
    request.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());
    request.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString());
    request.Content = JsonContent.Create(new
    {
        id = "LOC001",
        name = "Pushed Location",
        address = "456 Test Ave",
        city = "Munich",
        country = "DEU",
        coordinates = new { latitude = "48.1351", longitude = "11.5820" },
        last_updated = DateTimeOffset.UtcNow,
    });

    var response = await client.SendAsync(request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var body = await response.Content.ReadFromJsonAsync<JsonElement>();
    body.GetProperty("status_code").GetInt32().Should().Be(1000);
}
```

## Testing Error Scenarios

### Handler Returns Failure

```csharp
[Fact]
public async Task Returns_2003_When_Location_Not_Found()
{
    await RegisterTestCpoAsync();
    var client = _factory.CreateClient();
    var tokenB = _cpo.GetIssuedTokenB();

    var request = new HttpRequestMessage(HttpMethod.Get,
        "/ocpi/locations/DE/CPO/NONEXISTENT");
    request.Headers.Authorization = new AuthenticationHeaderValue("Token", tokenB);
    request.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());
    request.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString());

    var response = await client.SendAsync(request);

    var body = await response.Content.ReadFromJsonAsync<JsonElement>();
    body.GetProperty("status_code").GetInt32().Should().Be(2003);
}
```

### Invalid Token

```csharp
[Fact]
public async Task Returns_401_For_Invalid_Token()
{
    var client = _factory.CreateClient();

    var request = new HttpRequestMessage(HttpMethod.Get, "/ocpi/locations");
    request.Headers.Authorization = new AuthenticationHeaderValue("Token", "invalid-token");
    request.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());
    request.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString());

    var response = await client.SendAsync(request);

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

### CPO Failure During Sync

```csharp
[Fact]
public async Task PullSync_Handles_CpoFailure()
{
    var server = await OcpiCpoSimulator.CreateAsync(config =>
    {
        config.ForceStatusCode = new OcpiStatusCode(3000);
    });

    // Sync should complete without throwing, with IsSuccess = false
    var syncService = _factory.Services.GetRequiredService<IOcpiSyncService>();
    var result = await syncService.SyncModuleFromCpoAsync("DE:CPO", "locations");

    result.IsSuccess.Should().BeFalse();
    result.ItemCount.Should().Be(0);

    await server.DisposeAsync();
}
```

## Testing Tips

- Use `IAsyncLifetime` for test setup/teardown — the simulator is async
- Each test should create its own simulator to avoid shared state
- Use `FakeTimeProvider` to test time-dependent behavior without real delays
- Tag security tests with `[Trait("Category", "Security")]` for filtered CI runs
- The simulator uses a random port — always use `_server.BaseUrl` instead of hardcoding
