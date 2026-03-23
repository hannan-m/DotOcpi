# Sessions Module

The Sessions module allows CPOs to share charging session data with eMSPs. As an eMSP, DotOcpi acts as a **Receiver** — it receives Session data from CPOs via push and can pull via GET.

## eMSP Role Summary

| Operation | Direction | Description |
|---|---|---|
| **Receive PUT** | CPO → eMSP | CPO pushes a full Session object |
| **Receive PATCH** | CPO → eMSP | CPO sends a partial update (e.g., kwh, status change) |
| **Receive GET** | CPO → eMSP | CPO retrieves a previously pushed Session |
| **Client GET (list)** | eMSP → CPO | eMSP pulls all sessions (paginated) |
| **Client PUT ChargingPreferences** | eMSP → CPO | eMSP sends charging preferences (2.2+ only) |

## Endpoint URL Patterns

### eMSP Receiver Endpoints (server-side)

| Version | URL Pattern |
|---|---|
| 2.0 / 2.1.1 | `{base}/sessions/{session_id}` |
| 2.2 / 2.2.1 | `{base}/sessions/{country_code}/{party_id}/{session_id}` |

### CPO Sender Endpoints (client-side pull)

| Version | URL Pattern |
|---|---|
| All | `{cpo_sessions_url}?[date_from=][&date_to=][&offset=][&limit=]` |
| 2.2+ | `{cpo_sessions_url}/{session_id}/charging_preferences` (PUT) |

---

## Session Object

### Version 2.0

| Field | Type | Required | Description |
|---|---|---|---|
| id | string(15) | Yes | Unique session ID |
| start_datetime | DateTime | Yes | Session start time |
| end_datetime | DateTime | No | Session end time (null while active) |
| kwh | decimal | Yes | Energy delivered in kWh |
| auth_id | string(32) | Yes | Token used for authorization |
| auth_method | AuthMethod | Yes | How the session was authorized |
| location | Location | Yes | Full Location object (embedded) |
| meter_id | string(255) | No | Meter identification |
| currency | string(3) | Yes | ISO 4217 currency code |
| charging_periods | ChargingPeriod[] | No | Charging period breakdown |
| total_cost | decimal | Yes | Total cost (required in 2.0) |
| status | SessionStatus | Yes | Current session status |

### Version 2.1.1

| Field | Type | Required | Description |
|---|---|---|---|
| id | string(36) | Yes | Unique session ID |
| start_datetime | DateTime | Yes | Session start time |
| end_datetime | DateTime | No | Session end time |
| kwh | number | Yes | Energy delivered in kWh |
| auth_id | string(36) | Yes | Token auth_id |
| auth_method | AuthMethod | Yes | Authorization method |
| location | Location | Yes | Full Location object (embedded) |
| meter_id | string(255) | No | Meter identification |
| currency | string(3) | Yes | ISO 4217 currency code |
| charging_periods | ChargingPeriod[] | No | Charging period breakdown |
| total_cost | number | **No** | Total cost **(changed to optional)** |
| status | SessionStatus | Yes | Current session status |
| **last_updated** | DateTime | Yes | **Added** |

### Version 2.2 / 2.2.1

| Field | Type | Required | Description |
|---|---|---|---|
| **country_code** | CiString(2) | Yes | **Added: CPO's country code** |
| **party_id** | CiString(3) | Yes | **Added: CPO's party ID** |
| id | CiString(36) | Yes | Unique session ID |
| **start_date_time** | DateTime | Yes | **Renamed from `start_datetime`** |
| **end_date_time** | DateTime | No | **Renamed from `end_datetime`** |
| kwh | number | Yes | Energy delivered in kWh |
| **cdr_token** | CdrToken | Yes | **Replaces `auth_id`** |
| auth_method | AuthMethod | Yes | Authorization method |
| **authorization_reference** | CiString(36) | No | **Added: Links to authorize response** |
| **location_id** | CiString(36) | Yes | **Replaces embedded Location** |
| **evse_uid** | CiString(36) | Yes | **Added: EVSE reference** |
| **connector_id** | CiString(36) | Yes | **Added: Connector reference** |
| meter_id | string(255) | No | Meter identification |
| currency | string(3) | Yes | ISO 4217 currency code |
| charging_periods | ChargingPeriod[] | No | Charging period breakdown |
| total_cost | **Price** | No | **Type changed from number to Price object** |
| status | SessionStatus | Yes | Current session status |
| last_updated | DateTime | Yes | Last modification |

**Key changes from 2.1.1:** Replaced `auth_id` with `cdr_token` (CdrToken object). Replaced embedded `Location` object with `location_id`/`evse_uid`/`connector_id` references. Field names use underscores consistently (`start_date_time`). `total_cost` type changed from number to Price.

---

## Related Types

### ChargingPeriod / CdrDimension (all versions)

See [cdrs.md — ChargingPeriod](cdrs.md#chargingperiod-all-versions) and [cdrs.md — CdrDimension](cdrs.md#cdrdimension) for definitions. These types are shared between Sessions and CDRs.

### AuthMethod (all versions)

See [cdrs.md — AuthMethod](cdrs.md#authmethod) for the full enum with version availability.

### CdrToken (2.2+ only)

| Field | Type | Required | Description |
|---|---|---|---|
| country_code | CiString(2) | Yes (2.2.1 only) | Token owner's country code |
| party_id | CiString(3) | Yes (2.2.1 only) | Token owner's party ID |
| uid | CiString(36) | Yes | Token UID |
| type | TokenType | Yes | RFID, APP_USER, etc. |
| contract_id | CiString(36) | Yes | Contract ID (was auth_id) |

> **Note:** `country_code` and `party_id` were NOT present in CdrToken in OCPI 2.2. They were added in 2.2.1.

### SessionStatus

| Value | 2.0 | 2.1.1 | 2.2 | 2.2.1 |
|---|:---:|:---:|:---:|:---:|
| ACTIVE | Y | Y | Y | Y |
| COMPLETED | Y | Y | Y | Y |
| INVALID | Y | Y | Y | Y |
| PENDING | Y | Y | Y | Y |
| RESERVATION | - | - | Y | Y |

### ChargingPreferences (2.2+ only)

| Field | Type | Required | Description |
|---|---|---|---|
| profile_type | ProfileType | Yes | CHEAP, FAST, GREEN, REGULAR |
| departure_time | DateTime | No | Desired departure time |
| energy_need | number | No | Amount of energy needed in kWh |
| discharge_allowed | boolean | No | **2.2.1 only**: Whether V2G discharge is allowed |

### ChargingPreferencesResponse (2.2+ only)

`ACCEPTED`, `DEPARTURE_REQUIRED`, `ENERGY_NEED_REQUIRED`, `NOT_POSSIBLE`, `PROFILE_TYPE_NOT_SUPPORTED`

---

## Consumer Interface

```csharp
public interface ISessionsReceiver
{
    Task<OcpiResult> OnSessionPutAsync(
        OcpiRequestContext context,
        string sessionId,
        object data,  // V2_0.Session | V2_1_1.Session | V2_2.Session | V2_2_1.Session
        CancellationToken ct);

    Task<OcpiResult> OnSessionPatchAsync(
        OcpiRequestContext context,
        string sessionId,
        JsonElement patch,
        CancellationToken ct);

    Task<OcpiResult<object>> GetSessionAsync(
        OcpiRequestContext context,
        string sessionId,
        CancellationToken ct);
}
```

## Validation Rules

- `id` must not be empty or exceed max length
- `start_date_time` must be a valid ISO 8601 timestamp
- `end_date_time` if present, must be after `start_date_time`
- `kwh` must be >= 0
- `currency` must be a valid ISO 4217 code
- `status` must be a valid SessionStatus value
- `cdr_token` (2.2+) must have valid `uid` and `type`
- `location_id`, `evse_uid`, `connector_id` (2.2+) must not be empty
- URL path `country_code`/`party_id` must match body values (2.2+)
- PATCH must contain at least one field besides identifiers
