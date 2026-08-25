# G2 — API Standard V1 Draft

| Field | Value |
|---|---|
| Goal | G2 — REST API standard: routes, request/response, error contract, pagination, idempotency, concurrency, headers |
| Entry Gate | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Output Status | **DRAFT — for review only** |
| Author | Mavis (single writer, API role) |
| Companion docs | `G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md` (TASK B), `G2_SECURITY_ARCHITECTURE_V1_DRAFT.md` (TASK C), `G2_MODULE_RUNTIME_ARCHITECTURE_V1_DRAFT.md` (TASK D) |
| Style | REST + minimal API, JSON only, no SOAP, no XML, no RPC, no GraphQL (V1) |
| Auth | JWT Bearer (per TASK C §2.1) |

---

## 1. Mission

Define a **strict, reviewable, evolvable** HTTP API standard for GuliERP. The standard must be:

1. **Consistent** across all modules. A Sales endpoint looks like an Inventory endpoint.
2. **Self-documenting** via OpenAPI 3.1. Every endpoint is annotated.
3. **Hard to misuse**. Idempotency, concurrency, and pagination are conventions, not afterthoughts.
4. **Compatible** with the G2 Foundation (auth, tenant, audit) and the G2 Module Runtime (per-module endpoints).
5. **Free of low-code generic endpoints** ("CRUD any table") that historically enabled corruption.

---

## 2. Base path and version

- Base: `/api/v{version}/{module}/{resource}`
- V1: `/api/v1/{module}/{resource}` (literal `v1`, not `v1.0`)
- The version is in the **path**, not a header. Breaking changes = new path (`/api/v2/...`). Non-breaking additions do not bump the version.
- A new module adds paths under `v1`; it does not create a new version.

Examples:
```
POST   /api/v1/auth/login
POST   /api/v1/auth/refresh
POST   /api/v1/auth/logout
GET    /api/v1/auth/me

GET    /api/v1/sales/orders
POST   /api/v1/sales/orders
GET    /api/v1/sales/orders/{id}
PUT    /api/v1/sales/orders/{id}
POST   /api/v1/sales/orders/{id}/submit
POST   /api/v1/sales/orders/{id}/approve
POST   /api/v1/sales/orders/{id}/reject
POST   /api/v1/sales/orders/{id}/close
POST   /api/v1/sales/orders/{id}/cancel
GET    /api/v1/sales/orders/{id}/history

GET    /api/v1/inventory/items
POST   /api/v1/inventory/items
GET    /api/v1/inventory/items/{id}
PUT    /api/v1/inventory/items/{id}

GET    /api/v1/inventory/warehouses
GET    /api/v1/inventory/warehouses/{id}/stock

POST   /api/v1/dictionaries/{code}/items
GET    /api/v1/dictionaries/{code}/items
```

### 2.1 Module path discipline

- A module **owns** its `/api/v1/{module}/**` namespace. Cross-module routes are forbidden.
- The architecture test fails any route outside its module's namespace (matches `M17` from TASK D §10).
- A Foundation route (e.g. `/api/v1/auth/**`, `/api/v1/admin/**`) is owned by the Foundation and may not be added to by modules.

### 2.2 Resource naming

- Resources are **plural nouns** (`orders`, `items`, `warehouses`).
- Sub-resources use nested paths: `/api/v1/sales/orders/{id}/lines`.
- Actions on a resource are sub-paths: `/api/v1/sales/orders/{id}/submit`, `/api/v1/sales/orders/{id}/approve`. These are **state transitions**, not generic verbs.
- Generic verbs (`/api/v1/sales/orders/{id}/execute`, `/api/v1/sales/orders/{id}/process`) are **forbidden** because they hide business meaning. Use the state-transition name.

---

## 3. HTTP method conventions

| Method | Use | Idempotent? | Body |
|---|---|---|---|
| `GET` | Read a resource or list | Yes (safe) | None |
| `POST` | Create a new resource, or perform a non-idempotent action | No | Request body |
| `PUT` | Replace a resource entirely (the whole document) | Yes | Full resource body |
| `PATCH` | Partial update (a small set of fields) | No (not idempotent unless `Idempotency-Key`) | Partial body |
| `DELETE` | Soft-delete or hard-delete a resource (V1: hard delete + audit) | Yes | None |

**Why no `PATCH` in V1.0**: partial updates hide business state transitions. A `PATCH sales_orders.status = 'Confirmed'` is exactly what `POST /sales/orders/{id}/confirm` is for, with the correct concurrency check, audit entry, and approval flow. `PATCH` is V1.5 with a strict JSON-Patch spec and a per-field permission layer.

### 3.1 Idempotency

`POST` and `PATCH` MAY include an `Idempotency-Key` header. The server stores `(key, request_hash, response_status, response_body, expires_at)` for 24 hours. A second request with the same key returns the cached response (without re-executing the side effect). The `request_hash` ensures the same key with a **different** body returns `409 IDEMPOTENCY_KEY_MISMATCH` (defense against client bug).

`PUT` and `DELETE` are idempotent by HTTP definition; they do not need a key.

### 3.2 State transitions

State-changing actions on a document are **sub-paths** with `POST`:

```
POST /api/v1/sales/orders/{id}/submit
POST /api/v1/sales/orders/{id}/withdraw
POST /api/v1/sales/orders/{id}/approve
POST /api/v1/sales/orders/{id}/reject
POST /api/v1/sales/orders/{id}/confirm
POST /api/v1/sales/orders/{id}/close
POST /api/v1/sales/orders/{id}/cancel
```

The body is **always**:
```json
{ "expectedVersion": 3, "reason": "optional human reason", ...actionSpecificFields }
```

`expectedVersion` is **mandatory** for state transitions (optimistic concurrency). The server returns `409 CONCURRENCY_CONFLICT` if the version is stale.

### 3.3 Forbidden endpoints

| Endpoint pattern | Why forbidden |
|---|---|
| `POST /api/v1/{module}/execute` or `/run` | Hides business meaning; "execute what?" |
| `POST /api/v1/{module}/batch` (generic) | Hides the resource; use batch headers on the resource endpoint |
| `GET /api/v1/{module}/query?filter=...` with raw expression | SQL injection risk; bypasses EF Core |
| `POST /api/v1/{module}/import` (generic) | Hides the resource; use a per-resource bulk endpoint |
| `* /api/v1/_admin/sql` | SQL over HTTP is an emergency door; never in V1 |
| `* /api/v1/_dynamic/{table}` | The "generic CRUD engine" anti-pattern |

---

## 4. Request body conventions

### 4.1 Content-Type

- All request and response bodies are `application/json; charset=utf-8`.
- No XML, no form-urlencoded for resources (form-urlencoded is fine for `application/x-www-form-urlencoded` on `/auth/login` if the frontend prefers; same JSON API otherwise).
- No `application/octet-stream` for resources (files use the Foundation's `ObjectStore` API; see TASK B §6).

### 4.2 JSON formatting

- UTF-8, no BOM.
- 2-space indent is **not** required (server can be configured; client determines). Wire size matters.
- `snake_case` for **all** field names (request, response, error envelope). The C# side uses a global `JsonNamingPolicy.SnakeCaseLower` converter.
- Numbers as JSON numbers (not strings). Snowflake IDs are JSON **numbers** on the wire (see §4.4 for the Admin.NET string-quirk reference).
- Dates:
  - Date + time: ISO 8601 with timezone, e.g. `"2026-08-15T10:30:00.000+08:00"` or `"2026-08-15T02:30:00.000Z"`. The server accepts both and stores in UTC.
  - Date only: `"2026-08-15"`.
- Enums: as their string name (`"Draft"`, `"Confirmed"`). Not as integers.
- Money amounts: JSON number with **at most 4 decimal places**. Currency is a separate `currency_code` field (ISO 4217 alpha-3).
- Booleans: `true` / `false`, lowercase.
- `null`: explicit (not omitted) for optional fields. The contract distinguishes "absent" from "null" by the OpenAPI spec (`required: [...]`).
- No trailing commas. No comments.

### 4.3 Required vs optional

- The OpenAPI document is the **contract**. A field is required if and only if it is in the `required: [...]` array of the schema.
- Server validates required fields and returns `400 VALIDATION_ERROR` with details listing missing fields.

### 4.4 Snowflake IDs on the wire

- **Decision (GuliERP)**: snowflake IDs are JSON **numbers** on the wire. This is the standard for JavaScript clients (no precision loss for numbers up to 2^53 - 1; snowflakes max out at 2^63 - 1 which is too large for JS Number, but in practice all our snowflakes are well below 2^53 for the next ~100 years).

> **Reference**: the previous Admin.NET-based project (`poc/adminnet/Admin.NET/Admin.NET.Web.Core/Startup.cs:97`) registered `setting.Converters.AddLongTypeConverters()` to **wrap long → JSON String** to defend against JS precision loss. This is the Admin.NET framework's design choice. **GuliERP does NOT copy that decision.** We have chosen to send snowflake IDs as JSON numbers; the frontend uses `bigint` helpers (e.g. `json-bigint` for axios) when it needs full 64-bit precision. This is a deliberate departure from the previous project's wire contract.

- For values that may exceed 2^53 (e.g. extremely long-running snowflake counts), the server sends them as JSON strings with a `stringId` field suffix, e.g. `"id": "1793456789012345678"`. The schema documents the type as `string` for those fields.

---

## 5. Response body conventions

### 5.1 Success response shape (single resource)

```json
{
  "data": {
    "id": 1793456789012345678,
    "tenantId": 1001,
    "companyId": 2001,
    "orderNo": "SO-20260815-0001",
    "status": "Draft",
    "approvalStatus": "NotRequired",
    "executionStatus": "Open",
    "customerId": 3001,
    "customerCode": "C-001",
    "customerName": "上海某某贸易有限公司",
    "documentDate": "2026-08-15",
    "currencyCode": "CNY",
    "totalAmount": 11300.0000,
    "taxAmount": 1300.0000,
    "lines": [
      { "lineNo": 1, "itemId": 4001, "quantity": 10.0000, "unitPrice": 100.0000, "amount": 1000.0000, "taxRate": 0.13, "taxAmount": 130.0000 }
    ],
    "createdAt": "2026-08-15T10:30:00.000Z",
    "createdBy": 5001,
    "updatedAt": "2026-08-15T10:30:00.000Z",
    "updatedBy": 5001,
    "concurrencyVersion": 1
  },
  "meta": {
    "requestId": "req_8a3b...",
    "traceId": "trace_...",
    "serverTime": "2026-08-15T10:30:00.123Z"
  }
}
```

The envelope `{ data, meta }` is **always** present. List responses use `{ data: [...], meta: { ..., pagination } }`.

### 5.2 List response with pagination

```json
{
  "data": [ /* ... up to pageSize items ... */ ],
  "meta": {
    "requestId": "...",
    "traceId": "...",
    "serverTime": "...",
    "pagination": {
      "page": 1,
      "pageSize": 20,
      "totalItems": 1234,
      "totalPages": 62,
      "hasNext": true,
      "hasPrev": false
    }
  }
}
```

### 5.3 Empty list

`{ "data": [], "meta": { "pagination": { "page": 1, "pageSize": 20, "totalItems": 0, "totalPages": 0, "hasNext": false, "hasPrev": false } } }`

`data` is **always** an array, never `null`. An empty list is `[]`.

### 5.4 204 No Content

Used for `DELETE` success where there is no body. The server still returns the `meta` envelope headers (request id, trace id) in HTTP headers (`X-Request-Id`, `X-Trace-Id`).

### 5.5 Response headers (always)

| Header | Value | Notes |
|---|---|---|
| `Content-Type` | `application/json; charset=utf-8` | |
| `X-Request-Id` | The `RequestId` from the request (echo) | Set by `UseRequestId` middleware |
| `X-Trace-Id` | The `TraceId` (OpenTelemetry-compatible) | Set by `UseRequestId` middleware |
| `Cache-Control` | `no-store` for non-GET; `private, max-age=...` for cacheable GETs | Most V1 endpoints are `no-store` |
| `ETag` | For `GET` of a single resource | Optional V1; recommended for static dictionaries |
| `Vary` | `Authorization` | Per-user responses |

---

## 6. Headers (request)

| Header | Required? | Purpose |
|---|---|---|
| `Authorization` | Required for all endpoints except `/auth/login` and `/healthz` | `Bearer <accessToken>` |
| `Content-Type` | Required for `POST`/`PUT`/`PATCH` | `application/json; charset=utf-8` |
| `Accept` | Optional | `application/json` (default) |
| `Accept-Language` | Optional | `zh-CN` (default V1; ignored otherwise) |
| `X-Request-Id` | Optional (generated if absent) | Client-supplied correlation id |
| `X-Company-Id` | Optional (used to switch active company) | Must be in user's `UserCompany` set |
| `Idempotency-Key` | Optional on `POST` | UUID v4 recommended; max 128 chars |
| `If-Match` | Optional V1.5 (currently we use body's `expectedVersion`) | ETag for `GET` cache validation |

**Forbidden headers** (server ignores them silently or rejects with 400):

| Header | Why forbidden |
|---|---|
| `X-Tenant-Id` (from body / query) | Tenant is in JWT; never trust browser for tenant |
| `X-User-Id` (from body / query) | User is in JWT; never trust browser for user |
| `X-Bypass-Auth` | No such thing |

---

## 7. Error contract (unified envelope)

All errors return the **same** JSON shape:

```json
{
  "error": {
    "code": "SO_LINE_QTY_INVALID",
    "message": "数量必须大于 0",
    "details": {
      "lineNo": 3,
      "field": "quantity",
      "value": 0
    },
    "traceId": "trace_...",
    "requestId": "req_...",
    "documentation": "https://docs.gulierp.example.com/errors/SO_LINE_QTY_INVALID"
  }
}
```

| Field | Required | Purpose |
|---|---|---|
| `code` | Yes | Machine-readable stable error code, e.g. `SO_LINE_QTY_INVALID`. Format: `{MODULE}_{DOMAIN}_{REASON}`. Always UPPER_SNAKE. **Never** localized; the `message` is localized. |
| `message` | Yes | Human-readable, zh-CN V1. Stable wording; the frontend can display it directly. |
| `details` | No | Structured context for the frontend (e.g. which field, which row). Keys are `snake_case`. |
| `traceId` | Yes | Server-side trace, for log correlation |
| `requestId` | Yes | Client-side correlation |
| `documentation` | No | URL to a public docs page describing the error (V1.5+) |

The envelope is **always** the same shape, regardless of HTTP status. The status code is **also** present (the frontend can use either).

### 7.1 HTTP status mapping

| Class | Status | Used for |
|---|---|---|
| 2xx | 200 | Successful read, update, action |
| 2xx | 201 | Successful create |
| 2xx | 204 | Successful delete (no body) |
| 4xx | 400 | `VALIDATION_ERROR`, `MALFORMED_JSON`, missing required field |
| 4xx | 401 | `UNAUTHENTICATED` — missing or invalid token |
| 4xx | 403 | `PERMISSION_DENIED`, `TENANT_ACCESS_DENIED`, `COMPANY_ACCESS_DENIED` |
| 4xx | 404 | `NOT_FOUND`, or the resource's module is not enabled in this edition (deliberately indistinguishable) |
| 4xx | 409 | `CONCURRENCY_CONFLICT`, `DUPLICATE_KEY`, `IDEMPOTENCY_KEY_MISMATCH`, state machine violation (e.g. trying to `Confirm` an already-Closed order) |
| 4xx | 422 | `BUSINESS_RULE_VIOLATION` — semantically valid but business-rejected (e.g. discount > 100%) |
| 4xx | 423 | `ACCOUNT_LOCKED` — login lockout |
| 4xx | 429 | `RATE_LIMITED` |
| 5xx | 500 | `INTERNAL_ERROR` — unexpected; details NOT exposed |
| 5xx | 503 | `SERVICE_UNAVAILABLE` — DB down, etc. |

**Mapping rule**: 4xx means **the client did something wrong** (and can fix it). 5xx means **the server did something wrong** (and the client should retry, possibly with backoff).

### 7.2 Error code catalog (mandatory)

Every error code in the system is **registered** in a central catalog (`docs/api/error-codes.md` or generated from C#). The catalog has:

- The code
- The HTTP status
- A one-line description
- The owning module
- The remediation hint

This catalog is the **authoritative** source — the server emits only codes from this catalog, and the frontend's error-handling switch is generated from it.

### 7.3 Internal error hiding (HR-1, HR-2)

For any 5xx error, the response envelope's `code` is `INTERNAL_ERROR`, the `message` is a generic "An unexpected error occurred. Please try again later.", and the `details` is empty. The **actual** exception, stack trace, and request body are written to the server log with the `requestId`, so an operator can correlate.

**Never** expose stack traces in any environment (including Development). The Development environment can include a `developerHint` field in the envelope (still off by default; opt-in via config).

---

## 8. Pagination

V1 uses **offset-based pagination** (page + pageSize). Cursor-based is V1.5.

### 8.1 Request

```
GET /api/v1/sales/orders?page=1&pageSize=20&sort=-documentDate,orderNo
```

| Query param | Default | Notes |
|---|---|---|
| `page` | 1 | 1-indexed |
| `pageSize` | 20 | Allowed: 20, 50, 100. Other values return 400. |
| `sort` | module default (e.g. `-documentDate,id`) | Comma-separated; `-` prefix = desc. Allowed fields are restricted per endpoint (whitelist). |
| `filter[field]` | none | Field-specific. See §10. |
| `q` | none | Free-text search (when the endpoint supports it) |

### 8.2 Response

`meta.pagination` as per §5.2.

### 8.3 Performance budget

- A list query must return in < 200ms p99 for the default page size on a 1M-row table.
- The query must use an index. `EXPLAIN` verification is part of code review (see PostgreSQL standard §10.5).
- `OFFSET` is acceptable up to page 1000; beyond that, the API returns `400 PAGINATION_TOO_DEEP` and suggests using filters or the export endpoint.

### 8.4 Cursor pagination (V1.5)

For very large lists, V1.5 introduces `cursor` (an opaque token) for forward-only pagination. V1 does not.

---

## 9. Sorting and filtering

### 9.1 Sort whitelist

- The `sort` parameter accepts only fields whitelisted in the endpoint's OpenAPI annotation.
- An unknown field returns `400 INVALID_SORT_FIELD`.
- Default sort is the endpoint-specific "natural" order (e.g. sales orders: `-documentDate,id`).

### 9.2 Filter shape

Filters are **field-by-field query parameters**, not a free-form expression:

```
GET /api/v1/sales/orders?status=Draft&customerId=3001&dateFrom=2026-08-01&dateTo=2026-08-31
```

- `status` is enum-validated.
- `customerId` is a snowflake.
- `dateFrom` / `dateTo` are inclusive ISO 8601 dates.
- Each filter is **whitelisted** in the endpoint's OpenAPI annotation; unknown filters return `400 INVALID_FILTER_FIELD`.

**Forbidden**: `?filter={"and":[{"field":"total","op":">","value":1000}]}` style expressions (JSON-in-query). They are a SQL injection vector and a maintenance nightmare. V1.5 may add a curated set of operators (`eq`, `gt`, `gte`, `lt`, `lte`, `in`, `between`, `like`) as **typed** query params per field.

### 9.3 No raw SQL / filter expression

The server **never** accepts a raw filter expression, raw SQL, or a table name from the client. Every query is built by EF Core's LINQ provider. A raw SQL query in business code is allowed only via the `IQueryable` extension methods that go through parameterized SQL.

---

## 10. Authentication headers (already in TASK C §2.1)

- All endpoints except `/auth/login`, `/healthz`, `/readyz` require `Authorization: Bearer <accessToken>`.
- The access token TTL is 15 min; refresh via `/auth/refresh`.
- Missing or invalid token: `401 UNAUTHENTICATED`.
- Expired token: `401 TOKEN_EXPIRED` (frontend should refresh and retry).

---

## 11. Tenant + Company context (already in TASK C §3, §4)

- Tenant is from JWT (`tid` claim). Never from request.
- Company is from JWT (`cid` claim) or from `X-Company-Id` header (after server-side membership check).
- The server returns `403 TENANT_ACCESS_DENIED` if a query somehow attempts to read another tenant's data (should be impossible with the global query filter; this is the safety net).
- The server returns `403 COMPANY_ACCESS_DENIED` if `X-Company-Id` is sent but the user is not in that company.

---

## 12. Idempotency (V1)

### 12.1 Header

```
POST /api/v1/sales/orders
Idempotency-Key: 8b4f7d2a-1234-4abc-9def-0123456789ab
```

- The key is opaque to the server. UUID v4 is the recommended format.
- Maximum length 128 characters.
- The server scopes the key by `(tenant_id, user_id, endpoint, key)` — different tenants can use the same key without collision.

### 12.2 Server behavior

1. Server receives a request with `Idempotency-Key`.
2. Server computes `request_hash = SHA-256(method + path + body)`.
3. Server checks `idempotency_key` table for `(tenant_id, user_id, endpoint, key)`:
   - Not found: process the request, store `(key, hash, response_status, response_body, expires_at)`, return the response.
   - Found and hash matches: return the stored response immediately (do not re-execute).
   - Found and hash mismatches: return `409 IDEMPOTENCY_KEY_MISMATCH`.
   - Found and `expires_at < now()`: treat as not found, process, store, return.

### 12.3 TTL

24 hours. After 24h, the key is expired and a new request with the same key is processed normally (this is desired: a user retrying the same operation 25h later is a new operation).

### 12.4 Idempotency on state transitions

A `POST /api/v1/sales/orders/{id}/confirm` with `Idempotency-Key` is idempotent: a retry returns the same response (e.g. the now-Confirmed order) without double-incrementing the version or double-emitting an audit entry. This is **required** for approval flows where users may double-click.

---

## 13. Concurrency

- Every state-transition endpoint requires the body's `expectedVersion` (an integer).
- The server compares to the row's `concurrency_version`:
  - Match: process, increment, return new version.
  - Mismatch: return `409 CONCURRENCY_CONFLICT` with the current version in `details.currentVersion`.
- The frontend **must** read the current version from the `GET` response and echo it back.
- For non-state-changing reads (e.g. `GET`), no version check is needed.

**Why we do not use HTTP `If-Match` / ETag for state transitions in V1**: it is awkward to convey in a JSON body when the resource is nested. `expectedVersion` in the body is unambiguous and easier to type-check. V1.5 may introduce `If-Match` for `GET` caching.

---

## 14. Validation

- The server validates every request body against the OpenAPI schema.
- Validation errors return `400 VALIDATION_ERROR` with `details.errors` listing the offending fields:

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "请求参数校验失败",
    "details": {
      "errors": [
        { "field": "lines[0].quantity", "code": "REQUIRED", "message": "数量为必填" },
        { "field": "lines[1].unitPrice", "code": "MIN_VALUE", "message": "单价不能小于 0" }
      ]
    },
    "traceId": "...",
    "requestId": "..."
  }
}
```

- Validation runs **before** any business logic. No DB call, no audit call, no permission call.
- The validation library is FluentValidation (or DataAnnotations + custom). Both are acceptable; pick per module, document the choice.

---

## 15. Bulk operations

V1 supports **bounded** bulk operations. Unbounded bulk is forbidden (memory + transaction size).

- `POST /api/v1/sales/orders/bulk-approve` accepts up to **100** ids in one request.
- A bulk operation is **atomic** for its scope: either all 100 succeed, or all 100 fail (with per-id error details).
- A bulk operation emits **one** audit entry per item (so the audit trail is per-document, not per-bulk-call).

V1.5 introduces async bulk (returns a job id; the frontend polls for status).

---

## 16. File upload / download

- Upload: `POST /api/v1/{module}/attachments` (multipart/form-data). The file becomes an `ObjectStore` row. Returns the `attachment_id`.
- Download: `GET /api/v1/{module}/attachments/{id}/content` returns the file stream with `Content-Disposition: attachment; filename="..."`.
- File size limit: 100 MB per file in V1 (configurable per edition).
- **Tenant isolation**: an attachment is scoped to `(tenant_id, module_id)`. Cross-tenant download returns 404.
- **Virus scan** is V1.5 (today: a simple content-type whitelist + size limit).

---

## 17. Webhooks / push (V1.5+)

V1 is request/response only. The frontend polls for state changes (or uses a future WebSocket / SSE endpoint). Webhooks to external systems are V1.5.

---

## 18. OpenAPI documentation

- Every endpoint has an OpenAPI 3.1 annotation.
- The OpenAPI document is **the contract**. Tests are generated from it (e.g. via `NSwag` or `Spectral`).
- The OpenAPI document is **published** at `/api/v1/openapi.json` and rendered as Swagger UI at `/api/v1/docs`.
- The OpenAPI document is **versioned** with the API (`/api/v1/openapi.json`, never `/openapi.json`).
- Breaking changes require a new path (`/api/v2/...`) and a corresponding new OpenAPI document.

---

## 19. Rate limiting (V1 minimal)

- `POST /api/v1/auth/login`: 5 req / min / IP, 20 req / hour / username (per TASK C §9).
- All other endpoints: **no rate limit in V1** (internal network). V1.5 adds per-user / per-IP / per-endpoint limits.

---

## 20. Caching

- `GET` of **single resources** (e.g. `GET /api/v1/sales/orders/{id}`) returns `Cache-Control: private, max-age=10` (10 seconds) by default. The frontend may cache, but should re-fetch on focus / before any state-changing action.
- `GET` of **lists** does not cache.
- `GET` of **dictionaries** (`/api/v1/dictionaries/{code}/items`) caches for 5 minutes (in-memory, with a Foundation-level invalidation hook).
- All `POST` / `PUT` / `DELETE` are `Cache-Control: no-store`.
- **Server-side caching** of responses is **forbidden in V1** (multi-instance deployment + per-user responses makes it complex). In-memory caches (5-min `IDictionaryQuery` cache) are for **data**, not HTTP responses.

---

## 21. Anti-patterns we explicitly reject

| Anti-pattern | Why rejected | GuliERP's stance |
|---|---|---|
| **Generic CRUD endpoint** (`POST /api/v1/{module}/{resource}/{id}` that does anything) | Hides business meaning; creates 500-line controllers | **Banned**. Each action is a specific sub-path. |
| **Dynamic table endpoint** (`/api/v1/dynamic/{table}`) | SQL injection, no business rules, the Admin.NET anti-pattern | **Banned**. |
| **Raw SQL via query parameter** | SQL injection | **Banned**. |
| **Returning 200 with an error in the body** | Breaks HTTP semantics, breaks client error handling | **Banned**. Errors return non-2xx with the error envelope. |
| **Returning 200 with `success: false`** | Same as above | **Banned**. |
| **Different error shapes per module** | Frontend error handling becomes a mess | **Banned**. The envelope is the same for every endpoint. |
| **Exposing internal stack traces** (even in Development) | HR-1, HR-2 | **Banned**. |
| **Logging the request body** by default | PII / secrets leak risk | **Banned**. Opt-in per endpoint, with field-level redaction. |
| **Storing the JWT in localStorage** (frontend) | XSS-exfiltrable | Frontend guidance: store in memory + refresh; httpOnly cookie in V1.5. |
| **Returning the full user object on every endpoint** | PII bloat | **Banned**. The user object is only on `/auth/me`. |
| **PUT-as-create** (`PUT /api/v1/x/orders/123` creates if absent) | Hides 404 vs 201 | **Banned**. Use `POST` for create. |
| **DELETE returning the deleted object** | Wastes bandwidth; the caller already has it | **Banned**. Return 204. |
| **Returning `null` for "no result" lists** | Frontend has to null-check | **Banned**. Lists are always `[]`. |
| **Returning timestamps as Unix epoch numbers** | Loses timezone info, hard to read | **Banned**. Always ISO 8601 with offset. |
| **Mixed case field names** (CamelCase + snake_case) | Frontend conversion bugs | **Banned**. Always `snake_case` on the wire. |

---

## 22. Open questions for Operator / next G2 stage

| # | Question | Default if unanswered |
|---|---|---|
| Q1 | Should we use `application/problem+json` (RFC 7807) instead of the custom envelope? | Custom envelope (more expressive, our own contract) |
| Q2 | Should list endpoints support `?fields=id,orderNo,status` (sparse fieldsets)? | V1.5 (V1 returns the full DTO) |
| Q3 | Should we use `ETag` for `GET` caching in V1? | Optional, encouraged for hot endpoints |
| Q4 | Should `POST` of a state transition be idempotent by default (with `Idempotency-Key`)? | Yes — clients should always send a key for state transitions |
| Q5 | Do we need a `HEAD` for every `GET`? | V1.5 |

---

## 23. Stage 1 deliverable checklist (when implementation starts)

- [ ] All Foundation endpoints (`/auth/*`, `/admin/*`, `/healthz`, `/readyz`, `/openapi.json`, `/docs`) implemented and tested
- [ ] The unified error envelope is in place and tested for every error class (400, 401, 403, 404, 409, 422, 423, 429, 500, 503)
- [ ] The error code catalog is generated from C# and includes at least 30 example codes
- [ ] Idempotency-Key works end-to-end (send, replay, mismatch, expiry)
- [ ] Concurrency check works (concurrent updates return 409 with `currentVersion`)
- [ ] `X-Company-Id` is honored only for users in that company
- [ ] OpenAPI document is generated and valid (`spectral lint` passes)
- [ ] A smoke test: `POST /api/v1/auth/login` → `GET /api/v1/auth/me` → `POST /api/v1/auth/refresh` returns 200
- [ ] A smoke test: a forced 500 returns the unified envelope with no stack trace
- [ ] A smoke test: 401 on missing token, 403 on missing permission

**No business endpoints in Stage 1.** Stage 1 is the substrate.

---

*End of G2 API Standard V1 Draft — Status: DRAFT. Companion: TASK B (Foundation), TASK C (Security), TASK D (Module Runtime), TASK E (PostgreSQL), TASK G (Approval).*
