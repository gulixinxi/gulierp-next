# TRAE MDM-002 API Handoff

> **Scope**: MDM-002 backend API contract for the TRAE front-end
> team. This document covers BusinessPartner / Warehouse /
> Location endpoints, DTOs, query semantics, role filters,
> cascade rules, and error mapping. It is the **only authoritative
> API surface** for these three entities; the SPA must read this
> document and the actual `MdmEndpoints.cs` source.
>
> **Authoritative source**: `modules/mdm/GuliERP.Mdm.Application/MdmDtos.cs`
> and `apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs` (this document
> mirrors the source and is regenerated from it by hand).
>
> **No URL guessing**: if a property is not listed here, it does
> NOT exist on the wire. If a field IS listed here, the SPA
> MUST consume it. If a field is needed but not listed, the
> backend must add it FIRST, then update this document.

## 1. Base path

All MDM-002 endpoints live under `/api/v1/mdm/`:

| Group | Base path |
|---|---|
| BusinessPartner | `/api/v1/mdm/business-partners` |
| Warehouse | `/api/v1/mdm/warehouses` |
| Location | `/api/v1/mdm/locations` |

All endpoints require:
- An authenticated user (Cookie auth per `apps/web/src/api/http.ts`)
- An `X-CSRF-TOKEN` header on POST / PUT (CSRF contract per DEC-AUTH-009)
- A valid permission policy (see §10)
- A resolved Tenant + Company context (the headers are set by the SPA's
  request layer; the backend's `ICurrentTenant` / `ICurrentCompany`
  reject requests that lack a scope)

## 2. BusinessPartner

### 2.1 List — `GET /api/v1/mdm/business-partners`

Query parameters:
- `keyword` (string, optional) — case-insensitive substring match on
  `code`, `name`, or `shortName` (uppercase compare per MDM-001 pattern).
- `role` (int, optional) — bit-flag. `1` = Customer, `2` = Supplier,
  `3` = Both. Pass `1` to match Customer OR Both; pass `2` to match
  Supplier OR Both; pass `null` for no role filter.
- `status` (int, optional) — `1` = Active, `2` = Inactive.
- `page` (int, default 1) — 1-based.
- `pageSize` (int, default 20, max 200).

Response 200:
```json
{
  "items": [ BusinessPartnerDto, ... ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 42
}
```

### 2.2 GetById — `GET /api/v1/mdm/business-partners/{id}`

- 200 → `BusinessPartnerDto`
- 404 → if the row does not exist OR is in a different Tenant
  (existence is NOT leaked across tenants)

### 2.3 Create — `POST /api/v1/mdm/business-partners`

Request body (request schema mirrors `CreateBusinessPartnerRequest`):
```json
{
  "code": "BP-001",
  "name": "Acme Industries",
  "shortName": "Acme",
  "role": 3,
  "contactPerson": "Alice Wong",
  "phone": "+86 21 0000 0000",
  "email": "alice@acme.example",
  "addressLine1": "1 Acme Plaza",
  "addressLine2": "Suite 200",
  "city": "Shanghai",
  "region": "Shanghai",
  "postalCode": "200000",
  "countryCode": "CN",
  "taxNumber": "9131000077777777XL",
  "description": "Top customer since 2015"
}
```

- `code` is REQUIRED, max 40 chars, auto-uppercased + trimmed.
- `name` is REQUIRED, max 200 chars.
- `shortName` is OPTIONAL, max 40 chars.
- `role` is REQUIRED (1/2/3). If `0` (None) is sent, the response is
  400 `mdm_validation_failed`.
- `email` is OPTIONAL; if present, must contain `@`.
- `countryCode` is OPTIONAL; if present, MUST be a 2-letter ISO
  3166-1 alpha-2 code (e.g. `CN`, `US`).
- All other string fields are free-text with length caps.

Response 201 (Created) → `BusinessPartnerDto` with `Id`, `Status=Active`,
`ConcurrencyVersion=1`, `CreatedAt` / `ModifiedAt` set by server.

Tenant is taken from the request context (`ICurrentTenant.Id`), NEVER
from the request body. The SPA MUST NOT include a `tenantId` field in
the request body — if it does, the field is silently ignored.

### 2.4 Update — `PUT /api/v1/mdm/business-partners/{id}`

Request body mirrors `UpdateBusinessPartnerRequest` (Code is NOT
included — Code is immutable in V1). `expectedConcurrencyVersion` is
REQUIRED. Server compares it to the row's `concurrencyVersion`; mismatch
returns 400 `mdm_validation_failed` with a "reload and retry" message.

Response 200 → `BusinessPartnerDto` with the new `concurrencyVersion`.

## 3. Warehouse

### 3.1 List — `GET /api/v1/mdm/warehouses`

Query parameters:
- `keyword` (string, optional) — case-insensitive substring on `code` or `name`.
- `type` (int, optional) — 1=Physical, 2=Virtual, 3=Return.
- `status` (int, optional) — 1=Active, 2=Inactive.
- `page` / `pageSize` (default 1 / 20, max 200).

Filter is by `(TenantId, CompanyId)` from the current scope.

### 3.2 GetById — `GET /api/v1/mdm/warehouses/{id}`

- 200 → `WarehouseDto`
- 404 → row missing or in a different `(TenantId, CompanyId)`

### 3.3 Create — `POST /api/v1/mdm/warehouses`

Request body (`CreateWarehouseRequest`):
```json
{
  "plantId": null,
  "code": "WH-001",
  "name": "Main Warehouse",
  "type": 1,
  "addressLine1": "...",
  ...
  "countryCode": "CN",
  "description": "..."
}
```

- `plantId` is OPTIONAL (`long?`). V1 does NOT have a Plants table
  in MDM-002 — the Production module owns that in V2+. A
  `plantId` value is stored as a raw `bigint`; the SPA should
  treat it as an opaque reference.
- `code` is REQUIRED, max 40 chars, unique within `(TenantId, CompanyId, Code)`.
- `name` is REQUIRED, max 200 chars.
- `type` is REQUIRED (1/2/3).

### 3.4 Update — `PUT /api/v1/mdm/warehouses/{id}`

Same shape as create, plus `expectedConcurrencyVersion`.

## 4. Location

### 4.1 List — `GET /api/v1/mdm/locations`

Query parameters:
- `keyword` (string, optional) — case-insensitive substring on `code` or `name`.
- `warehouseId` (long, optional) — restrict to a single warehouse.
- `type` (int, optional) — 1=Bin, 2=Shelf, 3=Zone, 4=Dock.
- `status` (int, optional) — 1=Active, 2=Inactive.
- `page` / `pageSize`.

### 4.2 GetById — `GET /api/v1/mdm/locations/{id}`

### 4.3 Create — `POST /api/v1/mdm/locations`

Request body (`CreateLocationRequest`):
```json
{
  "warehouseId": 123,
  "code": "A-1-1",
  "name": "Aisle A - Bay 1 - Shelf 1",
  "type": 1,
  "aisle": "A",
  "bay": "1",
  "shelf": "1",
  "description": "..."
}
```

- `warehouseId` is REQUIRED and must be a Warehouse in the
  SAME `(TenantId, CompanyId)`. If the parent warehouse is
  not visible, the response is **400 with code
  `mdm_location_parent_warehouse_cross_scope`** (NOT 404,
  because the parent IS the auth context — the error is a
  validation failure, not an existence leak).
- `code` is REQUIRED, max 40 chars, unique within `(TenantId, CompanyId, Code)`.
- `type` is REQUIRED (1/2/3/4).

### 4.4 Update — `PUT /api/v1/mdm/locations/{id}`

Same as create, plus `expectedConcurrencyVersion`. If `warehouseId`
differs from the existing row, the new parent must also be in scope.

## 5. Role filter (BusinessPartner only)

The `role` query parameter is a bit-flag. This means a single
counterparty row can serve as Customer, Supplier, or both:

| Counterparty role | Filter `role=1` (Customer) matches? | Filter `role=2` (Supplier) matches? |
|---|---|---|
| `Customer` (1) | ✅ | ❌ |
| `Supplier` (2) | ❌ | ✅ |
| `Both` (3) | ✅ | ✅ |

This is a SQL-level `(role & filter) != 0` predicate — see
`MdmBusinessPartnerService.ListAsync`.

The SPA dropdown for "Customer" should send `role=1`; for
"Supplier" send `role=2`; for "All" send `role` (omit).

## 6. Cascade rules

- **Deactivating a Warehouse**: the SPA must explicitly deactivate
  every Location in that Warehouse first. The backend FK is
  `Restrict` (no cascade delete; no auto-deactivate). However, V1
  does NOT block deactivating a Warehouse that still has Active
  Locations — V2+ will. The SPA should warn the operator.

- **Re-parenting a Location**: `PUT /api/v1/mdm/locations/{id}` with
  a different `warehouseId` IS allowed. The new warehouse must be
  in the same `(TenantId, CompanyId)`. The Location's audit
  `ModifiedAt` / `ModifiedBy` is updated.

- **BusinessPartner Role change**: a `PUT` that changes `role` from
  `Both` to `Customer` is a logical narrowing. The SPA should warn
  the operator if open Purchase Orders reference the row as a
  Supplier (V2+ will block; V1 just logs the change).

## 7. Concurrency

Every DTO carries `concurrencyVersion` (int, server-generated).
The SPA MUST:
- Display the value in edit forms (read-only).
- Send the value back as `expectedConcurrencyVersion` in every PUT.
- On 400 `mdm_validation_failed` mentioning ConcurrencyVersion, force
  a GET and re-display the row (the row was changed by another user).

The server increments `concurrencyVersion` on every successful PUT.

## 8. Cross-tenant / cross-company safety

- A `GET /api/v1/mdm/business-partners/{id}` for an id that exists
  in Tenant B but not in the caller's Tenant A returns 404.
- A `POST` with `code` already in use in the same Tenant returns
  400 `mdm_duplicate_code`.
- A `POST /api/v1/mdm/locations` with a `warehouseId` from a
  different `(TenantId, CompanyId)` returns 400
  `mdm_location_parent_warehouse_cross_scope`.
- A `POST /api/v1/mdm/business-partners` carrying a `tenantId`
  in the body is ignored — the server always uses the request
  context's `ICurrentTenant.Id`. This means a malicious client
  CANNOT smuggle a foreign TenantId.

## 9. Error code map

| `code` (extension on `application/problem+json`) | HTTP status | Meaning |
|---|---|---|
| `mdm_validation_failed` | 400 | Generic business validation (empty name, invalid email, country code, etc.) OR concurrency mismatch |
| `mdm_duplicate_code` | 400 | Code already exists in the same scope |
| `mdm_business_partner_not_found` | 404 | (reserved — not currently emitted; service returns `null` → endpoint returns 404) |
| `mdm_business_partner_cross_tenant` | 404 | (reserved) |
| `mdm_warehouse_not_found` | 404 | (reserved) |
| `mdm_warehouse_cross_scope` | 404 | (reserved) |
| `mdm_location_not_found` | 404 | (reserved) |
| `mdm_location_parent_warehouse_cross_scope` | 400 | Location's parent warehouse not in current `(TenantId, CompanyId)` |

The SPA should render the `detail` field as the user-facing message
and the `code` as a stable identifier (e.g. for analytics / i18n).

## 10. Permission policies

Each endpoint is gated by an ASP.NET Core policy name (prefix
`GuliERP.Permission:mdm.…`):

| Endpoint | Policy |
|---|---|
| `GET /api/v1/mdm/business-partners*` | `mdm.business-partner.read` |
| `POST /api/v1/mdm/business-partners*` | `mdm.business-partner.manage` |
| `PUT /api/v1/mdm/business-partners*` | `mdm.business-partner.manage` |
| `GET /api/v1/mdm/warehouses*` | `mdm.warehouse.read` |
| `POST /api/v1/mdm/warehouses*` | `mdm.warehouse.manage` |
| `PUT /api/v1/mdm/warehouses*` | `mdm.warehouse.manage` |
| `GET /api/v1/mdm/locations*` | `mdm.location.read` |
| `POST /api/v1/mdm/locations*` | `mdm.location.manage` |
| `PUT /api/v1/mdm/locations*` | `mdm.location.manage` |

These are NOT URLs — they are business capabilities. The Permission
Authorization Handler resolves the policy against the user's role
claims. Unauthorized access returns 403 (no RFC7807 body).

## 11. Customer / Supplier Lookup

For dropdowns, use the **list endpoint with a tight `pageSize=20`
and a short `keyword`** (e.g. the first 3 chars of the partner's
name). Do NOT call `GET /{id}` repeatedly from a dropdown — it
will not scale and the network round-trip is wasted.

## 12. Warehouse / Location cascade picker

For "create Location" forms, the SPA should:
1. Call `GET /api/v1/mdm/warehouses?keyword=...&pageSize=20` to
   populate the Warehouse dropdown.
2. Once the user picks a warehouse, optionally call
   `GET /api/v1/mdm/locations?warehouseId={picked}&keyword=...&pageSize=20`
   to show a parent-Location hint (V1 does NOT have a parent-Location
   link on Location, so this is a read-only hint, not a real tree).

## 13. Front-end forbidden computations

The SPA MUST NOT compute, derive, or guess:
- ConcurrencyVersion (use the server's value).
- TenantId (the server resolves it from the request context).
- CompanyId (same).
- Code canonicalization (the server uppercases + trims; the SPA
  should send what the operator types, and the server normalizes).
- CreatedAt / ModifiedAt / CreatedBy / ModifiedBy (server-only).
- "Both" role interpretation (a `role=3` row is exactly that — the
  SPA should not auto-pick a "primary" role).
- Cascade disable when deactivating a Warehouse (V1 does not
  enforce; V2+ will; the SPA should still warn the operator).

If the SPA needs a derived value, ask the backend to add it
explicitly; do not compute it client-side.

## 14. Source files (for the SPA team)

| Path | Purpose |
|---|---|
| `modules/mdm/GuliERP.Mdm.Application/MdmDtos.cs` | All 12 DTOs + 3 list queries + PagedResult |
| `modules/mdm/GuliERP.Mdm.Application/IMdmMasterData002Services.cs` | 3 service interfaces (12 methods) |
| `modules/mdm/GuliERP.Mdm.Application/MdmPermissions.cs` | 6 permission codes |
| `modules/mdm/GuliERP.Mdm.Application/MdmPolicies.cs` | 6 policy names |
| `modules/mdm/GuliERP.Mdm.Application/MdmErrorCodes.cs` | 6 stable error codes |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmMasterData002Services.cs` | 3 service implementations |
| `apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs` | 12 endpoint registrations |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260821104254_MDM002_BusinessPartnerWarehouseLocation.cs` | EF migration |
| `modules/mdm/GuliERP.Mdm.Domain/Enums/MdmEnums.cs` | `BusinessPartnerRole`, `WarehouseType`, `LocationType` |
