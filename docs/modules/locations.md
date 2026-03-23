# Locations Module

The Locations module allows CPOs to share their charging infrastructure data with eMSPs. As an eMSP, DotOcpi acts as a **Receiver** — it receives Location data from CPOs via push (PUT/PATCH) and can pull data via GET.

## eMSP Role Summary

| Operation | Direction | Description |
|---|---|---|
| **Receive PUT** | CPO → eMSP | CPO pushes a full Location/EVSE/Connector object |
| **Receive PATCH** | CPO → eMSP | CPO sends a partial update |
| **Receive GET** | CPO → eMSP | CPO retrieves a previously pushed object |
| **Client GET (list)** | eMSP → CPO | eMSP pulls all locations (paginated) |
| **Client GET (object)** | eMSP → CPO | eMSP pulls a specific Location/EVSE/Connector |

## Endpoint URL Patterns

### eMSP Receiver Endpoints (server-side)

| Version | URL Pattern |
|---|---|
| 2.0 | `{base}/locations/{location_id}[/{evse_uid}[/{connector_id}]]` |
| 2.1.1 | `{base}/locations/{location_id}[/{evse_uid}[/{connector_id}]]` |
| 2.2 / 2.2.1 | `{base}/locations/{country_code}/{party_id}/{location_id}[/{evse_uid}[/{connector_id}]]` |

### CPO Sender Endpoints (client-side pull)

| Version | URL Pattern |
|---|---|
| All | `{cpo_locations_url}?[date_from=][&date_to=][&offset=][&limit=]` |
| 2.1.1+ | `{cpo_locations_url}/{location_id}[/{evse_uid}[/{connector_id}]]` |

---

## Location Object

### Version 2.0

| Field | Type | Required | Description |
|---|---|---|---|
| id | string(15) | Yes | Unique ID within CPO |
| type | LocationType | Yes | ON_STREET, PARKING_GARAGE, etc. |
| name | string(255) | No | Display name |
| address | string(45) | Yes | Street address |
| city | string(45) | Yes | City |
| postal_code | string(10) | Yes | Postal code |
| country | string(3) | Yes | ISO 3166-1 alpha-3 |
| coordinates | GeoLocation | Yes | Latitude/longitude |
| related_locations | AdditionalGeoLocation[] | No | Related locations (e.g., entrance) |
| evses | EVSE[] | No | Charging stations at this location |
| directions | DisplayText[] | No | Human-readable directions |
| operator | BusinessDetails | No | Operator information |
| suboperator | BusinessDetails | No | Suboperator information |
| opening_times | Hours | No | Operating hours |
| charging_when_closed | boolean | No | Whether charging is possible when closed |
| images | Image[] | No | Photos of the location |

### Version 2.1.1

| Field | Type | Required | Description |
|---|---|---|---|
| id | string(39) | Yes | Unique ID within CPO |
| type | LocationType | Yes | ON_STREET, PARKING_GARAGE, etc. |
| name | string(255) | No | Display name |
| address | string(45) | Yes | Street address |
| city | string(45) | Yes | City |
| postal_code | string(10) | Yes | Postal code |
| country | string(3) | Yes | ISO 3166-1 alpha-3 |
| coordinates | GeoLocation | Yes | Latitude/longitude |
| related_locations | AdditionalGeoLocation[] | No | Related locations |
| evses | EVSE[] | No | Charging stations |
| directions | DisplayText[] | No | Directions |
| operator | BusinessDetails | No | Operator |
| suboperator | BusinessDetails | No | Suboperator |
| **owner** | BusinessDetails | No | **Added: Owner information** |
| **facilities** | Facility[] | No | **Added: Available facilities** |
| **time_zone** | string(255) | No | **Added: IANA time zone** |
| opening_times | Hours | No | Operating hours |
| charging_when_closed | boolean | No | Charging when closed |
| images | Image[] | No | Photos |
| **energy_mix** | EnergyMix | No | **Added: Energy source mix** |
| **last_updated** | DateTime | Yes | **Added: Last modification timestamp** |

**Changes from 2.0:** Added `owner`, `facilities`, `time_zone`, `energy_mix`, `last_updated`. ID expanded from string(15) to string(39).

### Version 2.2 / 2.2.1

| Field | Type | Required | Description |
|---|---|---|---|
| **country_code** | CiString(2) | Yes | **Added: CPO's country code (ISO 3166-1 alpha-2)** |
| **party_id** | CiString(3) | Yes | **Added: CPO's party ID** |
| id | CiString(36) | Yes | Unique ID within CPO |
| **publish** | boolean | Yes | **Added: Whether to publish this location** |
| **publish_allowed_to** | PublishTokenType[] | No | **Added: Restrict publication to specific tokens** |
| name | string(255) | No | Display name |
| address | string(45) | Yes | Street address |
| city | string(45) | Yes | City |
| postal_code | string(10) | **No** | Postal code **(was required in 2.1.1)** |
| **state** | string(20) | No | **Added: State/province** |
| country | string(3) | Yes | ISO 3166-1 alpha-3 |
| coordinates | GeoLocation | Yes | Latitude/longitude |
| related_locations | AdditionalGeoLocation[] | No | Related locations |
| **parking_type** | ParkingType | No | **Replaces `type` (LocationType)** |
| evses | EVSE[] | No | Charging stations |
| directions | DisplayText[] | No | Directions |
| operator | BusinessDetails | No | Operator |
| suboperator | BusinessDetails | No | Suboperator |
| owner | BusinessDetails | No | Owner |
| facilities | Facility[] | No | Available facilities |
| time_zone | string(255) | **Yes** | IANA time zone **(was optional in 2.1.1)** |
| opening_times | Hours | No | Operating hours |
| charging_when_closed | boolean | No | Charging when closed |
| images | Image[] | No | Photos |
| energy_mix | EnergyMix | No | Energy source mix |
| last_updated | DateTime | Yes | Last modification timestamp |

**Changes from 2.1.1:** Removed `type` (LocationType). Added `country_code`, `party_id`, `publish`, `publish_allowed_to`, `parking_type`, `state`. Made `postal_code` optional, `time_zone` required. IDs use CiString.

---

## EVSE Object

### Version 2.0

| Field | Type | Required | Description |
|---|---|---|---|
| uid | string(15) | Yes | Unique ID within CPO (not the EVSE ID printed on the unit) |
| evse_id | string(48) | No | Compliant EVSE ID (DIN/ISO) |
| status | Status | Yes | Current status |
| status_schedule | StatusSchedule[] | No | Planned status changes |
| capabilities | Capability[] | No | Functional capabilities |
| connectors | Connector[] | Yes (1+) | Available connectors |
| floor_level | string(4) | No | Floor/level (e.g., "-2", "3") |
| coordinates | GeoLocation | No | Exact EVSE location if different from Location |
| physical_reference | string(16) | No | Number/ID visible on the unit |
| directions | DisplayText[] | No | Directions to the EVSE |
| parking_restrictions | ParkingRestriction[] | No | Parking restrictions |
| images | Image[] | No | Photos of the EVSE |

### Version 2.1.1

Same as 2.0 with: `uid` expanded to string(39), **`last_updated` (DateTime, required) added**.

### Version 2.2 / 2.2.1

Same as 2.1.1 with: `uid` uses CiString(36), `evse_id` uses CiString(48). Removed `status_schedule` and `directions` from EVSE (moved to Location level or removed). `images` is retained on the EVSE object.

---

## Connector Object

### Version 2.0

| Field | Type | Required | Description |
|---|---|---|---|
| id | string(15) | Yes | Unique ID within EVSE |
| status | Status | Yes | Current status |
| standard | ConnectorType | Yes | Connector standard |
| format | ConnectorFormat | Yes | SOCKET or CABLE |
| power_type | PowerType | Yes | AC_1_PHASE, AC_3_PHASE, DC |
| voltage | int | Yes | Voltage |
| amperage | int | Yes | Amperage |
| tariff_id | string(15) | No | Applicable tariff ID |
| terms_and_conditions | URL | No | T&C URL |

### Version 2.1.1

| Field | Type | Required | Description |
|---|---|---|---|
| id | string(36) | Yes | Unique ID within EVSE |
| standard | ConnectorType | Yes | Connector standard |
| format | ConnectorFormat | Yes | SOCKET or CABLE |
| power_type | PowerType | Yes | AC_1_PHASE, AC_3_PHASE, DC |
| voltage | int | Yes | Voltage |
| amperage | int | Yes | Amperage |
| tariff_id | string(36) | No | Applicable tariff ID |
| terms_and_conditions | URL | No | T&C URL |
| **last_updated** | DateTime | Yes | **Added** |

**Removed `status` from Connector** (status stays on EVSE only).

### Version 2.2 / 2.2.1

| Field | Type | Required | Description |
|---|---|---|---|
| id | CiString(36) | Yes | Unique ID within EVSE |
| standard | ConnectorType | Yes | Connector standard |
| format | ConnectorFormat | Yes | SOCKET or CABLE |
| power_type | PowerType | Yes | AC_1_PHASE, AC_3_PHASE, DC |
| **max_voltage** | int | Yes | **Renamed from `voltage`** |
| **max_amperage** | int | Yes | **Renamed from `amperage`** |
| **max_electric_power** | int | No | **Added: Max power in Watts** |
| **tariff_ids** | CiString(36)[] | No | **Changed from singular `tariff_id` to array** |
| terms_and_conditions | URL | No | T&C URL |
| last_updated | DateTime | Yes | Last modification |

---

## Enums

### ConnectorType

| Value | 2.0 | 2.1.1 | 2.2 | 2.2.1 |
|---|:---:|:---:|:---:|:---:|
| CHADEMO | Y | Y | Y | Y |
| CHAOJI | - | - | - | Y |
| DOMESTIC_A through DOMESTIC_L | Y | Y | Y | Y |
| DOMESTIC_M, DOMESTIC_N, DOMESTIC_O | - | - | - | Y |
| GBT_AC, GBT_DC | - | - | - | Y |
| IEC_60309_2_single_16 | Y | Y | Y | Y |
| IEC_60309_2_three_16 | Y | Y | Y | Y |
| IEC_60309_2_three_32 | Y | Y | Y | Y |
| IEC_60309_2_three_64 | Y | Y | Y | Y |
| IEC_62196_T1 | Y | Y | Y | Y |
| IEC_62196_T1_COMBO | Y | Y | Y | Y |
| IEC_62196_T2 | Y | Y | Y | Y |
| IEC_62196_T2_COMBO | Y | Y | Y | Y |
| IEC_62196_T3A | Y | Y | Y | Y |
| IEC_62196_T3C | Y | Y | Y | Y |
| NEMA_5_20, NEMA_6_30, NEMA_6_50 | - | - | - | Y |
| NEMA_10_30, NEMA_10_50 | - | - | - | Y |
| NEMA_14_30, NEMA_14_50 | - | - | - | Y |
| PANTOGRAPH_BOTTOM_UP | - | - | Y | Y |
| PANTOGRAPH_TOP_DOWN | - | - | Y | Y |
| TESLA_R | Y | Y | Y | Y |
| TESLA_S | Y | Y | Y | Y |

### ConnectorFormat (all versions)

`SOCKET`, `CABLE`

### PowerType

| Value | 2.0 | 2.1.1 | 2.2 | 2.2.1 |
|---|:---:|:---:|:---:|:---:|
| AC_1_PHASE | Y | Y | Y | Y |
| AC_2_PHASE | - | - | - | Y |
| AC_2_PHASE_SPLIT | - | - | - | Y |
| AC_3_PHASE | Y | Y | Y | Y |
| DC | Y | Y | Y | Y |

### Status

| Value | 2.0 | 2.1.1 | 2.2 | 2.2.1 |
|---|:---:|:---:|:---:|:---:|
| AVAILABLE | Y | Y | Y | Y |
| BLOCKED | Y | Y | Y | Y |
| CHARGING | Y | Y | Y | Y |
| INOPERATIVE | Y | Y | Y | Y |
| OUTOFORDER | Y | Y | Y | Y |
| PLANNED | - | Y | Y | Y |
| REMOVED | - | Y | Y | Y |
| RESERVED | Y | Y | Y | Y |
| UNKNOWN | Y | Y | Y | Y |

> `PLANNED` and `REMOVED` were added in 2.1.1.

### Capability

| Value | 2.0 | 2.1.1 | 2.2 | 2.2.1 |
|---|:---:|:---:|:---:|:---:|
| CHARGING_PROFILE_CAPABLE | Y | Y | Y | Y |
| CHARGING_PREFERENCES_CAPABLE | - | - | Y | Y |
| CHIP_CARD_SUPPORT | - | - | Y | Y |
| CONTACTLESS_CARD_SUPPORT | - | - | Y | Y |
| CREDIT_CARD_PAYABLE | Y | Y | Y | Y |
| DEBIT_CARD_PAYABLE | - | - | Y | Y |
| PED_TERMINAL | - | - | Y | Y |
| REMOTE_START_STOP_CAPABLE | - | Y | Y | Y |
| RESERVABLE | Y | Y | Y | Y |
| RFID_READER | Y | Y | Y | Y |
| START_SESSION_CONNECTOR_REQUIRED | - | - | - | Y |
| TOKEN_GROUP_CAPABLE | - | - | Y | Y |
| UNLOCK_CAPABLE | - | Y | Y | Y |

### ParkingRestriction

| Value | 2.0 | 2.1.1 | 2.2 | 2.2.1 |
|---|:---:|:---:|:---:|:---:|
| CUSTOMERS | Y | Y | Y | Y |
| DISABLED | Y | Y | Y | Y |
| EMPLOYEES | - | - | - | Y |
| EV_ONLY | Y | Y | Y | Y |
| MOTORCYCLES | Y | Y | Y | Y |
| PLUGGED | Y | Y | Y | Y |
| TAXIS | - | - | - | Y |
| TENANTS | - | - | - | Y |

### LocationType (2.0 and 2.1.1 only)

`ON_STREET`, `PARKING_GARAGE`, `UNDERGROUND_GARAGE`, `PARKING_LOT`, `OTHER`, `UNKNOWN`

Replaced by `ParkingType` in 2.2+.

### ParkingType (2.2+ only)

`ALONG_MOTORWAY`, `PARKING_GARAGE`, `PARKING_LOT`, `ON_DRIVEWAY`, `ON_STREET`, `UNDERGROUND_GARAGE`

### Facility (2.1.1+, not in 2.0)

`HOTEL`, `RESTAURANT`, `CAFE`, `MALL`, `SUPERMARKET`, `SPORT`, `RECREATION_AREA`, `NATURE`, `MUSEUM`, `BIKE_SHARING` (2.2+), `BUS_STOP`, `TAXI_STAND`, `TRAM_STOP` (2.2+), `METRO_STATION` (2.2+), `TRAIN_STATION`, `AIRPORT`, `PARKING_LOT` (2.2+), `CARPOOL_PARKING`, `FUEL_STATION`, `WIFI`

### ImageCategory (all versions)

`CHARGER`, `ENTRANCE`, `LOCATION`, `NETWORK`, `OPERATOR`, `OTHER`, `OWNER`

---

## Consumer Interface

```csharp
public interface ILocationsReceiver
{
    Task<OcpiResult> OnLocationPutAsync(
        OcpiRequestContext context,
        string locationId,
        object data,  // V2_0.Location | V2_1_1.Location | V2_2.Location | V2_2_1.Location
        CancellationToken ct);

    Task<OcpiResult> OnLocationPatchAsync(
        OcpiRequestContext context,
        string locationId,
        JsonElement patch,
        CancellationToken ct);

    Task<OcpiResult> OnEvsePutAsync(
        OcpiRequestContext context,
        string locationId,
        string evseUid,
        object data,  // version-specific EVSE model
        CancellationToken ct);

    Task<OcpiResult> OnEvsePatchAsync(
        OcpiRequestContext context,
        string locationId,
        string evseUid,
        JsonElement patch,
        CancellationToken ct);

    Task<OcpiResult> OnConnectorPutAsync(
        OcpiRequestContext context,
        string locationId,
        string evseUid,
        string connectorId,
        object data,  // version-specific Connector model
        CancellationToken ct);

    Task<OcpiResult> OnConnectorPatchAsync(
        OcpiRequestContext context,
        string locationId,
        string evseUid,
        string connectorId,
        JsonElement patch,
        CancellationToken ct);

    Task<OcpiResult<object>> GetLocationAsync(
        OcpiRequestContext context,
        string locationId,
        CancellationToken ct);
}
```

## Validation Rules

- `id` must not be empty or exceed max length per version
- `coordinates` latitude must be between -90 and 90, longitude between -180 and 180
- `country` must be a valid ISO 3166-1 alpha-3 code
- `country_code` (2.2+) must be a valid ISO 3166-1 alpha-2 code
- `party_id` (2.2+) must be 3 characters
- `time_zone` (2.2+ required) must be a valid IANA timezone identifier
- `evses` connectors array must have at least 1 entry
- `publish` (2.2+) must be present
- PATCH requests must contain at least one field besides identifiers
- URL path `country_code`/`party_id` must match body values (2.2+)
