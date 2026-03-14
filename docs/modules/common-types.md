# Common Types

Shared types used across multiple OCPI modules.

## DisplayText (all versions)

| Field | Type | Required | Description |
|---|---|---|---|
| language | string(2) | Yes | ISO 639-1 language code |
| text | string(512) | Yes | Human-readable text |

## GeoLocation (all versions)

| Field | Type | Required | Description |
|---|---|---|---|
| latitude | string(10) | Yes | Latitude (-90 to 90, as string) |
| longitude | string(11) | Yes | Longitude (-180 to 180, as string) |

> Coordinates are strings, not numbers, to preserve precision.

## AdditionalGeoLocation (all versions)

| Field | Type | Required | Description |
|---|---|---|---|
| latitude | string(10) | Yes | Latitude |
| longitude | string(11) | Yes | Longitude |
| name | DisplayText | No | Description of the location |

## BusinessDetails (all versions)

| Field | Type | Required | Description |
|---|---|---|---|
| name | string(100) | Yes | Company name |
| website | URL | No | Company website |
| logo | Image | No | Company logo (2.1.1+; not present in 2.0) |

## Image (all versions)

| Field | Type | Required | Description |
|---|---|---|---|
| url | URL | Yes | Image URL |
| thumbnail | URL | No | Thumbnail URL |
| category | ImageCategory | Yes | CHARGER, ENTRANCE, LOCATION, etc. |
| type | CiString(4) | Yes | File type (e.g., "png", "jpeg") |
| width | int | No | Width in pixels |
| height | int | No | Height in pixels |

## Hours (all versions)

| Field | Type | Required | Description |
|---|---|---|---|
| twentyfourseven | boolean | Conditional | Open 24/7. Required if `regular_hours` is not provided |
| regular_hours | RegularHours[] | Conditional | Required if `twentyfourseven` is false |
| exceptional_openings | ExceptionalPeriod[] | No | Special opening periods |
| exceptional_closings | ExceptionalPeriod[] | No | Special closing periods |

## RegularHours (all versions)

| Field | Type | Required | Description |
|---|---|---|---|
| weekday | int(1) | Yes | 1 = Monday through 7 = Sunday |
| period_begin | string(5) | Yes | HH:MM opening time |
| period_end | string(5) | Yes | HH:MM closing time |

## ExceptionalPeriod (all versions)

| Field | Type | Required | Description |
|---|---|---|---|
| period_begin | DateTime | Yes | Start of exceptional period |
| period_end | DateTime | Yes | End of exceptional period |

## Price (2.2+ only)

| Field | Type | Required | Description |
|---|---|---|---|
| excl_vat | number | Yes | Price excluding VAT |
| incl_vat | number | No | Price including VAT |

> In 2.0/2.1.1, costs are simple `number`/`decimal` fields, not `Price` objects.

## EnergyMix (2.1.1+)

| Field | Type | Required | Description |
|---|---|---|---|
| is_green_energy | boolean | Yes | Whether energy is 100% green |
| energy_sources | EnergySource[] | No | Breakdown of energy sources |
| environ_impact | EnvironmentalImpact[] | No | Environmental impact data |
| supplier_name | string(64) | No | Energy supplier name |
| energy_product_name | string(64) | No | Energy product name |

## EnergySource (2.1.1+)

| Field | Type | Required | Description |
|---|---|---|---|
| source | EnergySourceCategory | Yes | Source category |
| percentage | number | Yes | Percentage (0-100) |

### EnergySourceCategory (2.1.1+)

`NUCLEAR`, `GENERAL_FOSSIL`, `COAL`, `GAS`, `GENERAL_GREEN`, `SOLAR`, `WIND`, `WATER`

## EnvironmentalImpact (2.1.1+)

| Field | Type | Required | Description |
|---|---|---|---|
| category | EnvironmentalImpactCategory | Yes | Impact category |
| amount | number | Yes | Amount per MWh |

### EnvironmentalImpactCategory (2.1.1+)

`NUCLEAR_WASTE`, `CARBON_DIOXIDE`

## StatusSchedule (all versions)

| Field | Type | Required | Description |
|---|---|---|---|
| period_begin | DateTime | Yes | Schedule start |
| period_end | DateTime | No | Schedule end |
| status | Status | Yes | Planned status |

## PublishTokenType (2.2+ only)

Controls which tokens can see a Location when `publish` is `false`.

| Field | Type | Required | Description |
|---|---|---|---|
| uid | CiString(36) | No | Token UID |
| type | TokenType | Conditional | Required when `uid` is set |
| visual_number | string(64) | No | Visual number on token |
| issuer | string(64) | No | Token issuer |
| group_id | CiString(36) | No | Token group |

> At least one of `uid`, `visual_number`, or `group_id` must be set.

## OCPI Response Envelope (all versions)

| Field | Type | Required | Description |
|---|---|---|---|
| data | Object/Array | Varies | Response payload |
| status_code | int | Yes | OCPI status code |
| status_message | string | No | Human-readable debug message |
| timestamp | DateTime | Yes | Response generation time |

## OCPI Status Codes

| Code | Category | Description |
|---|---|---|
| **1000** | Success | Generic success |
| **2000** | Client Error | Generic client error |
| **2001** | Client Error | Invalid or missing parameters |
| **2002** | Client Error | Not enough information |
| **2003** | Client Error | Unknown location |
| **2004** | Client Error | Unknown token |
| **3000** | Server Error | Generic server error |
| **3001** | Server Error | Unable to use client's API |
| **3002** | Server Error | Unsupported version |
| **3003** | Server Error | No matching endpoints |

Ranges x900–x999 in each category are reserved for custom codes.

## Primitive Types

| Type | Description |
|---|---|
| **string** | Case-sensitive, printable UTF-8 (2.2+) or ASCII (2.0/2.1.1) |
| **CiString** | Case-insensitive string. Comparisons ignore case; storage preserves original. Implemented as `readonly record struct CiString` — see [strategies.md — Type Convention Strategy](../strategies.md#15-type-convention-strategy) |
| **number** | JSON number, typically up to 4 decimal places |
| **DateTime** | ISO 8601 / RFC 3339 format, UTC required |
| **URL** | string(255), must be valid per W3C |

### The `#NA` Sentinel

When a required string field genuinely cannot be populated, the literal string `#NA` may be used. The library recognizes this sentinel via `OcpiSentinel.IsNotAvailable()` and accepts it during validation of required string fields. See [strategies.md — Type Convention Strategy](../strategies.md#15-type-convention-strategy).
