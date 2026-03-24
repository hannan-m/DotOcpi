---
title: Quick Start
layout: default
parent: Getting Started
nav_order: 2
---

# Quick Start
{: .no_toc }

Build a working eMSP in under 5 minutes.
{: .fs-5 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## 1. Create a New Project

```bash
dotnet new web -n MyEmsp
cd MyEmsp
dotnet add package DotOcpi
dotnet add package DotOcpi.AspNetCore
dotnet add package DotOcpi.Client
```

## 2. Register Services

In `Program.cs`:

```csharp
using DotOcpi;

var builder = WebApplication.CreateBuilder(args);

// Register DotOcpi services
builder.Services.AddDotOcpi(options =>
{
    options.SupportedVersions = [OcpiVersion.V2_2_1];
    options.DefaultEmspIdentity = new PartyIdentity("NL", "MSP");
    options.BaseUrl = new Uri("https://my-emsp.com/ocpi");
})
.AddInMemoryTokenStore()
.AddInMemoryCpoRegistry()
.AddAspNetCoreServer()
.AddClient();

// Register your module handlers (see step 3)
builder.Services.AddSingleton<ILocationsReceiver, MyLocationsReceiver>();
builder.Services.AddSingleton<ISessionsReceiver, MySessionsReceiver>();
builder.Services.AddSingleton<ICdrsReceiver, MyCdrsReceiver>();
builder.Services.AddSingleton<ITariffsReceiver, MyTariffsReceiver>();

var app = builder.Build();

// Map OCPI endpoints
app.MapAllOcpiEndpoints();

app.Run();
```

## 3. Implement Module Handlers

Create handlers for the data you want to receive from CPOs. At minimum, implement `ILocationsReceiver`:

```csharp
using System.Text.Json;
using DotOcpi;

public class MyLocationsReceiver : ILocationsReceiver
{
    private readonly ILogger<MyLocationsReceiver> _logger;

    public MyLocationsReceiver(ILogger<MyLocationsReceiver> logger)
    {
        _logger = logger;
    }

    public Task<OcpiResult> OnLocationPutAsync(
        OcpiRequestContext context, string locationId, object data, CancellationToken ct)
    {
        _logger.LogInformation("Received location {LocationId} from {CpoId}", locationId, context.CpoId);
        // Store the location in your database
        return Task.FromResult(OcpiResult.Success());
    }

    public Task<OcpiResult> OnLocationPatchAsync(
        OcpiRequestContext context, string locationId, JsonElement patch, CancellationToken ct)
    {
        _logger.LogInformation("Location {LocationId} updated by {CpoId}", locationId, context.CpoId);
        // Apply the patch to your stored location
        return Task.FromResult(OcpiResult.Success());
    }

    public Task<OcpiResult> OnEvsePutAsync(
        OcpiRequestContext context, string locationId, string evseUid, object data, CancellationToken ct)
    {
        return Task.FromResult(OcpiResult.Success());
    }

    public Task<OcpiResult> OnEvsePatchAsync(
        OcpiRequestContext context, string locationId, string evseUid, JsonElement patch, CancellationToken ct)
    {
        return Task.FromResult(OcpiResult.Success());
    }

    public Task<OcpiResult> OnConnectorPutAsync(
        OcpiRequestContext context, string locationId, string evseUid, string connectorId,
        object data, CancellationToken ct)
    {
        return Task.FromResult(OcpiResult.Success());
    }

    public Task<OcpiResult> OnConnectorPatchAsync(
        OcpiRequestContext context, string locationId, string evseUid, string connectorId,
        JsonElement patch, CancellationToken ct)
    {
        return Task.FromResult(OcpiResult.Success());
    }

    public Task<OcpiResult<object>> GetLocationAsync(
        OcpiRequestContext context, string locationId, CancellationToken ct)
    {
        // Return a stored location, or failure if not found
        return Task.FromResult(
            OcpiResult<object>.Failure(new OcpiStatusCode(2003), "Location not found"));
    }
}
```

## 4. Register with a CPO

Once your application is running, register with a CPO using the `IRegistrationClient`:

```csharp
// Inject IRegistrationClient (or resolve from IServiceProvider)
var result = await registrationClient.RegisterAsync(new RegistrationRequest(
    VersionsUrl: "https://cpo.example.com/ocpi/versions",
    TokenA: "pre-shared-token-from-cpo",
    EmspCountryCode: "NL",
    EmspPartyId: "MSP",
    EmspVersionsUrl: "https://my-emsp.com/ocpi/versions",
    EmspBusinessName: "My eMSP"
));

// result.Connection contains the negotiated connection details
// result.CpoToken is the raw Token B from the CPO (store securely!)
Console.WriteLine($"Connected to {result.Connection.ConnectionKey} via OCPI {result.Connection.Version}");
```

The registration orchestrator automatically:
1. Discovers the CPO's supported versions
2. Negotiates the highest mutual version
3. Exchanges credentials (Token A &rarr; Token B)
4. Stores the connection in the CPO registry

## 5. Pull Data from the CPO

```csharp
// Inject IOcpiClient
await foreach (var location in ocpiClient.Locations.GetAllLocationsAsync("DE:CPO"))
{
    Console.WriteLine($"Location: {location}");
}

// Pull with date filter (incremental sync)
var since = DateTimeOffset.UtcNow.AddDays(-1);
await foreach (var session in ocpiClient.Sessions.GetAllSessionsAsync("DE:CPO", dateFrom: since))
{
    Console.WriteLine($"Session: {session}");
}
```

Pagination is handled automatically — `GetAllLocationsAsync` returns `IAsyncEnumerable<T>` and follows `Link` headers across pages.

## 6. Send Commands

```csharp
var result = await ocpiClient.Commands.SendStartSessionAsync("DE:CPO", new
{
    response_url = "https://my-emsp.com/ocpi/commands/START_SESSION/callback",
    token = new { uid = "TOKEN001", type = "RFID" },
    location_id = "LOC001",
});

if (result.IsSuccess)
{
    Console.WriteLine("Command accepted by CPO");
    // The CPO will call back to response_url with the final result
}
```

## What's Next?

- [Configuration Reference](/DotOcpi/getting-started/configuration/) — customize versions, logging, security
- [Registration Guide](/DotOcpi/guides/registration/) — understand the full registration flow
- [Module Guides](/DotOcpi/modules/) — deep dive into each OCPI module
- [Testing Guide](/DotOcpi/advanced/testing/) — test against a fake CPO server

---

<div style="display: flex; justify-content: space-between; margin-top: 2rem;">
  <div>← <a href="/DotOcpi/getting-started/installation/">Installation</a></div>
  <div><a href="/DotOcpi/getting-started/configuration/">Configuration</a> →</div>
</div>
