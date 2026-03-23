# Credentials Module

The Credentials module handles registration, authentication, and connection management between eMSP and CPO. This is the only module where the eMSP acts as both **client and server** in the same flow.

For the full handshake sequence diagrams, see [architecture.md — Credentials & Registration](../architecture.md#6-credentials--registration).

## Endpoint URL Patterns

### All Versions

| Method | URL Pattern | Description |
|---|---|---|
| GET | `{base}/credentials` | Retrieve other party's credentials |
| POST | `{base}/credentials` | Initial registration |
| PUT | `{base}/credentials` | Update credentials / rotate tokens |
| DELETE | `{base}/credentials` | Unregister |

### HTTP Status Semantics

| Method | Already Registered | Not Registered |
|---|---|---|
| POST | 405 Method Not Allowed | Performs registration |
| PUT | Updates credentials | 405 Method Not Allowed |
| DELETE | Unregisters | 405 Method Not Allowed |

---

## Credentials Object

### Version 2.0 (flat structure)

| Field | Type | Required | Description |
|---|---|---|---|
| token | string(64) | Yes | Token for the receiving party to use |
| url | URL | Yes | Sender's versions endpoint URL |
| business_name | string(100) | Yes | Company name (flat field, not nested BusinessDetails) |
| party_id | string(3) | Yes | Sender's party ID |
| country_code | string(2) | Yes | Sender's country code |
| website | URL | No | Company website |

> In OCPI 2.0, business information uses flat fields (`business_name`, `website`) rather than a nested `BusinessDetails` object. There is no `business_logo` field.

### Version 2.1.1 (flat structure)

| Field | Type | Required | Description |
|---|---|---|---|
| token | string(64) | Yes | Token for the receiving party to use |
| url | URL | Yes | Sender's versions endpoint URL |
| business_name | string(100) | Yes | Company name (flat field, not nested BusinessDetails) |
| party_id | string(3) | Yes | Sender's party ID |
| country_code | string(2) | Yes | Sender's country code |
| business_logo | Image | No | Company logo (**added in 2.1.1**) |
| website | URL | No | Company website |

Single role per connection. The `party_id`, `country_code`, and business fields are top-level (flat), not nested inside a `BusinessDetails` or `CredentialsRole` object.

### Version 2.2 / 2.2.1 (roles array)

| Field | Type | Required | Description |
|---|---|---|---|
| token | string(64) | Yes | Token for the receiving party to use |
| url | URL | Yes | Sender's versions endpoint URL |
| roles | CredentialsRole[] | Yes (1+) | Sender's roles and identities |

Supports multiple roles per connection (e.g., a platform that is both CPO and eMSP).

### CredentialsRole (2.2+ only)

| Field | Type | Required | Description |
|---|---|---|---|
| role | Role | Yes | CPO, EMSP, HUB, NAP, NSP, OTHER, SCSP |
| business_details | BusinessDetails | Yes | Company information |
| party_id | CiString(3) | Yes | Party ID for this role |
| country_code | CiString(2) | Yes | Country code for this role |

### BusinessDetails (all versions)

| Field | Type | Required | Description |
|---|---|---|---|
| name | string(100) | Yes | Company name |
| website | URL | No | Company website |
| logo | Image | No | Company logo (2.1.1+; not present in 2.0) |

---

## Version Information Module

### Versions Endpoint: `GET /ocpi/versions`

Returns a list of supported OCPI versions:

```json
{
    "data": [
        {"version": "2.1.1", "url": "https://example.com/ocpi/2.1.1"},
        {"version": "2.2.1", "url": "https://example.com/ocpi/2.2.1"}
    ],
    "status_code": 1000,
    "timestamp": "2026-03-13T12:00:00Z"
}
```

### Version Detail Endpoint: `GET /ocpi/versions/{version_id}/`

Returns module endpoints for a specific version.

#### All Versions

| Field | Type | Required | Description |
|---|---|---|---|
| version | VersionNumber | Yes | Version identifier |
| endpoints | Endpoint[] | Yes (1+) | Module endpoints |

**Endpoint (all versions):**

| Field | Type | Required | Description |
|---|---|---|---|
| identifier | ModuleID | Yes | Module name |
| role | InterfaceRole | Yes | SENDER or RECEIVER |
| url | URL | Yes | Endpoint URL |

> The `role` field is present in all versions, enabling separate SENDER/RECEIVER endpoints for the same module.

### InterfaceRole (all versions)

| Value | Description |
|---|---|
| SENDER | Owner of data; provides GET (pull) interface |
| RECEIVER | Recipient of data; provides PUT/PATCH/POST (push) interface |

### ModuleID

| Module | 2.0 | 2.1.1 | 2.2 | 2.2.1 |
|---|:---:|:---:|:---:|:---:|
| cdrs | Y | Y | Y | Y |
| chargingprofiles | - | - | Y | Y |
| commands | Y | Y | Y | Y |
| credentials | Y | Y | Y | Y |
| hubclientinfo | - | - | Y | Y |
| locations | Y | Y | Y | Y |
| sessions | Y | Y | Y | Y |
| tariffs | Y | Y | Y | Y |
| tokens | Y | Y | Y | Y |

### VersionNumber

`2.0`, `2.1` (deprecated), `2.1.1`, `2.2` (deprecated), `2.2.1`

---

## Registration Flow

See [architecture.md — Credentials & Registration](../architecture.md#6-credentials--registration) for the full sequence diagram.

### Summary

1. **Out-of-band**: Receiver creates Token A, shares it + versions URL with Sender
2. **Version discovery**: Sender calls `GET /versions` → `GET /versions/{id}/` using Token A
3. **Registration**: Sender `POST /credentials` with Token A in header, Token B in body
4. **Validation**: Receiver calls Sender's versions endpoint using Token B
5. **Response**: Receiver responds with Token C; Token A is discarded
6. **Active**: Token B authenticates CPO→eMSP requests; Token C authenticates eMSP→CPO requests

### Token Rotation

Same flow as registration but uses `PUT /credentials` instead of `POST`. Old tokens are replaced atomically.

---

## Consumer Interface

The Credentials module does **not** expose a consumer interface. The library handles credentials exchange internally via `RegistrationOrchestrator` (see [implementation-guide.md — Phase 8](../implementation-guide.md#phase-8-version-negotiation--registration)). Consumers initiate registration through `IRegistrationClient.RegisterAsync()` and do not need to implement any credentials-specific interface.

---

## Validation Rules

- `token` must not be empty, max 64 characters
- `url` must be a valid HTTPS URL
- `party_id` must be exactly 3 characters
- `country_code` must be exactly 2 characters (ISO 3166-1 alpha-2)
- `roles` (2.2+) must have at least 1 entry
- Each role must include valid `business_details` with a non-empty `name`
- POST must be rejected with 405 if already registered
- PUT must be rejected with 405 if not registered
- DELETE must be rejected with 405 if not registered
