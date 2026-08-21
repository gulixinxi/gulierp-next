# TRAE MDM-001 API Handoff

> Audience: TRAE (web SPA owner). Source of truth for the MDM-001 REST contract.
> Status: **MDM_001_CODE_READY_OPERATOR_EVIDENCE_PENDING** — endpoints are
> wired and the build is green, but Operator-side PostgreSQL evidence is
> still required to upgrade the gate to **MDM_001_REAL_MASTER_DATA_VERIFIED**.

## 1. Endpoint root

All MDM-001 endpoints are mounted under `/api/v1/mdm/`:

| Resource         | List                            | Get                          | Create                       | Update                        |
|------------------|---------------------------------|------------------------------|------------------------------|-------------------------------|
| UOM              | `GET    /api/v1/mdm/uoms`        | `GET    /api/v1/mdm/uoms/{id}` | `POST   /api/v1/mdm/uoms`     | `PUT    /api/v1/mdm/uoms/{id}` |
| ItemCategory     | `GET    /api/v1/mdm/item-categories` | `GET    /api/v1/mdm/item-categories/{id}` | `POST   /api/v1/mdm/item-categories` | `PUT    /api/v1/mdm/item-categories/{id}` |
| Item             | `GET    /api/v1/mdm/items`       | `GET    /api/v1/mdm/items/{id}` | `POST   /api/v1/mdm/items`    | `PUT    /api/v1/mdm/items/{id}` |

All `POST` / `PUT` are **CSRF-protected** (X-CSRF-TOKEN header + `.GuliERP.Antiforgery` cookie) per G2-004R1 (DEC-AUTH-009). The SPA MUST call `GET /api/v1/auth/csrf` first and echo the token back in `X-CSRF-TOKEN` for every state-changing call. `GET` endpoints are CSRF-exempt.

## 2. Authentication + Authorization

- All endpoints require an authenticated session (G2-004 cookie scheme).
- Each endpoint is gated by a permission policy:

| Resource     | Read                              | Manage                                |
|--------------|-----------------------------------|---------------------------------------|
| UOM          | `mdm.uom.read`                    | `mdm.uom.manage`                      |
| ItemCategory | `mdm.item-category.read`          | `mdm.item-category.manage`            |
| Item         | `mdm.item.read`                   | `mdm.item.manage`                     |

The `manage` permission is required to `POST` / `PUT`; the `read` permission is enough for `GET`.

## 3. Tenant scope

- **UOM is system-scope** — no Tenant filter on read / write.
- **ItemCategory + Item are tenant-scope** — the API applies `TenantId == currentTenant.Id` from the `ICurrentTenant` context (resolved from the auth cookie's `tenant_id` claim by `AuthenticationContextMiddleware`).
- Cross-tenant access returns **404** (not 403) so resource existence is not leaked.

## 4. Wire DTOs

### 4.0 Wire ID contract (API-CONTRACT-ID-001)

**Every snowflake / HiLo id field on the wire is a JSON STRING, not a number.**
The backend uses `SnowflakeLongJsonConverter` (apps/api/GuliERP.Api/Kernel/).

| Field | Wire type |
|---|---|
| `id`, `parentId`, `categoryId`, `baseUomId`, `warehouseId`, `plantId` | `string` |
| `userId`, `tenantId`, `companyId` (in `LoginResponse` / `/auth/me`) | `string` |
| `concurrencyVersion`, `page`, `pageSize`, enum values (`dimension` / `kind` / `status` / `itemNature` / `role` / `type`) | `number` (unchanged) |
| `totalCount` (in `PagedResult<T>`) | `number` (unchanged) |

Rationale: JavaScript's `Number.MAX_SAFE_INTEGER` is 2^53 - 1 = 9_007_199_254_740_991. The GuliERP HiLo sequence (`identity.gulierp_hilo_sequence`) routinely produces ids above that (the user-reported real UOM id was `83727350616817740`). When a JS client parsed that as a JSON number, the value was silently rounded, the round-trip id no longer matched the database row, and the next `GET /.../{id}` returned 404.

SPA contract:
- Treat every id as an opaque `string` token. NEVER do `Number(id)`, `parseInt(id)`, or unary `+id`.
- Nullable ids are `string | null` (or absent).
- Route params carry the string as-is: `GET /api/v1/mdm/uoms/83727350616817740`. ASP.NET Core's route binder parses the URL segment as `long` via the `TypeConverter`; this is independent of JSON serialization, so any valid `long` (incl. > 2^53) round-trips losslessly.
- Request bodies accept both string (preferred) and number (backward-compat with already-shipped clients that did `Number(row.id)`).
- `Select` components and form fields use the string id as the `value` directly.
- An invalid id string (e.g. `"abc"`, empty `""`) causes the deserializer to throw `JsonException` → backend returns 400 with RFC7807 `validation_failed` (NEVER a 500).

### 4.1 UOM

**`UomDto`** (response body for GET / GET-by-id / POST / PUT):

```json
{
  "id": "1",
  "code": "KGM",
  "name": "Kilogram",
  "symbol": "kg",
  "dimension": 2,         // int: 1=COUNT 2=MASS 3=LENGTH 4=AREA 5=VOLUME 6=TIME
  "kind": 2,              // int: 1=DISCRETE 2=SI
  "status": 1,            // int: 1=ACTIVE 2=INACTIVE
  "description": null,
  "createdAt": "2026-08-20T10:00:00+00:00",
  "modifiedAt": "2026-08-20T10:00:00+00:00",
  "concurrencyVersion": 1
}
```

**`CreateUomRequest`** (POST body):

```json
{
  "code": "KGM",
  "name": "Kilogram",
  "symbol": "kg",
  "dimension": 2,
  "kind": 2,
  "description": null
}
```

**`UpdateUomRequest`** (PUT body; `code` is immutable in V1):

```json
{
  "name": "Kilogram (recalibrated)",
  "symbol": "kg",
  "status": 1,
  "description": "ISO 80000-4 mass unit",
  "expectedConcurrencyVersion": 1
}
```

### 4.2 ItemCategory

**`ItemCategoryDto`**:

```json
{
  "id": "5",
  "parentId": "3",
  "code": "RAW-STEEL",
  "name": "Raw Steel",
  "status": 1,
  "description": null,
  "createdAt": "2026-08-20T10:00:00+00:00",
  "modifiedAt": "2026-08-20T10:00:00+00:00",
  "concurrencyVersion": 1
}
```

**`CreateItemCategoryRequest`**:

```json
{
  "code": "RAW-STEEL",
  "name": "Raw Steel",
  "parentId": "3",
  "description": null
}
```

**`UpdateItemCategoryRequest`**:

```json
{
  "name": "Raw Steel (cold-rolled)",
  "parentId": "3",
  "status": 1,
  "description": null,
  "expectedConcurrencyVersion": 1
}
```

### 4.3 Item

**`ItemDto`**:

```json
{
  "id": "100",
  "code": "MAT-001",
  "name": "Steel plate",
  "specification": "1m x 2m x 3mm",
  "categoryId": "5",
  "baseUomId": "7",
  "itemNature": 1,        // int: 1=MATERIAL 2=SEMI_FINISHED 3=FINISHED_GOOD 4=SERVICE
  "status": 1,
  "description": null,
  "createdAt": "2026-08-20T10:00:00+00:00",
  "modifiedAt": "2026-08-20T10:00:00+00:00",
  "concurrencyVersion": 1
}
```

**`CreateItemRequest`**:

```json
{
  "code": "MAT-001",
  "name": "Steel plate",
  "specification": "1m x 2m x 3mm",
  "categoryId": "5",
  "baseUomId": "7",
  "itemNature": 1,
  "description": null
}
```

**`UpdateItemRequest`**:

```json
{
  "name": "Steel plate (heat-treated)",
  "specification": "1m x 2m x 3mm HT",
  "categoryId": "5",
  "baseUomId": "7",
  "itemNature": 1,
  "status": 1,
  "description": null,
  "expectedConcurrencyVersion": 1
}
```

## 5. Pagination + list query

All list endpoints support these query parameters:

| Parameter  | Type    | Default | Description                                       |
|------------|---------|---------|---------------------------------------------------|
| `keyword`  | string  | (none)  | Case-insensitive `contains` on `code` + `name`.   |
| `status`   | int     | (none)  | `1`=ACTIVE, `2`=INACTIVE. Omit for all.           |
| `page`     | int     | `1`     | 1-based page index.                               |
| `pageSize` | int     | `20`    | Capped at `200`.                                  |

Item / ItemCategory add:

| Parameter    | Type | Description                                |
|--------------|------|--------------------------------------------|
| `parentId`   | long | (ItemCategory only) Filter by parent. May be sent as a JSON string per §4.0. |
| `categoryId` | long | (Item only) Filter by category. May be sent as a JSON string per §4.0. |
| `itemNature` | int  | (Item only) 1..4 per the enum.             |

The response is a `PagedResult<T>`:

```json
{
  "items": [ /* T[] */ ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 42
}
```

## 6. Error contract

All errors return RFC 7807 / 9457 ProblemDetails with GuliERP extensions
(`code`, `requestId`, `traceId`):

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "MDM validation failed.",
  "status": 400,
  "detail": "ItemCategory with Code 'RAW-STEEL' already exists in this tenant.",
  "code": "mdm_duplicate_code",
  "requestId": "...",
  "traceId": "..."
}
```

| Code                                | HTTP | When                                                                          |
|-------------------------------------|------|-------------------------------------------------------------------------------|
| `mdm_not_found`                     | 404  | Resource not found in current scope.                                          |
| `mdm_duplicate_code`                | 400  | Code already exists (per-tenant for ItemCategory / Item; global for UOM).     |
| `mdm_uom_not_found`                 | 400  | Request referenced a non-existent UOM.                                        |
| `mdm_item_category_not_found`       | 400  | Request referenced a non-existent ItemCategory.                               |
| `mdm_item_category_cross_tenant`    | 400  | Request referenced an ItemCategory in a different Tenant.                     |
| `mdm_item_category_cycle`           | 400  | Update would create a self-reference or a cycle in the hierarchy.            |
| `mdm_validation_failed`             | 400  | Empty Code, empty Name, missing BaseUom, etc.                                 |
| `authorization_forbidden`           | 403  | User lacks the required permission.                                           |
| `authentication_required`           | 401  | No / expired auth cookie.                                                     |
| `csrf_validation_failed`            | 400  | Missing / invalid X-CSRF-TOKEN on a state-changing request.                   |
| `validation_failed`                 | 400  | Body / query / path failed ASP.NET Core model validation.                     |

## 7. Optimistic concurrency

Every `Update*` request must include `expectedConcurrencyVersion` matching the
value from the most recent `GET`. Mismatch returns 400 +
`mdm_validation_failed`. The server increments the version on every successful
update; the SPA must re-read the DTO to obtain the new value.

## 8. Code canonicalization

All `code` fields are **trimmed and uppercased** before storage. The SPA may
send any case (`"mat-001"`, `"MAT-001"`, `"  Mat-001  "`); the server stores
`"MAT-001"`. The unique index is on the canonical form, so
`mat-001` and `MAT-001` collide (the second insert is rejected with
`mdm_duplicate_code`).

## 9. TRAE prototype reconciliation

| TRAE prototype assumption          | V1 reality                                                         |
|------------------------------------|--------------------------------------------------------------------|
| `Uom.DecimalPlaces` (int 0-6)      | **REMOVED.** Not a UOM field. Precision is a business semantic, deferred. |
| `ItemCategory.Level` (int)         | **REMOVED.** Derived UI data; the SPA computes it.                 |
| `ItemCategory.FullPath` (string)   | **REMOVED.** Derived UI data; the SPA computes it.                 |
| `Item.itemType` (goods/service/package) | **RENAMED → `itemNature`** with values `MATERIAL` / `SEMI_FINISHED` / `FINISHED_GOOD` / `SERVICE`. `PACKAGE` is **DEFERRED**. |
| `Item.InventoryMethod` (FIFO / LIFO / WEIGHTED_AVG / SPECIFIC) | **REMOVED.** Inventory accounting — not a UOM / Item concern. Deferred to the Inventory Goal. |
| 7 booleans on Item                 | **REMOVED.** The single `itemNature` enum is the V1 truth.         |
| Item image                         | **REMOVED.** Not in V1.                                            |

## 10. Auth + CSRF requirement summary

| Verb   | Cookie required | CSRF token required | Permission required |
|--------|-----------------|---------------------|---------------------|
| GET    | yes             | no                  | `*.read`            |
| POST   | yes             | yes                 | `*.manage`          |
| PUT    | yes             | yes                 | `*.manage`          |

## 11. Forbidden / not implemented in V1

- **No Numbering / Coding Engine** — `code` is manual. SUP-001 future.
- **No SoftDelete Framework** — V1 uses `Status = INACTIVE` only.
- **No Audit Endpoints** — audit fields are written by the service; the
  audit log API is a future Foundation deliverable.
- **No InventoryMethod / SourcingPolicy / Image / Spec wizard** — V1 carries
  `Specification` (free text) and the single `itemNature` enum.
- **No automatic Tenant provisioning** — the SPA must already have a Tenant
  context (from the G2-003 Identity login).

## 12. Test evidence

| Suite                                           | Count | Status                                |
|-------------------------------------------------|-------|---------------------------------------|
| `GuliERP.Mdm.Tests` (unit)                      | 19    | 19 / 19 PASS (this build).            |
| `GuliERP.Mdm.IntegrationTests` (real PG)        | 8     | Operator-required; see `tools\dev\mdm-001-operator-evidence.ps1`. |

## 13. Migration + seed evidence

- Migration: `20260820190000_MDM001_InitializeMdmSchema` creates
  `mdm` schema + 3 tables + indexes + FKs.
- Sequence: reuses `identity.gulierp_hilo_sequence` (created by IDGEN001).
- Seed: `MdmSeed` loads the curated 13 DEV `SAFE_TO_SEED_SYSTEM` UOM rows
  from `data/bootstrap/reference/system/uom.json`. The 8 `PROPOSED` external
  SI rows are **NOT** auto-seeded (per MDM-000 §15).

## 14. Gate

Current gate: **MDM_001_CODE_READY_OPERATOR_EVIDENCE_PENDING**.

Upgrade to **MDM_001_REAL_MASTER_DATA_VERIFIED** requires the Operator to run
`tools\dev\mdm-001-operator-evidence.ps1` end-to-end against the canonical
PostgreSQL target (`gulierp_g2_003_test`). The script asserts the DB target,
applies the migration, runs both test suites, and prints the final gate.
