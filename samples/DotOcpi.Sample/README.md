# DotOcpi Sample Dashboard

An interactive ASP.NET Core Razor Pages dashboard demonstrating the full DotOcpi library — OCPI eMSP management with real-time updates.

## Quick Start

```bash
dotnet run --project samples/DotOcpi.Sample -p:NuGetAudit=false
```

Open **http://localhost:5050** in your browser. Swagger UI at **http://localhost:5050/swagger**.

Two simulated CPOs (DE:CPO on OCPI 2.2.1, FR:ION on OCPI 2.1.1) auto-register on startup with full data.

## Pages

| Page | URL | Description |
|------|-----|-------------|
| Dashboard | `/` | Summary cards, activity feed, quick actions |
| CPO Registry | `/Cpos` | Registered CPOs, manual registration form |
| CPO Detail | `/Cpos/Detail?key=DE:CPO` | Per-CPO data with module tabs |
| Locations | `/Locations` | All charging stations with EVSE/connector details |
| Sessions | `/Sessions` | Charging sessions with status filtering |
| Commands | `/Commands` | Send StartSession/StopSession, command history |
| Tariffs | `/Tariffs` | Pricing structures with element breakdown |
| CDRs | `/Cdrs` | Charge Detail Records with cost breakdown |
| Sync Status | `/Sync` | Per-CPO sync status with manual sync triggers |
| Metrics | `/Metrics` | Request counts, error rates, data store stats |

## API Endpoints

| Method | URL | Description |
|--------|-----|-------------|
| GET | `/api/status` | JSON overview of connections and data counts |
| GET | `/api/metrics` | Aggregated operational metrics |
| GET | `/api/events` | SSE stream for real-time updates |
| POST | `/api/cpos/register` | Register a new CPO (form: versionsUrl, tokenA) |
| POST | `/api/commands/start-session` | Send StartSession command |
| POST | `/api/commands/stop-session` | Send StopSession command |
| GET | `/api/commands/history` | Recent command history (HTML) |
| POST | `/api/sync/{cpoId}/{module}` | Trigger sync for one CPO module |
| POST | `/api/sync/{cpoId}` | Trigger sync for all modules of a CPO |
| POST | `/api/sync/all` | Trigger sync for all CPOs |
| GET | `/api/data/locations` | Stored locations as OCPI JSON |
| GET | `/api/data/tariffs` | Stored tariffs as OCPI JSON |
| GET | `/swagger` | Swagger UI for API documentation |

## Features Demonstrated

- Multi-version OCPI (2.2.1 + 2.1.1) with version negotiation
- Full credentials handshake (Token A → B exchange)
- Pull sync with background service (30s intervals)
- Push tokens to CPO
- Send commands (StartSession/StopSession) with session lifecycle simulation
- CDR generation on session completion
- Real-time updates via Server-Sent Events
- HTMX for interactive UI without JavaScript frameworks
- Per-CPO sync configuration

## Architecture

```
Program.cs          → DI setup, Razor Pages, Minimal API endpoints, Swagger
Services/
  CpoSimulator      → Manages simulated CPO servers (auto-starts on launch)
  AutoRegistration   → Registers CPOs + initial sync on startup
  InMemoryDataStore  → ConcurrentDictionary store for all module data
  SseEventService    → SSE connection management + event broadcasting
  MetricsCollector   → Per-CPO request/error/sync tracking
  CommandHistory     → Recent command tracking
  CredentialsHelper  → Version-aware credential building
  SampleTokenProvider → In-memory outbound token storage
Handlers/
  SampleLocationsReceiver, SampleSessionsReceiver, etc.
  SampleSyncHandler  → Stores synced data + broadcasts SSE events
  SampleCommandsCallback, SampleChargingProfilesCallback
Pages/
  Razor Pages with HTMX for interactivity
```

## Technology

- ASP.NET Core 10 Razor Pages
- HTMX 2.0.4 (vendored, no CDN dependency)
- Server-Sent Events for real-time updates
- DotOcpi.Simulator for test CPO servers
- No npm, no node, no build tools
