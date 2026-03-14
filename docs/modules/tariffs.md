# Tariffs Module

The Tariffs module allows CPOs to share pricing information with eMSPs. As an eMSP, DotOcpi acts as a **Receiver** — it receives Tariff data via push and can pull via GET.

## eMSP Role Summary

| Operation | Direction | Description |
|---|---|---|
| **Receive PUT** | CPO → eMSP | CPO pushes a full Tariff object |
| **Receive PATCH** | CPO → eMSP | CPO sends a partial update (2.0/2.1.1 only — removed in 2.2+) |
| **Receive DELETE** | CPO → eMSP | CPO deletes a Tariff |
| **Receive GET** | CPO → eMSP | CPO retrieves a previously pushed Tariff |
| **Client GET (list)** | eMSP → CPO | eMSP pulls all tariffs (paginated) |

> PATCH was removed from the tariff receiver interface in 2.2+. Tariffs are replaced in full via PUT.

## Endpoint URL Patterns

### eMSP Receiver Endpoints (server-side)

| Version | URL Pattern |
|---|---|
| 2.0 / 2.1.1 | `{base}/tariffs/{tariff_id}` |
| 2.2 / 2.2.1 | `{base}/tariffs/{country_code}/{party_id}/{tariff_id}` |

### CPO Sender Endpoints (client-side pull)

| Version | URL Pattern |
|---|---|
| All | `{cpo_tariffs_url}?[date_from=][&date_to=][&offset=][&limit=]` |

---

## Tariff Object

### Version 2.0

| Field | Type | Required | Description |
|---|---|---|---|
| id | string(15) | Yes | Unique tariff ID |
| currency | string(3) | Yes | ISO 4217 currency code |
| tariff_alt_text | DisplayText[] | No | Human-readable tariff description |
| tariff_alt_url | URL | No | URL with tariff information |
| elements | TariffElement[] | Yes (1+) | Tariff elements with pricing |

### Version 2.1.1

| Field | Type | Required | Description |
|---|---|---|---|
| id | string(36) | Yes | Unique tariff ID |
| currency | string(3) | Yes | ISO 4217 currency code |
| tariff_alt_text | DisplayText[] | No | Human-readable description |
| tariff_alt_url | URL | No | URL with tariff info |
| elements | TariffElement[] | Yes (1+) | Tariff elements |
| **energy_mix** | EnergyMix | No | **Added: Energy source mix** |
| **last_updated** | DateTime | Yes | **Added** |

### Version 2.2 / 2.2.1

| Field | Type | Required | Description |
|---|---|---|---|
| **country_code** | CiString(2) | Yes | **Added: CPO's country code** |
| **party_id** | CiString(3) | Yes | **Added: CPO's party ID** |
| id | CiString(36) | Yes | Unique tariff ID |
| currency | string(3) | Yes | ISO 4217 currency code |
| **type** | TariffType | No | **Added: Tariff type classification** |
| tariff_alt_text | DisplayText[] | No | Human-readable description |
| tariff_alt_url | URL | No | URL with tariff info |
| **min_price** | Price | No | **Added: Minimum price per session** |
| **max_price** | Price | No | **Added: Maximum price per session** |
| elements | TariffElement[] | Yes (1+) | Tariff elements |
| **start_date_time** | DateTime | No | **Added: Tariff validity start** |
| **end_date_time** | DateTime | No | **Added: Tariff validity end** |
| energy_mix | EnergyMix | No | Energy source mix |
| last_updated | DateTime | Yes | Last modification |

---

## Related Types

### TariffElement (all versions)

| Field | Type | Required | Description |
|---|---|---|---|
| price_components | PriceComponent[] | Yes (1+) | Price components |
| restrictions | TariffRestrictions | No | When this element applies |

### PriceComponent

| Field | Type | Required | Description |
|---|---|---|---|
| type | TariffDimensionType | Yes | ENERGY, FLAT, PARKING_TIME, TIME |
| price | number | Yes | Price per unit |
| **vat** | number | No (2.2+ only) | **Added: VAT percentage** |
| step_size | int | Yes | Minimum step size for billing |

### TariffRestrictions

| Field | Type | 2.0/2.1.1 | 2.2/2.2.1 | Description |
|---|---|:---:|:---:|---|
| start_time | string(5) | No | No | HH:MM start of applicable time |
| end_time | string(5) | No | No | HH:MM end of applicable time |
| start_date | string(10) | No | No | YYYY-MM-DD start date |
| end_date | string(10) | No | No | YYYY-MM-DD end date |
| min_kwh | number | No | No | Minimum energy in kWh |
| max_kwh | number | No | No | Maximum energy in kWh |
| min_current | number | - | No | **Added in 2.2**: Min current in A |
| max_current | number | - | No | **Added in 2.2**: Max current in A |
| min_power | number | No | No | Min power in kW |
| max_power | number | No | No | Max power in kW |
| min_duration | int | No | No | Min duration in seconds |
| max_duration | int | No | No | Max duration in seconds |
| day_of_week | DayOfWeek[] | No | No | Applicable days |
| reservation | ReservationRestrictionType | - | No | **Added in 2.2** |

### Enums

#### TariffDimensionType (all versions)

`ENERGY`, `FLAT`, `PARKING_TIME`, `TIME`

#### TariffType (2.2+ only)

`AD_HOC_PAYMENT`, `PROFILE_CHEAP`, `PROFILE_FAST`, `PROFILE_GREEN`, `REGULAR`

#### DayOfWeek (all versions)

`MONDAY`, `TUESDAY`, `WEDNESDAY`, `THURSDAY`, `FRIDAY`, `SATURDAY`, `SUNDAY`

#### ReservationRestrictionType (2.2+ only)

`RESERVATION`, `RESERVATION_EXPIRES`

---

## Consumer Interface

```csharp
public interface ITariffsReceiver
{
    Task<OcpiResult> OnTariffPutAsync(
        OcpiRequestContext context,
        string tariffId,
        object tariff,  // V2_0.Tariff | V2_1_1.Tariff | V2_2.Tariff | V2_2_1.Tariff
        CancellationToken ct);

    Task<OcpiResult> OnTariffPatchAsync(
        OcpiRequestContext context,
        string tariffId,
        JsonElement patch,
        CancellationToken ct);
    // Note: PATCH only called for 2.0/2.1.1 connections.
    // The library will reject PATCH for 2.2+ with 405.

    Task<OcpiResult> OnTariffDeleteAsync(
        OcpiRequestContext context,
        string tariffId,
        CancellationToken ct);

    Task<OcpiResult<object>> OnTariffGetAsync(
        OcpiRequestContext context,
        string tariffId,
        CancellationToken ct);
}
```

## Validation Rules

- `id` must not be empty
- `currency` must be valid ISO 4217
- `elements` must have at least 1 TariffElement
- Each TariffElement must have at least 1 PriceComponent
- `step_size` must be > 0
- `price` must be >= 0
- `vat` (2.2+) if present, must be >= 0
- `start_date_time` must be before `end_date_time` if both present
- `min_price.excl_vat` must be >= 0
- `max_price.excl_vat` must be >= `min_price.excl_vat` if both present
