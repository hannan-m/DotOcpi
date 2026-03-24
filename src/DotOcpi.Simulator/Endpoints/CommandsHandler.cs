using System.Text.Json;
using DotOcpi.Simulator.Charging;
using DotOcpi.Simulator.Infrastructure;
using DotOcpi.Simulator.Models;
using DotOcpi.Simulator.State;
using Microsoft.AspNetCore.Http;

namespace DotOcpi.Simulator.Endpoints;

internal static class CommandsHandler
{
    public static async Task HandleCommand(
        HttpContext ctx,
        SimulatorState state,
        CpoSimulatorConfiguration config,
        Action<string, string?, object?>? onSessionEvent
    )
    {
        await FailureInjectionHelper.ApplyAsync(ctx, config).ConfigureAwait(false);
        if (!await AuthHelper.ValidateModuleAuthAsync(ctx, state, config).ConfigureAwait(false))
            return;

        var responseStatus = config.CommandResponseStatus;
        var body = RequestRecordingMiddleware.GetParsedObjectBody(ctx);
        var path = ctx.Request.Path.Value ?? "";

        // Session lifecycle in typed mode
        if (!config.IsLegacyMode && responseStatus == "ACCEPTED" && body.HasValue)
        {
            if (path.Contains("START_SESSION"))
                HandleStartSession(body.Value, state, config, onSessionEvent);
            else if (path.Contains("STOP_SESSION"))
                HandleStopSession(body.Value, state, config, onSessionEvent);
            else if (path.Contains("RESERVE_NOW"))
                HandleReserveNow(body.Value, state, config);
            else if (path.Contains("CANCEL_RESERVATION"))
                HandleCancelReservation(body.Value, state, config, onSessionEvent);
        }
        else if (config.IsLegacyMode && responseStatus == "ACCEPTED" && body.HasValue)
        {
            // Legacy session lifecycle (adds anonymous objects to config.Sessions)
            if (path.Contains("START_SESSION"))
                LegacyStartSession(body.Value, state, config);
            else if (path.Contains("STOP_SESSION"))
                LegacyStopSession(body.Value, state, config);
        }

        // Extract response_url for async callback
        string? responseUrl = null;
        if (
            config.CommandCallbackEnabled
            && body.HasValue
            && body.Value.TryGetProperty("response_url", out var urlProp)
        )
            responseUrl = urlProp.GetString();

        await OcpiResponseWriter
            .WriteSuccessAsync(
                ctx,
                static (writer, status) =>
                {
                    writer.WriteStartObject("data"u8);
                    writer.WriteString("result"u8, status);
                    writer.WriteNumber("timeout"u8, 30);
                    writer.WriteEndObject();
                },
                responseStatus
            )
            .ConfigureAwait(false);

        // Fire async callback
        if (responseUrl is not null && (config.AllowLoopbackCallbacks || SsrfGuard.IsUrlSafeForOutbound(responseUrl)))
            onSessionEvent?.Invoke("COMMAND_CALLBACK", responseUrl, responseStatus);
    }

    private static void HandleStartSession(
        JsonElement body,
        SimulatorState state,
        CpoSimulatorConfiguration config,
        Action<string, string?, object?>? onEvent
    )
    {
        var locationId = body.TryGetProperty("location_id", out var loc) ? loc.GetString() ?? "LOC1" : "LOC1";
        var evseUid = body.TryGetProperty("evse_uid", out var evse) ? evse.GetString() : null;
        var tokenUid = "TOKEN001";
        var contractId = "CONTRACT001";
        if (body.TryGetProperty("token", out var token))
        {
            if (token.TryGetProperty("uid", out var uid))
                tokenUid = uid.GetString() ?? "TOKEN001";
            if (token.TryGetProperty("contract_id", out var cid))
                contractId = cid.GetString() ?? contractId;
            else if (token.TryGetProperty("auth_id", out var aid))
                contractId = aid.GetString() ?? contractId;
        }

        // Find EVSE
        var locationSpec = state.FindLocation(locationId);
        if (locationSpec is null)
            return;

        EvseSpec? targetEvse = null;
        foreach (var e in locationSpec.Evses)
        {
            if (evseUid is null || e.Uid == evseUid)
            {
                targetEvse = e;
                break;
            }
        }
        if (targetEvse is null)
            return;

        var evseKey = $"{locationId}:{targetEvse.Uid}";
        if (!state.Evses.TryGetValue(evseKey, out var evseState))
            return;

        var (newStatus, allowed) = EvseStateMachine.Transition(evseState.Status, EvseEvent.StartSession);
        if (!allowed)
            return;

        var sessionId = $"SES-{Guid.NewGuid():N}"[..12];
        evseState.Update(newStatus, activeSessionId: sessionId);

        var session = new SessionState
        {
            SessionId = sessionId,
            LocationId = locationId,
            EvseUid = targetEvse.Uid,
            ConnectorId = targetEvse.Connector.Id,
            TokenUid = tokenUid,
            TokenContractId = contractId,
            StartTime = DateTimeOffset.UtcNow,
            LastUpdated = DateTimeOffset.UtcNow,
            ConnectionId = "default",
        };

        state.Sessions[sessionId] = session;

        onEvent?.Invoke("SESSION_STARTED", sessionId, null);
    }

    private static void HandleStopSession(
        JsonElement body,
        SimulatorState state,
        CpoSimulatorConfiguration config,
        Action<string, string?, object?>? onEvent
    )
    {
        var sessionId = body.TryGetProperty("session_id", out var sid) ? sid.GetString() : null;
        if (sessionId is null || !state.Sessions.TryGetValue(sessionId, out var session))
            return;

        session.WithLock(s =>
        {
            s.Phase = SessionPhase.Completed;
            s.EndTime = DateTimeOffset.UtcNow;
            s.LastUpdated = DateTimeOffset.UtcNow;
        });

        var evseKey = $"{session.LocationId}:{session.EvseUid}";
        if (state.Evses.TryGetValue(evseKey, out var evseState))
        {
            var (newStatus, _) = EvseStateMachine.Transition(evseState.Status, EvseEvent.StopSession);
            evseState.ClearSession(newStatus);
        }

        // Generate CDR
        var conn = state.DefaultConnection;
        var version = conn?.NegotiatedVersion ?? config.SupportedVersions[0];
        var loc = state.FindLocation(session.LocationId) ?? LocationSpec.Default;
        var cdr = VersionModelBuilder.BuildCdr(
            version,
            session,
            config.CpoIdentity,
            config.EmspIdentity,
            loc,
            config.CpoCurrency,
            config.DefaultVatRate
        );
        lock (config.Cdrs)
        {
            config.Cdrs.Add(cdr);
        }

        onEvent?.Invoke("SESSION_STOPPED", sessionId, cdr);
    }

    private static void HandleReserveNow(JsonElement body, SimulatorState state, CpoSimulatorConfiguration config)
    {
        var locationId = body.TryGetProperty("location_id", out var loc) ? loc.GetString() ?? "LOC1" : "LOC1";
        var evseUid = body.TryGetProperty("evse_uid", out var evse) ? evse.GetString() : null;
        var reservationId = body.TryGetProperty("reservation_id", out var rid)
            ? rid.GetString() ?? Guid.NewGuid().ToString("N")[..8]
            : Guid.NewGuid().ToString("N")[..8];
        var tokenUid = "TOKEN001";
        if (body.TryGetProperty("token", out var token) && token.TryGetProperty("uid", out var uid))
            tokenUid = uid.GetString() ?? "TOKEN001";

        var locationSpec = state.FindLocation(locationId);
        if (locationSpec is null)
            return;

        EvseSpec? targetEvse = null;
        foreach (var e in locationSpec.Evses)
        {
            if (evseUid is null || e.Uid == evseUid)
            {
                targetEvse = e;
                break;
            }
        }
        if (targetEvse is null)
            return;

        var evseKey = $"{locationId}:{targetEvse.Uid}";
        if (!state.Evses.TryGetValue(evseKey, out var evseState))
            return;

        var (newStatus, allowed) = EvseStateMachine.Transition(evseState.Status, EvseEvent.ReserveNow);
        if (!allowed)
            return;

        evseState.Update(newStatus, activeReservationId: reservationId);

        var expiry =
            body.TryGetProperty("expiry_date", out var exp) && DateTimeOffset.TryParse(exp.GetString(), out var ed)
                ? ed
                : DateTimeOffset.UtcNow.Add(config.ReservationTimeout);

        state.Reservations[reservationId] = new ReservationState
        {
            ReservationId = reservationId,
            LocationId = locationId,
            EvseUid = targetEvse.Uid,
            TokenUid = tokenUid,
            ExpiryDate = expiry,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    private static void HandleCancelReservation(
        JsonElement body,
        SimulatorState state,
        CpoSimulatorConfiguration config,
        Action<string, string?, object?>? onEvent
    )
    {
        var reservationId = body.TryGetProperty("reservation_id", out var rid) ? rid.GetString() : null;
        if (reservationId is null || !state.Reservations.TryRemove(reservationId, out var reservation))
            return;

        var evseKey = $"{reservation.LocationId}:{reservation.EvseUid}";
        if (state.Evses.TryGetValue(evseKey, out var evseState))
        {
            var (newStatus, _) = EvseStateMachine.Transition(evseState.Status, EvseEvent.CancelReservation);
            evseState.Update(newStatus, clearReservation: true);
        }
    }

    // ── Legacy session lifecycle (anonymous objects) ──────────

    private static void LegacyStartSession(JsonElement body, SimulatorState state, CpoSimulatorConfiguration config)
    {
        var locationId = body.TryGetProperty("location_id", out var loc) ? loc.GetString() ?? "LOC1" : "LOC1";
        var evseUid = body.TryGetProperty("evse_uid", out var evse) ? evse.GetString() ?? "EVSE001" : "EVSE001";
        var tokenUid = "TOKEN001";
        if (body.TryGetProperty("token", out var token) && token.TryGetProperty("uid", out var uid))
            tokenUid = uid.GetString() ?? "TOKEN001";

        var sessionId = $"SES-{Guid.NewGuid():N}"[..12];
        var now = DateTimeOffset.UtcNow;

        var conn = state.DefaultConnection;
        var version = conn?.NegotiatedVersion ?? config.SupportedVersions[0];

        object session;
        if (version.UsesPartyIdInUrls())
        {
            session = new
            {
                country_code = config.CpoIdentity.CountryCode,
                party_id = config.CpoIdentity.PartyId,
                id = sessionId,
                start_date_time = OcpiDateTime.Format(now),
                kwh = 0.0m,
                cdr_token = new
                {
                    country_code = config.EmspIdentity.CountryCode,
                    party_id = config.EmspIdentity.PartyId,
                    uid = tokenUid,
                    type = "RFID",
                    contract_id = $"{config.EmspIdentity.CountryCode}-{config.EmspIdentity.PartyId}-000001",
                },
                auth_method = "COMMAND",
                location_id = locationId,
                evse_uid = evseUid,
                connector_id = "1",
                currency = config.CpoCurrency,
                total_cost = new { excl_vat = 0.0m },
                status = "ACTIVE",
                last_updated = OcpiDateTime.Format(now),
            };
        }
        else
        {
            // V2.0/V2.1.1: uses auth_id, embedded Location object, decimal total_cost
            var locationObj = FindLegacyLocationById(config.Locations, locationId);
            session = new
            {
                id = sessionId,
                start_date_time = OcpiDateTime.Format(now),
                kwh = 0.0m,
                auth_id = $"{config.EmspIdentity.CountryCode}-{config.EmspIdentity.PartyId}-000001",
                auth_method = "AUTH_REQUEST",
                location = locationObj
                    ?? (object)
                        new
                        {
                            id = locationId,
                            address = "Unknown",
                            city = "Unknown",
                            postal_code = "00000",
                            country = "UNK",
                            coordinates = new { latitude = "0.0", longitude = "0.0" },
                        },
                currency = config.CpoCurrency,
                total_cost = 0.0m,
                status = "ACTIVE",
                last_updated = OcpiDateTime.Format(now),
            };
        }

        lock (config.Sessions)
        {
            config.Sessions.Add(session);
        }
    }

    private static object? FindLegacyLocationById(List<object> locations, string id)
    {
        foreach (var item in locations)
        {
            var json = JsonSerializer.SerializeToElement(item);
            if (json.TryGetProperty("id", out var idProp) && idProp.GetString() == id)
                return item;
        }
        return null;
    }

    /// <summary>
    /// Builds a V2.2+ cdr_location object from legacy location config data.
    /// Extracts actual address, coordinates, and connector details from the location.
    /// </summary>
    private static object BuildLegacyCdrLocation(List<object> locations, string locationId, string evseUid)
    {
        var locObj = FindLegacyLocationById(locations, locationId);
        if (locObj is null)
        {
            return new
            {
                id = locationId,
                address = "Unknown",
                city = "Unknown",
                country = "UNK",
                coordinates = new { latitude = "0.0", longitude = "0.0" },
                evse_uid = evseUid,
                evse_id = evseUid,
                connector_id = "1",
                connector_standard = "IEC_62196_T2_COMBO",
                connector_format = "CABLE",
                connector_power_type = "DC",
            };
        }

        var loc = JsonSerializer.SerializeToElement(locObj);
        var address = loc.TryGetProperty("address", out var a) ? a.GetString() ?? "Unknown" : "Unknown";
        var city = loc.TryGetProperty("city", out var c) ? c.GetString() ?? "Unknown" : "Unknown";
        var country = loc.TryGetProperty("country", out var co) ? co.GetString() ?? "UNK" : "UNK";
        var postalCode = loc.TryGetProperty("postal_code", out var pc) ? pc.GetString() : null;
        var lat = "0.0";
        var lon = "0.0";
        if (loc.TryGetProperty("coordinates", out var coords))
        {
            lat = coords.TryGetProperty("latitude", out var la) ? la.GetString() ?? "0.0" : "0.0";
            lon = coords.TryGetProperty("longitude", out var lo) ? lo.GetString() ?? "0.0" : "0.0";
        }

        // Find the specific EVSE and connector
        var connStandard = "IEC_62196_T2_COMBO";
        var connFormat = "CABLE";
        var connPowerType = "DC";
        var connectorId = "1";
        var evseId = evseUid;

        if (loc.TryGetProperty("evses", out var evses) && evses.ValueKind == JsonValueKind.Array)
        {
            foreach (var evse in evses.EnumerateArray())
            {
                var uid = evse.TryGetProperty("uid", out var u) ? u.GetString() : null;
                if (uid == evseUid || evses.GetArrayLength() == 1)
                {
                    if (evse.TryGetProperty("evse_id", out var eid))
                        evseId = eid.GetString() ?? evseUid;
                    if (
                        evse.TryGetProperty("connectors", out var conns)
                        && conns.ValueKind == JsonValueKind.Array
                        && conns.GetArrayLength() > 0
                    )
                    {
                        var conn = conns[0];
                        connectorId = conn.TryGetProperty("id", out var ci) ? ci.GetString() ?? "1" : "1";
                        connStandard = conn.TryGetProperty("standard", out var cs)
                            ? cs.GetString() ?? connStandard
                            : connStandard;
                        connFormat = conn.TryGetProperty("format", out var cf)
                            ? cf.GetString() ?? connFormat
                            : connFormat;
                        connPowerType = conn.TryGetProperty("power_type", out var cp)
                            ? cp.GetString() ?? connPowerType
                            : connPowerType;
                    }
                    break;
                }
            }
        }

        return new
        {
            id = locationId,
            address,
            city,
            postal_code = postalCode,
            country,
            coordinates = new { latitude = lat, longitude = lon },
            evse_uid = evseUid,
            evse_id = evseId,
            connector_id = connectorId,
            connector_standard = connStandard,
            connector_format = connFormat,
            connector_power_type = connPowerType,
        };
    }

    private static void LegacyStopSession(JsonElement body, SimulatorState state, CpoSimulatorConfiguration config)
    {
        var sessionId = body.TryGetProperty("session_id", out var sid) ? sid.GetString() : null;
        if (sessionId is null)
            return;

        var conn = state.DefaultConnection;
        var version = conn?.NegotiatedVersion ?? config.SupportedVersions[0];

        lock (config.Sessions)
        {
            for (var i = 0; i < config.Sessions.Count; i++)
            {
                var json = JsonSerializer.SerializeToElement(config.Sessions[i]);
                if (json.TryGetProperty("id", out var id) && id.GetString() == sessionId)
                {
                    var now = DateTimeOffset.UtcNow;
                    var startTime = json.TryGetProperty("start_date_time", out var st) ? st.GetString() ?? "" : "";
                    var kwh = json.TryGetProperty("kwh", out var k) ? k.GetDecimal() : 0m;
                    var durationHours = DateTimeOffset.TryParse(startTime, out var parsedSt)
                        ? Math.Max(0.01m, (decimal)(now - parsedSt).TotalHours)
                        : 0.01m;
                    // If no energy was delivered (instant stop), estimate based on duration
                    if (kwh == 0)
                        kwh = Math.Round(config.DefaultPricePerKwh > 0 ? 11m * durationHours : 0m, 1);
                    var costExcl = Math.Round(kwh * config.DefaultPricePerKwh, 2);
                    var costIncl = Math.Round(costExcl * (1 + config.DefaultVatRate), 2);

                    if (version.UsesPartyIdInUrls())
                    {
                        var locationId = json.TryGetProperty("location_id", out var loc)
                            ? loc.GetString() ?? "LOC1"
                            : "LOC1";
                        var evseUid = json.TryGetProperty("evse_uid", out var ev)
                            ? ev.GetString() ?? "EVSE001"
                            : "EVSE001";

                        config.Sessions[i] = new
                        {
                            country_code = config.CpoIdentity.CountryCode,
                            party_id = config.CpoIdentity.PartyId,
                            id = sessionId,
                            start_date_time = startTime,
                            end_date_time = OcpiDateTime.Format(now),
                            kwh,
                            cdr_token = new
                            {
                                country_code = config.EmspIdentity.CountryCode,
                                party_id = config.EmspIdentity.PartyId,
                                uid = "TOKEN001",
                                type = "RFID",
                                contract_id = $"{config.EmspIdentity.CountryCode}-{config.EmspIdentity.PartyId}-000001",
                            },
                            auth_method = "COMMAND",
                            location_id = locationId,
                            evse_uid = evseUid,
                            connector_id = "1",
                            currency = config.CpoCurrency,
                            total_cost = new { excl_vat = costExcl, incl_vat = costIncl },
                            status = "COMPLETED",
                            last_updated = OcpiDateTime.Format(now),
                        };

                        // Build cdr_location from actual config data
                        var cdrLocData = BuildLegacyCdrLocation(config.Locations, locationId, evseUid);

                        lock (config.Cdrs)
                        {
                            config.Cdrs.Add(
                                new
                                {
                                    country_code = config.CpoIdentity.CountryCode,
                                    party_id = config.CpoIdentity.PartyId,
                                    id = $"CDR-{sessionId}",
                                    start_date_time = startTime,
                                    end_date_time = OcpiDateTime.Format(now),
                                    session_id = sessionId,
                                    auth_method = "COMMAND",
                                    cdr_location = cdrLocData,
                                    currency = config.CpoCurrency,
                                    charging_periods = new[]
                                    {
                                        new
                                        {
                                            start_date_time = startTime,
                                            dimensions = new object[]
                                            {
                                                new { type = "ENERGY", volume = kwh },
                                                new { type = "TIME", volume = Math.Round(durationHours, 4) },
                                            },
                                        },
                                    },
                                    total_cost = new { excl_vat = costExcl, incl_vat = costIncl },
                                    total_energy = kwh,
                                    total_time = Math.Round(durationHours, 4),
                                    total_energy_cost = new { excl_vat = costExcl },
                                    last_updated = OcpiDateTime.Format(now),
                                }
                            );
                        }
                    }
                    else
                    {
                        // V2.0/V2.1.1: auth_id, embedded location, decimal total_cost
                        // Build a CDR-appropriate location snapshot (only the used EVSE, not all EVSEs)
                        var sessionEvseUid =
                            json.TryGetProperty("location", out var sessionLoc)
                            && sessionLoc.TryGetProperty("evses", out var sessionEvses)
                            && sessionEvses.ValueKind == JsonValueKind.Array
                            && sessionEvses.GetArrayLength() > 0
                            && sessionEvses[0].TryGetProperty("uid", out var sessionEvseUidProp)
                                ? sessionEvseUidProp.GetString()
                                : null;

                        // Use the original location from config for CDR snapshot
                        var locationId =
                            json.TryGetProperty("location", out var sesLoc)
                            && sesLoc.TryGetProperty("id", out var sesLocId)
                                ? sesLocId.GetString()
                                : null;
                        var cdrLocation = FindLegacyLocationById(config.Locations, locationId ?? "");

                        // Calculate actual duration from timestamps
                        var actualDurationHours = DateTimeOffset.TryParse(startTime, out var parsedStart)
                            ? Math.Max(0.01m, (decimal)(now - parsedStart).TotalHours)
                            : 0.01m;

                        config.Sessions[i] = new
                        {
                            id = sessionId,
                            start_date_time = startTime,
                            end_date_time = OcpiDateTime.Format(now),
                            kwh,
                            auth_id = $"{config.EmspIdentity.CountryCode}-{config.EmspIdentity.PartyId}-000001",
                            auth_method = "AUTH_REQUEST",
                            location = cdrLocation
                                ?? (object)
                                    new
                                    {
                                        id = locationId ?? "LOC1",
                                        address = "Unknown",
                                        city = "Unknown",
                                        postal_code = "00000",
                                        country = "UNK",
                                        coordinates = new { latitude = "0.0", longitude = "0.0" },
                                    },
                            currency = config.CpoCurrency,
                            total_cost = costExcl,
                            status = "COMPLETED",
                            last_updated = OcpiDateTime.Format(now),
                        };

                        lock (config.Cdrs)
                        {
                            config.Cdrs.Add(
                                new
                                {
                                    id = $"CDR-{sessionId}",
                                    start_date_time = startTime,
                                    end_date_time = OcpiDateTime.Format(now),
                                    auth_id = $"{config.EmspIdentity.CountryCode}-{config.EmspIdentity.PartyId}-000001",
                                    auth_method = "AUTH_REQUEST",
                                    location = cdrLocation
                                        ?? (object)
                                            new
                                            {
                                                id = locationId ?? "LOC1",
                                                address = "Unknown",
                                                city = "Unknown",
                                                postal_code = "00000",
                                                country = "UNK",
                                                coordinates = new { latitude = "0.0", longitude = "0.0" },
                                            },
                                    currency = config.CpoCurrency,
                                    charging_periods = new[]
                                    {
                                        new
                                        {
                                            start_date_time = startTime,
                                            dimensions = new object[]
                                            {
                                                new { type = "ENERGY", volume = kwh },
                                                new { type = "TIME", volume = Math.Round(actualDurationHours, 4) },
                                            },
                                        },
                                    },
                                    total_cost = costExcl,
                                    total_energy = kwh,
                                    total_time = Math.Round(actualDurationHours, 4),
                                    last_updated = OcpiDateTime.Format(now),
                                }
                            );
                        }
                    }

                    return;
                }
            }
        }
    }
}
