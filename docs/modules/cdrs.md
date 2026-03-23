# CDRs (Charge Detail Records) Module

CDRs are the billing records for completed charging sessions. As an eMSP, DotOcpi acts as a **Receiver** — it receives CDRs from CPOs via POST (new CDR) and can pull via GET.

## eMSP Role Summary

| Operation | Direction | Description |
|---|---|---|
| **Receive POST** | CPO → eMSP | CPO posts a new CDR (not PUT — CDRs are immutable) |
| **Receive GET** | CPO → eMSP | CPO verifies a posted CDR exists |
| **Client GET (list)** | eMSP → CPO | eMSP pulls all CDRs (paginated) |

> CDRs use POST (not PUT) because they are created once and are immutable. The response includes a `Location` header with the URL where the CDR can be retrieved.

## Endpoint URL Patterns

### eMSP Receiver Endpoints (server-side)

| Version | Method | URL Pattern |
|---|---|---|
| All | POST | `{base}/cdrs` (returns Location header with CDR URL) |
| All | GET | `{cdr_url_from_location_header}` (eMSP-defined URL structure) |

### CPO Sender Endpoints (client-side pull)

| Version | URL Pattern |
|---|---|
| All | `{cpo_cdrs_url}?[date_from=][&date_to=][&offset=][&limit=]` |

---

## CDR Object

### Version 2.0

| Field | Type | Required | Description |
|---|---|---|---|
| id | string(39) | Yes | Unique CDR ID |
| start_date_time | DateTime | Yes | Session start |
| end_date_time | DateTime | Yes | Session end |
| auth_id | string(36) | Yes | Token auth ID |
| auth_method | AuthMethod | Yes | Authorization method |
| location | Location | Yes | Full Location object (embedded) |
| meter_id | string(255) | No | Meter identification |
| currency | string(3) | Yes | ISO 4217 currency code |
| tariffs | Tariff[] | No | Applied tariffs |
| charging_periods | ChargingPeriod[] | Yes (1+) | Period breakdown |
| total_cost | decimal | Yes | Total cost |
| total_energy | decimal | Yes | Total energy in kWh |
| total_time | decimal | Yes | Total time in hours |
| total_parking_time | decimal | No | Total parking time in hours |
| remark | string(255) | No | Additional info |

### Version 2.1.1

| Field | Type | Required | Description |
|---|---|---|---|
| id | string(36) | Yes | Unique CDR ID |
| start_date_time | DateTime | Yes | Session start |
| stop_date_time | DateTime | Yes | Session end |
| auth_id | string(36) | Yes | Token auth ID |
| auth_method | AuthMethod | Yes | Authorization method |
| location | Location | Yes | Full Location object |
| meter_id | string(255) | No | Meter identification |
| currency | string(3) | Yes | ISO 4217 currency code |
| tariffs | Tariff[] | No | Applied tariffs |
| charging_periods | ChargingPeriod[] | Yes (1+) | Period breakdown |
| total_cost | number | Yes | Total cost |
| total_energy | number | Yes | Total energy in kWh |
| total_time | number | Yes | Total time in hours |
| total_parking_time | number | No | Total parking time in hours |
| remark | string(255) | No | Additional info |
| **last_updated** | DateTime | Yes | **Added** |

ID changed from string(39) to string(36). Added `last_updated`.

### Version 2.2 / 2.2.1

| Field | Type | Required | Description |
|---|---|---|---|
| **country_code** | CiString(2) | Yes | **Added: CPO's country code** |
| **party_id** | CiString(3) | Yes | **Added: CPO's party ID** |
| id | CiString(39) | Yes | Unique CDR ID (expanded to 39) |
| start_date_time | DateTime | Yes | Session start |
| **end_date_time** | DateTime | Yes | **Renamed from `stop_date_time`** |
| **session_id** | CiString(36) | No | **Added: Reference to Session** |
| **cdr_token** | CdrToken | Yes | **Replaces `auth_id`** |
| auth_method | AuthMethod | Yes | Authorization method |
| **authorization_reference** | CiString(36) | No | **Added: Links to authorize response** |
| **cdr_location** | CdrLocation | Yes | **Replaces full `Location` with simplified CdrLocation** |
| meter_id | string(255) | No | Meter identification |
| currency | string(3) | Yes | ISO 4217 currency code |
| tariffs | Tariff[] | No | Applied tariffs |
| charging_periods | ChargingPeriod[] | Yes (1+) | Period breakdown |
| **signed_data** | SignedData | No | **Added: Calibration law signed data** |
| total_cost | **Price** | Yes | **Type changed to Price object** |
| **total_fixed_cost** | Price | No | **Added: Fixed cost component** |
| total_energy | number | Yes | Total energy in kWh |
| **total_energy_cost** | Price | No | **Added: Energy cost component** |
| total_time | number | Yes | Total time in hours |
| **total_time_cost** | Price | No | **Added: Time cost component** |
| total_parking_time | number | No | Total parking time |
| **total_parking_cost** | Price | No | **Added: Parking cost component** |
| **total_reservation_cost** | Price | No | **Added: Reservation cost component** |
| remark | string(255) | No | Additional info |
| **invoice_reference_id** | CiString(39) | No | **Added: Invoice reference** |
| **credit** | boolean | No | **Added: Whether this is a credit CDR** |
| **credit_reference_id** | CiString(39) | No | **Added: Reference to credited CDR** |
| **home_charging_compensation** | boolean | No | **Added (2.2.1 only)** |
| last_updated | DateTime | Yes | Last modification |

---

## Related Types

### CdrToken (2.2+ only)

See [sessions.md — CdrToken](sessions.md#cdrtoken-22-only) for the full definition. Key difference: `country_code` and `party_id` were NOT present in CdrToken in OCPI 2.2 — they were added in 2.2.1.

### CdrLocation (2.2+ only)

Simplified Location snapshot stored in the CDR. Unlike a full Location reference, this captures the state at the time of charging.

| Field | Type | Required | Description |
|---|---|---|---|
| id | CiString(36) | Yes | Location ID |
| name | string(255) | No | Location name |
| address | string(45) | Yes | Street address |
| city | string(45) | Yes | City |
| postal_code | string(10) | Yes (2.2) / No (2.2.1) | Postal code |
| state | string(20) | No (2.2.1 only) | State/province |
| country | string(3) | Yes | Country (ISO 3166-1 alpha-3) |
| coordinates | GeoLocation | Yes | Latitude/longitude |
| evse_uid | CiString(36) | Yes | EVSE UID used |
| evse_id | CiString(48) | Yes | EVSE ID (printed on unit) |
| connector_id | CiString(36) | Yes | Connector used |
| connector_standard | ConnectorType | Yes | Connector type |
| connector_format | ConnectorFormat | Yes | SOCKET or CABLE |
| connector_power_type | PowerType | Yes | Power type |

### SignedData (2.2+ only)

| Field | Type | Required | Description |
|---|---|---|---|
| encoding_method | CiString(36) | Yes | Encoding method identifier |
| encoding_method_version | int | No | Version of encoding method |
| public_key | string(512) | No | Public key for verification |
| signed_values | SignedValue[] | Yes (1+) | Array of signed meter values |
| url | CiString(512) | No | URL for verification |

### SignedValue (2.2+ only)

| Field | Type | Required | Description |
|---|---|---|---|
| nature | CiString(32) | Yes | Nature of value (e.g., "START", "END") |
| plain_data | string(512) | Yes | Plain text value |
| signed_data | string(5000) | Yes | Signed/encrypted value |

### Price (2.2+ only)

See [common-types.md — Price](common-types.md#price-22-only) for definition.

### ChargingPeriod (all versions)

| Field | Type | Required | Description |
|---|---|---|---|
| start_date_time | DateTime | Yes | Period start time |
| dimensions | CdrDimension[] | Yes (1+) | Measured dimensions |
| tariff_id | CiString(36) | No (2.2+ only) | Applied tariff ID |

### CdrDimension (all versions)

| Field | Type | Required |
|---|---|---|
| type | CdrDimensionType | Yes |
| volume | number | Yes |

### AuthMethod

| Value | 2.0 | 2.1.1 | 2.2 | 2.2.1 |
|---|:---:|:---:|:---:|:---:|
| AUTH_REQUEST | Y | Y | Y | Y |
| COMMAND | - | - | Y | Y |
| WHITELIST | Y | Y | Y | Y |

### CdrDimensionType

| Value | 2.0 | 2.1.1 | 2.2 | 2.2.1 |
|---|:---:|:---:|:---:|:---:|
| CURRENT | - | - | Y | Y |
| ENERGY | Y | Y | Y | Y |
| ENERGY_EXPORT | - | - | Y | Y |
| ENERGY_IMPORT | - | - | Y | Y |
| FLAT | Y | Y | - | - |
| MAX_CURRENT | Y | Y | Y | Y |
| MIN_CURRENT | Y | Y | Y | Y |
| MAX_POWER | - | - | Y | Y |
| MIN_POWER | - | - | Y | Y |
| PARKING_TIME | Y | Y | Y | Y |
| POWER | - | - | Y | Y |
| RESERVATION_TIME | - | - | Y | Y |
| STATE_OF_CHARGE | - | - | Y | Y |
| TIME | Y | Y | Y | Y |

> `FLAT` was removed in 2.2.

---

## Consumer Interface

```csharp
public interface ICdrsReceiver
{
    /// <summary>
    /// Called when a CPO posts a new CDR.
    /// Returns the CDR ID and whether it was newly created (for idempotency).
    /// </summary>
    Task<OcpiResult<CdrPostResult>> OnCdrPostAsync(
        OcpiRequestContext context,
        object data,
        CancellationToken ct);

    /// <summary>
    /// Retrieves a CDR by ID.
    /// </summary>
    Task<OcpiResult<object>> GetCdrAsync(
        OcpiRequestContext context,
        string cdrId,
        CancellationToken ct);
}
```

## Validation Rules

- `id` must not be empty
- `start_date_time` must be valid
- `end_date_time` / `stop_date_time` must be after `start_date_time`
- `charging_periods` must have at least 1 entry, each with at least 1 dimension
- `total_cost` must be >= 0
- `total_energy` must be >= 0
- `total_time` must be >= 0
- `currency` must be valid ISO 4217
- `cdr_token` (2.2+) must have valid fields
- `cdr_location` (2.2+) must have valid coordinates
- If `credit` is true, `credit_reference_id` should be present (2.2+)
