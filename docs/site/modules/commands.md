---
title: Commands
layout: default
parent: Modules
nav_order: 7
---

# Commands Module
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Overview

The Commands module lets your eMSP send charging commands to CPOs. Commands follow an asynchronous pattern: your eMSP sends a command, the CPO acknowledges it, then calls back with the final result.

{: .note }
> Commands are not supported in OCPI 2.0. Available from OCPI 2.1.1 onwards.

## Available Commands

| Command | Description | Available |
|:--------|:------------|:----------|
| `StartSession` | Start a charging session | 2.1.1+ |
| `StopSession` | Stop an active charging session | 2.1.1+ |
| `ReserveNow` | Reserve a specific EVSE/connector | 2.1.1+ |
| `UnlockConnector` | Remotely unlock a connector | 2.1.1+ |
| `CancelReservation` | Cancel an existing reservation | 2.2.1 only |

## Sending Commands

See [Sending Commands Guide](/DotOcpi/guides/sending-commands/) for detailed examples.

```csharp
// Start session
await ocpiClient.Commands.SendStartSessionAsync("DE:CPO", new
{
    response_url = "https://my-emsp.com/ocpi/commands/START_SESSION/cb",
    token = new { uid = "TOKEN001", type = "RFID" },
    location_id = "LOC001",
});

// Stop session
await ocpiClient.Commands.SendStopSessionAsync("DE:CPO", new
{
    response_url = "https://my-emsp.com/ocpi/commands/STOP_SESSION/cb",
    session_id = "SESSION001",
});

// Reserve
await ocpiClient.Commands.SendReserveNowAsync("DE:CPO", new
{
    response_url = "https://my-emsp.com/ocpi/commands/RESERVE_NOW/cb",
    token = new { uid = "TOKEN001", type = "RFID" },
    expiry_date = DateTimeOffset.UtcNow.AddMinutes(15),
    reservation_id = "RES001",
    location_id = "LOC001",
});

// Unlock
await ocpiClient.Commands.SendUnlockConnectorAsync("DE:CPO", new
{
    response_url = "https://my-emsp.com/ocpi/commands/UNLOCK_CONNECTOR/cb",
    location_id = "LOC001",
    evse_uid = "EVSE001",
    connector_id = "CONN001",
});

// Cancel reservation (2.2.1 only)
await ocpiClient.Commands.SendCancelReservationAsync("DE:CPO", new
{
    response_url = "https://my-emsp.com/ocpi/commands/CANCEL_RESERVATION/cb",
    reservation_id = "RES001",
});
```

## Handling Callbacks

```csharp
public class MyCommandsCallback : ICommandsCallback
{
    public async Task<OcpiResult> OnCommandResultAsync(
        OcpiRequestContext context, string correlationId, object result, CancellationToken ct)
    {
        // Process the async command result from the CPO
        _logger.LogInformation("Command {Id} result from {Cpo}", correlationId, context.CpoId);
        return OcpiResult.Success();
    }
}
```

## Command Response vs Result

| Type | When | Contains |
|:-----|:-----|:---------|
| `CommandResponse` | Immediate (synchronous) | Whether command was accepted/rejected |
| `CommandResult` | Later (async callback) | Final outcome of the command |

### CommandResponse Values

| Value | Description |
|:------|:------------|
| `NOT_SUPPORTED` | Command not supported by CPO |
| `REJECTED` | Command rejected |
| `ACCEPTED` | Command accepted, will process |
| `UNKNOWN_SESSION` | Session ID not found |

### CommandResult Values

| Value | Description |
|:------|:------------|
| `ACCEPTED` | Command successfully executed |
| `CANCELED_RESERVATION` | Reservation was canceled |
| `EVSE_OCCUPIED` | EVSE already in use |
| `EVSE_INOPERATIVE` | EVSE not operational |
| `FAILED` | Command failed |
| `NOT_SUPPORTED` | Not supported |
| `REJECTED` | Rejected after acceptance |
| `TIMEOUT` | Command timed out |
| `UNKNOWN_RESERVATION` | Reservation not found |

## Version Differences

| Feature | 2.1.1 | 2.2 | 2.2.1 |
|:--------|:------|:----|:------|
| CancelReservation | - | - | Yes |
| `reservation_id` type | `int` | `string` | `string` |
| CommandResponse/Result split | Combined | Separate types | Separate types |
