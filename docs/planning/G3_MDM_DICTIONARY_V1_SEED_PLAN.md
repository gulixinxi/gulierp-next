# G3 MDM Dictionary V1 Seed Plan

| Field | Value |
|---|---|
| **Plan ID** | `G3_MDM_DICTIONARY_V1_SEED_PLAN` |
| **Goal** | `G3_MDM_DICTIONARY_V1_SEED_001` |
| **Project** | `D:\guli\projects\gulierp-next` |
| **HEAD** | `9684985` (master, post V1 multi-agent acceptance) |
| **Predecessor** | `G3_MDM_FOUNDATION_AUDIT_001` (HIGH #1 priority) |
| **Author** | Mavis (M3 / mavis), acting as GuliERP AI Factory Planning + Review Agent |
| **Date** | 2026-08-25 (Asia/Shanghai) |
| **Status** | **PROPOSAL — awaiting user ratification** |
| **Per Brief** | NO code / DB / migration change. NO commit / push. Plan + design only. |

This plan designs the **V1 system dictionary seed** for GuliERP
Next. It covers 9 system dictionary types + ~40-60 system
dictionary items, idempotent + env-override + sentinel pattern
(reusing `MdmSeed.cs`'s proven approach for UoM).

---

## 0. Executive Summary

| Item | Value |
|---|---|
| **9 system dictionary types** | DOCUMENT_STATUS, CUSTOMER_TYPE, SUPPLIER_TYPE, ITEM_STATUS, EMPLOYEE_STATUS, PAYMENT_METHOD, TRANSPORT_MODE, SETTLEMENT_METHOD, ENTERPRISE_TYPE |
| **~40-60 system items** (sum across 9 types) | All `IsSystem=true`, `IsDefault=true` on one item per type, all `Status=Active` |
| **Idempotent** | Sentinel-based skip per dictionary type (extend `MdmSeed.cs` pattern) |
| **Env-overridable** | Per-type env var: `GULIERP_MDM_DICT_SEED_FILE_<TYPE>` |
| **Reuse `data/bootstrap/reference/tenant-template/`** | 3 of 9 dicts already curated (`business-partner-type.json`, `payment-method.json`, `position.json`) |
| **New JSON files** | 6 new files in `data/bootstrap/reference/system/` |
| **Implementation B1 / B2 / B3** | Backend (1 Goal, ~3 days) / Seed Data (this Plan, 0 code) / Verification (1 Goal, ~1 day) |
| **Code touched** | 1 new `MdmDictionarySeed.cs` (extends `MdmSeed.cs` pattern) + 1 new `IDictionarySeedRunner` interface + 1 `SeedHostedService` (auto-run on app start, dev/test only) |
| **Migrations** | 0 (the `20260825014004_AddMdmDictionaryTypesAndItems` already exists; no new migration needed) |
| **DB changes** | 0 (all data via `MdmDbContext.Add` + `SaveChangesAsync` through `MdmDictionaryService`) |

**Final Gate**: `G3_MDM_DICTIONARY_V1_SEED_READY`

---

## 1. TASK 1 — Current Seed Mechanism (read-only audit)

### 1.1 `MdmSeed.cs` (existing template)

`modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmSeed.cs` (249 lines).

| Field | Value |
|---|---|
| File path constant | `data/bootstrap/reference/system/uom.json` |
| Sentinel | `BENG` (first-row code, idempotent skip) |
| Env override | `GULIERP_MDM_SEED_FILE` (hard opt-out) |
| Path resolution | env > explicit param > walk-up from `AppContext.BaseDirectory` (8-hop cap) > walk-up from CWD |
| Filter | `seed_status == "SAFE_TO_SEED_SYSTEM"` |
| Idempotency | Skips if sentinel row already exists |
| Async signature | `Task SeedAsync(MdmDbContext, ILogger, seedFilePath, CancellationToken)` |
| Bounded | Development / Testing only; Production operator-controlled |

### 1.2 `MdmDictionaryService` (existing consumer)

`modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmDictionaryService.cs` (351 lines).

| Method | Purpose |
|---|---|
| `ListTypesAsync` / `GetTypeByIdAsync` | Read DictionaryType |
| `CreateTypeAsync` / `UpdateTypeAsync` / `ChangeTypeStatusAsync` | Write DictionaryType (rejects `IsSystem=true` writes via `EnsureNotSystem`) |
| `ListItemsAsync` / `GetItemByIdAsync` | Read DictionaryItem |
| `CreateItemAsync` / `UpdateItemAsync` / `ChangeItemStatusAsync` | Write DictionaryItem (rejects `IsSystem=true` writes) |
| Tenant scope | `RequireTenant()` raises if `ICurrentTenant` unresolved |
| Concurrency | `EnsureConcurrency(actual, expected, ...)` via `ConcurrencyVersion` |

### 1.3 `data/bootstrap/reference/tenant-template/` (existing curated data)

| File | Size | Items | Maps to brief dict | Notes |
|---|---:|---:|---|---|
| `business-partner-type.json` | 1.5 KB | 4 | 2 CUSTOMER_TYPE + 3 SUPPLIER_TYPE | CUST + SUP combined as BP type |
| `payment-method.json` | 2.2 KB | 5 | 6 PAYMENT_METHOD + 8 SETTLEMENT_METHOD | PM + PT mixed (per file's own note) |
| `position.json` | 2.3 KB | 4 | (not in brief) | Position (员工岗位), not state |

**Note**: The existing 3 JSON files cover brief dicts 2+3 (BP type) and 6+8 (payment method+settlement), but with **inconsistent naming** vs the 9 brief dicts. The plan re-shapes them into 9 distinct types.

### 1.4 `data/bootstrap/reference/system/` (existing curated data)

| File | Size | Notes |
|---|---:|---|
| `uom.json` | 10 KB | Used by `MdmSeed.cs` (13 SAFE_TO_SEED_SYSTEM items + 8 PROPOSED) |
| `country.json` | 1.4 KB | Not yet wired (deferred to future Goal) |
| `currency.json` | 9.3 KB | Not yet wired |
| `education.json` | 3.7 KB | Not yet wired |
| `ethnic-group.json` | 17 KB | Not yet wired |
| `semantic-data-type.json` | 13.7 KB | MDM-000D semantic types (not a Dictionary; different concern) |

### 1.5 Summary

- **Pattern established**: `MdmSeed.cs` JSON + sentinel + env-override + idempotent
- **Service ready**: `MdmDictionaryService` accepts `IsSystem=true` rows via the `MdmDbContext` (the `EnsureNotSystem` rejection is a **UI/admin API** guard, not a seed-time guard)
- **3 dictionary JSON files** already exist (curated, untracked) but need **re-shaping** for the 9 brief dicts
- **0 of the 9 brief dicts are seeded today**

---

## 2. TASK 2 — Dictionary V1 Seed Matrix (9 types × ~5 items each = ~45 items)

### 2.1 Master table

| # | DictionaryCode | Name (zh) | Name (en) | Items | Sentinel | File | Existing Coverage |
|---|---|---|---|---:|---|---|---|
| 1 | `DOC_STATUS` | 单据状态 | Document Status | 4 | `DS_DRAFT` | `system/dict-document-status.json` | NEW (currently hard-coded as `SalesOrderStatus` enum) |
| 2 | `CUST_TYPE` | 客户类型 | Customer Type | 4 | `CT_RETAIL` | `system/dict-customer-type.json` | PARTIAL (reuse `business-partner-type.json` BP_CUSTOMER + LOGISTICS) |
| 3 | `SUPP_TYPE` | 供应商类型 | Supplier Type | 4 | `ST_MANUFACTURER` | `system/dict-supplier-type.json` | PARTIAL (reuse `business-partner-type.json` BP_SUPPLIER + SUBCONTRACTOR) |
| 4 | `ITEM_STATUS` | 商品状态 | Item Status | 4 | `IS_ACTIVE` | `system/dict-item-status.json` | NEW (currently hard-coded as `MasterDataStatus` enum, 2 values) |
| 5 | `EMP_STATUS` | 员工状态 | Employee Status | 4 | `ES_ACTIVE` | `system/dict-employee-status.json` | NEW (currently no employee status field) |
| 6 | `PM_METHOD` | 付款方式 | Payment Method | 5 | `PM_CASH` | `system/dict-payment-method.json` | PARTIAL (reuse `payment-method.json` PM_PREPAID_DELIVERY + PM_COD) |
| 7 | `TM_MODE` | 运输方式 | Transport Mode | 6 | `TM_TRUCK` | `system/dict-transport-mode.json` | NEW |
| 8 | `SM_TERM` | 结算方式 | Settlement Term | 5 | `SM_NET_30` | `system/dict-settlement-term.json` | PARTIAL (reuse `payment-method.json` PT_NET_30/60/90) |
| 9 | `ENT_TYPE` | 企业类型 | Enterprise Type | 5 | `ET_CORPORATION` | `system/dict-enterprise-type.json` | NEW |
| **Total** | | | | **~41 items** | | 9 files | 3 partial / 6 new |

### 2.2 Dictionary #1: DOC_STATUS (Document Status)

| Code | Name (zh) | Name (en) | IsDefault | SortOrder | Maps from |
|---|---|---|:---:|---:|---|
| `DS_DRAFT` | 草稿 | Draft | **✓** | 1 | (V1 frozen `SalesOrderStatus.Draft`) |
| `DS_CONFIRMED` | 已确认 | Confirmed | | 2 | (V1 frozen `SalesOrderStatus.Confirmed`) |
| `DS_APPROVED` | 已审核 | Approved | | 3 | (new; needed for PO + REC) |
| `DS_CLOSED` | 已关闭 | Closed | | 4 | (new; needed for SO + PO closing flow) |
| `DS_CANCELLED` | 已取消 | Cancelled | | 5 | (V1 frozen `SalesOrderStatus.Cancelled`) |

**Sentinel**: `DS_DRAFT` (the first / default).

### 2.3 Dictionary #2: CUST_TYPE (Customer Type)

| Code | Name (zh) | Name (en) | IsDefault | SortOrder | Notes |
|---|---|---|:---:|---:|---|
| `CT_RETAIL` | 零售 | Retail | **✓** | 1 | end consumer (B2C) |
| `CT_WHOLESALE` | 批发 | Wholesale | | 2 | bulk buyer |
| `CT_DISTRIBUTOR` | 经销 | Distributor | | 3 | channel partner |
| `CT_ENTERPRISE` | 企业 | Enterprise (B2B) | | 4 | corporate buyer |

**Sentinel**: `CT_RETAIL`.

### 2.4 Dictionary #3: SUPP_TYPE (Supplier Type)

| Code | Name (zh) | Name (en) | IsDefault | SortOrder | Notes |
|---|---|---|:---:|---:|---|
| `ST_MANUFACTURER` | 生产商 | Manufacturer | **✓** | 1 | makes the goods |
| `ST_WHOLESALER` | 批发商 | Wholesaler | | 2 | bulk reseller |
| `ST_IMPORTER` | 进口商 | Importer | | 3 | cross-border |
| `ST_SERVICE` | 服务商 | Service Provider | | 4 | service-only (e.g., logistics) |

**Sentinel**: `ST_MANUFACTURER`.

### 2.5 Dictionary #4: ITEM_STATUS (Item Status)

| Code | Name (zh) | Name (en) | IsDefault | SortOrder | Notes |
|---|---|---|:---:|---:|---|
| `IS_ACTIVE` | 在用 | Active | **✓** | 1 | normal sale |
| `IS_INACTIVE` | 停用 | Inactive | | 2 | temporarily off-shelf |
| `IS_DISCONTINUED` | 停产 | Discontinued | | 3 | no longer produced |
| `IS_EOL` | 淘汰 | End-of-Life | | 4 | archived (catalog only) |

**Sentinel**: `IS_ACTIVE`.
**Note**: separate from `MasterDataStatus` (which is the system enum for ALL MDM entities, 2 values). `ITEM_STATUS` is the **business** status (4 values).

### 2.6 Dictionary #5: EMP_STATUS (Employee Status)

| Code | Name (zh) | Name (en) | IsDefault | SortOrder | Notes |
|---|---|---|:---:|---:|---|
| `ES_ACTIVE` | 在职 | Active | **✓** | 1 | normal employee |
| `ES_LEAVE` | 休假 | On Leave | | 2 | temporary absence (maternity / medical) |
| `ES_SUSPENDED` | 停薪留职 | Suspended (no pay) | | 3 | long-term absence (sabbatical) |
| `ES_TERMINATED` | 离职 | Terminated | | 4 | no longer employed |

**Sentinel**: `ES_ACTIVE`.
**Note**: brief calls this "员工状态" (status), distinct from "员工类型" (type). `Position` (员工岗位) is deferred to a future Goal (already has `position.json` data, not in this V1).

### 2.7 Dictionary #6: PM_METHOD (Payment Method)

| Code | Name (zh) | Name (en) | IsDefault | SortOrder | Notes |
|---|---|---|:---:|---:|---|
| `PM_CASH` | 现金 | Cash | **✓** | 1 | physical cash |
| `PM_BANK_TRANSFER` | 银行转账 | Bank Transfer | | 2 | wire / ACH |
| `PM_CHECK` | 支票 | Check | | 3 | paper check |
| `PM_CREDIT_CARD` | 信用卡 | Credit Card | | 4 | card payment |
| `PM_ONLINE` | 在线支付 | Online (Alipay / WeChat) | | 5 | digital wallet |

**Sentinel**: `PM_CASH`.
**Note**: split from `payment-method.json` `PM_*` (method only; `PT_*` for terms moved to `SM_TERM`).

### 2.8 Dictionary #7: TM_MODE (Transport Mode)

| Code | Name (zh) | Name (en) | IsDefault | SortOrder | Notes |
|---|---|---|:---:|---:|---|
| `TM_TRUCK` | 公路 | Truck | **✓** | 1 | road |
| `TM_RAIL` | 铁路 | Rail | | 2 | rail |
| `TM_AIR` | 航空 | Air | | 3 | air freight |
| `TM_SEA` | 海运 | Sea | | 4 | ocean |
| `TM_COURIER` | 快递 | Courier | | 5 | express (SF / YTO / etc.) |
| `TM_SELF_PICKUP` | 自提 | Self-pickup | | 6 | customer pickup |

**Sentinel**: `TM_TRUCK`.

### 2.9 Dictionary #8: SM_TERM (Settlement Term)

| Code | Name (zh) | Name (en) | IsDefault | SortOrder | term_days | Notes |
|---|---|---|:---:|---:|---:|---|
| `SM_NET_30` | 月结30天 | Net 30 | **✓** | 1 | 30 | pay within 30 days |
| `SM_NET_60` | 月结60天 | Net 60 | | 2 | 60 | pay within 60 days |
| `SM_NET_90` | 月结90天 | Net 90 | | 3 | 90 | pay within 90 days |
| `SM_COD` | 货到付款 | Cash on Delivery | | 4 | 0 | pay on receipt |
| `SM_PREPAY` | 预付 | Prepayment | | 5 | -1 | pay before shipment |

**Sentinel**: `SM_NET_30`.
**Note**: split from `payment-method.json` `PT_*` (term only; `PM_*` for methods moved to `PM_METHOD`).

### 2.10 Dictionary #9: ENT_TYPE (Enterprise Type)

| Code | Name (zh) | Name (en) | IsDefault | SortOrder | Notes |
|---|---|---|:---:|---:|---|
| `ET_SOLE_PROP` | 个人独资企业 | Sole Proprietorship | | 1 | individual owner |
| `ET_PARTNERSHIP` | 合伙企业 | Partnership | | 2 | 2+ partners |
| `ET_LLC` | 有限责任公司 | Limited Liability Company | **✓** | 3 | most common in CN |
| `ET_CORPORATION` | 股份有限公司 | Corporation (Joint-stock) | | 4 | publicly listed |
| `ET_OTHER` | 其他 | Other | | 5 | state-owned / foreign / etc. |

**Sentinel**: `ET_LLC` (most common in CN).

### 2.11 Total item count

| Dictionary | Items |
|---|---:|
| DOC_STATUS | 5 |
| CUST_TYPE | 4 |
| SUPP_TYPE | 4 |
| ITEM_STATUS | 4 |
| EMP_STATUS | 4 |
| PM_METHOD | 5 |
| TM_MODE | 6 |
| SM_TERM | 5 |
| ENT_TYPE | 5 |
| **TOTAL** | **42** |

Each type has 1 `IsDefault=true` item (the first row). All 42 items
are `IsSystem=true` (platform-seeded) and `Status=Active`.

---

## 3. TASK 3 — JSON Seed Structure (idempotent + env-override + sentinel)

### 3.1 File path convention

All 9 system dicts follow the same path pattern:

```
data/bootstrap/reference/system/dict-<slug>.json
```

| Dictionary | File | Slug |
|---|---|---|
| DOC_STATUS | `dict-document-status.json` | `document-status` |
| CUST_TYPE | `dict-customer-type.json` | `customer-type` |
| SUPP_TYPE | `dict-supplier-type.json` | `supplier-type` |
| ITEM_STATUS | `dict-item-status.json` | `item-status` |
| EMP_STATUS | `dict-employee-status.json` | `employee-status` |
| PM_METHOD | `dict-payment-method.json` | `payment-method` |
| TM_MODE | `dict-transport-mode.json` | `transport-mode` |
| SM_TERM | `dict-settlement-term.json` | `settlement-term` |
| ENT_TYPE | `dict-enterprise-type.json` | `enterprise-type` |

All 9 files are committed to git (per `GULIERP_BOOTSTRAP_REFERENCE_DATA_001`
recommendation; existing 3 `tenant-template/*.json` files should be
**renamed / merged** into the 9 system files in this Goal).

### 3.2 JSON schema (v1)

Reuse the existing `data/bootstrap/reference/tenant-template/*.json`
schema with extensions. One JSON file per dictionary type:

```json
{
  "meta": {
    "dictionary_type_code": "DOC_STATUS",
    "source_systems": [
      {
        "system": "DEV",
        "source_name": "单据状态"
      }
    ],
    "extraction_time": "2026-08-25T20:00:00+08:00",
    "classification": "SAFE_TO_SEED_SYSTEM",
    "seed_status_at_file_level": "SAFE_TO_SEED_SYSTEM",
    "is_system": true,
    "sentinel_item_code": "DS_DRAFT",
    "item_count": 5,
    "notes": [
      "V1 frozen: maps to SalesOrderStatus + extends to PO + REC + Approval flow.",
      "Replaces the hard-coded SalesOrderStatus enum (per G3_MDM_FOUNDATION_AUDIT_001 D2)."
    ]
  },
  "items": [
    {
      "canonical_code": "DS_DRAFT",
      "canonical_name_zh": "草稿",
      "canonical_name_en": "Draft",
      "is_default": true,
      "sort_order": 1,
      "term_days": null,
      "source_systems": [
        { "system": "DEV", "source_name": "草稿" }
      ],
      "seed_status": "SAFE_TO_SEED_SYSTEM"
    },
    {
      "canonical_code": "DS_CONFIRMED",
      "canonical_name_zh": "已确认",
      "canonical_name_en": "Confirmed",
      "is_default": false,
      "sort_order": 2,
      "term_days": null,
      "source_systems": [
        { "system": "DEV", "source_name": "已确认" }
      ],
      "seed_status": "SAFE_TO_SEED_SYSTEM"
    },
    {
      "canonical_code": "DS_APPROVED",
      "canonical_name_zh": "已审核",
      "canonical_name_en": "Approved",
      "is_default": false,
      "sort_order": 3,
      "term_days": null,
      "source_systems": [
        { "system": "DEV", "source_name": "已审核" }
      ],
      "seed_status": "SAFE_TO_SEED_SYSTEM"
    },
    {
      "canonical_code": "DS_CLOSED",
      "canonical_name_zh": "已关闭",
      "canonical_name_en": "Closed",
      "is_default": false,
      "sort_order": 4,
      "term_days": null,
      "source_systems": [
        { "system": "DEV", "source_name": "已关闭" }
      ],
      "seed_status": "SAFE_TO_SEED_SYSTEM"
    },
    {
      "canonical_code": "DS_CANCELLED",
      "canonical_name_zh": "已取消",
      "canonical_name_en": "Cancelled",
      "is_default": false,
      "sort_order": 5,
      "term_days": null,
      "source_systems": [
        { "system": "DEV", "source_name": "已取消" }
      ],
      "seed_status": "SAFE_TO_SEED_SYSTEM"
    }
  ]
}
```

### 3.3 Schema fields

**Top-level `meta`**:

| Field | Type | Required | Notes |
|---|---|:---:|---|
| `dictionary_type_code` | string (UPPER_SNAKE) | ✓ | Matches `DictionaryType.Code` |
| `source_systems` | array<object> | | Provenance (DEV / external) |
| `extraction_time` | ISO 8601 string | ✓ | When the data was extracted |
| `classification` | enum string | ✓ | `SAFE_TO_SEED_SYSTEM` / `SAFE_TO_SEED_TENANT_TEMPLATE` / `PROPOSED` |
| `seed_status_at_file_level` | enum string | ✓ | Same enum as `classification` (file-level) |
| `is_system` | bool | ✓ | All 9 are `true` (platform-owned) |
| `sentinel_item_code` | string | ✓ | The first item's `canonical_code`; idempotent skip |
| `item_count` | int | ✓ | Must equal `items.length` |
| `notes` | array<string> | | Free text |

**Per-item**:

| Field | Type | Required | Maps to | Notes |
|---|---|:---:|---|---|
| `canonical_code` | string (UPPER_SNAKE) | ✓ | `DictionaryItem.Code` | Unique per (Tenant, DictionaryType) |
| `canonical_name_zh` | string (zh-CN) | ✓ | `DictionaryItem.Name` | Primary display name |
| `canonical_name_en` | string (en) | | `DictionaryItem.Description`? | Optional; future i18n |
| `is_default` | bool | | `DictionaryItem.IsDefault` | One per type |
| `sort_order` | int | | `DictionaryItem.SortOrder` | 1-based |
| `term_days` | int? | | (not in V1 entity) | For `SM_TERM` only; 0 = COD, -1 = PREPAY |
| `source_systems` | array<object> | ✓ | (audit) | Provenance |
| `seed_status` | enum string | ✓ | (filter) | `SAFE_TO_SEED_SYSTEM` is the only auto-seeded value |

### 3.4 Idempotency pattern (per dictionary type)

Per `MdmSeed.cs` precedent:

1. **Env override** (hard opt-out): `GULIERP_MDM_DICT_SEED_FILE_<TYPE>` (e.g.,
   `GULIERP_MDM_DICT_SEED_FILE_DOC_STATUS`)
2. **Explicit path** parameter
3. **Walk-up** from `AppContext.BaseDirectory` (8-hop cap) to find the
   default `data/bootstrap/reference/system/dict-<slug>.json`
4. **Walk-up** from `Environment.CurrentDirectory`

**Sentinel check**: if `mdm.gulierp_dictionary_item` already contains
`canonical_code = sentinel_item_code` for the same
`dictionary_type_code` AND `tenant_id = current`, skip the entire
type (do not re-seed).

### 3.5 Seed-time guard vs. API-time guard

| Layer | `IsSystem=true` writeable? | Reason |
|---|:---:|---|
| `MdmDictionaryService` (admin API) | ❌ NO (rejected by `EnsureNotSystem`) | Operator cannot tamper with system dicts at runtime |
| `MdmDictionarySeed.cs` (this Goal's seeder) | ✅ YES (writes via `MdmDbContext.Add` + `SaveChangesAsync`, bypassing service) | Platform bootstrap; not a user action |

**Code path**:
- Service layer: operator calls `POST /api/v1/mdm/dictionary/types` → rejected
- Seed layer: `MdmDictionarySeed.SeedAsync` writes directly to `MdmDbContext.DictionaryTypes` and `.DictionaryItems` → succeeds

This is the same pattern as `MdmSeed.cs` (UoM seed bypasses
`MdmService.CreateUomAsync`).

### 3.6 Tenant-scope decision (PLANNING — requires user confirmation)

Two valid approaches for system dictionary seed:

| Approach | Tenant scope | Pros | Cons |
|---|---|---|---|
| **A. Per-tenant seed** (like UoM) | Run on every tenant bootstrap; each tenant gets its own system dict rows | Tenants can disable / customize per-tenant | Higher DB row count (9 dicts × 42 items × N tenants) |
| **B. Global seed** (system-scoped) | System dicts are SHARED across all tenants (no TenantId on DictionaryType / DictionaryItem for system rows) | Lower row count; one source of truth | Tenants cannot customize (which may be required for global enterprise deployments) |

**Recommendation**: **Approach A (per-tenant, like UoM)** because:
1. UoM is also per-tenant (V1) and the pattern is already established
2. The `IMultiTenant` interface is on `DictionaryType` and `DictionaryItem`
3. Tenants may want to add their own items (e.g., a tenant may
   add `CT_DISTRIBUTOR` with a Chinese-specific name)
4. The seed runs on each tenant's first bootstrap; the sentinel
   makes it idempotent on subsequent runs

**Note**: This decision should be confirmed by the user before B1
implementation. If user prefers **Approach B (global)**, the schema
change requires adding a `IsGlobal` flag to `DictionaryType` and
`DictionaryItem` (and a migration). For now, this Plan assumes
**Approach A** (no schema change).

### 3.7 Production safety

Per `MdmSeed.cs` design:

> The seed is bounded to Development / Testing environments.
> Production must not auto-seed master data.

`MdmDictionarySeed.cs` follows the same convention:
- Auto-run on `app.Run()` via `IHostedService` in **Development** env
  (`app.Environment.IsDevelopment()`)
- **NOT** auto-run in Production; the operator runs the seed
  explicitly via a maintenance tool (or via direct SQL — which is
  the only allowed non-API write path for the operator)
- The seed is **never destructive**: it only `Add`s rows; it never
  `Update`s or `Delete`s

### 3.8 Test isolation

Integration tests should use a **separate sentinel** to avoid
colliding with the dev seed:

```csharp
// In test setup
db.Database.ExecuteSqlRaw(
  "INSERT INTO mdm.gulierp_dictionary_type (..., IsSystem=true) " +
  "VALUES (..., 'DICT_TEST_DOC_STATUS', ...) " +
  "ON CONFLICT (TenantId, Code) DO NOTHING");
```

Or use a separate test-only JSON file (`dict-document-status.test.json`).

---

## 4. TASK 4 — Implementation Plan (B1 / B2 / B3)

This Goal is structured as **3 sub-Goals (B1, B2, B3)** to keep
each commit small and reviewable.

### 4.1 B1: Backend (Dictionary Seed Runner)

**Goal ID**: `G3_MDM_DICTIONARY_V1_SEED_B1_BACKEND_001`
**Size**: ~3 days
**Owner**: Codex (executor)
**Review**: MiniMax (audit)
**Approver**: Human (commit)

#### B1.1 New files

| Path | Type | Size | Purpose |
|---|---|---:|---|
| `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/IDictionarySeedRunner.cs` | interface | ~30 lines | `Task SeedAsync(MdmDbContext, ILogger, CancellationToken)`; per-dictionary runner |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmDictionarySeed.cs` | static class | ~200 lines | The runner implementation; reads 9 JSON files, applies idempotency, writes via `MdmDbContext` |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/DictionarySeedHostedService.cs` | class (implements `IHostedService`) | ~50 lines | Auto-runs `MdmDictionarySeed.SeedAsync` on `app.Run()` in Dev only |
| `modules/mdm/GuliERP.Mdm.Application/IMdmDictionarySeedService.cs` | interface | ~20 lines | `Task SeedAllAsync(CancellationToken)`; for explicit / production invocation |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmDictionarySeedService.cs` | class | ~40 lines | Implements `IMdmDictionarySeedService`; explicit / production entry point |
| `modules/mdm/GuliERP.Mdm.Infrastructure/DependencyInjection.cs` | (modified) | +10 lines | Register `IMdmDictionarySeedService` (singleton) + `DictionarySeedHostedService` (in Dev only) |
| `modules/mdm/GuliERP.Mdm.Application/MdmErrorCodes.cs` | (modified) | +3 lines | Add `DictionarySeedFileNotFound`, `DictionarySeedSentinelMismatch`, `DictionarySeedSystemProtected` |

#### B1.2 Modified files (no breaking changes)

| Path | Change |
|---|---|
| `apps/api/GuliERP.Api/Program.cs` | Register `IMdmDictionarySeedService` and `DictionarySeedHostedService` (Dev only) |
| `tests/GuliERP.Mdm.Tests/MdmDictionarySeedFacts.cs` | (new test) ~15 unit tests |
| `tests/GuliERP.Mdm.IntegrationTests/DictionarySeedIntegrationFacts.cs` | (new test) ~5 integration tests (Operator PG only) |

#### B1.3 Idempotency contract

```csharp
public interface IDictionarySeedRunner
{
    /// <summary>Dictionary type code, e.g. "DOC_STATUS".</summary>
    string DictionaryTypeCode { get; }

    /// <summary>Sentinel item code; if present, seed is skipped.</summary>
    string SentinelItemCode { get; }

    /// <summary>Default JSON file path, relative to repo root.</summary>
    string DefaultJsonFilePath { get; }

    /// <summary>
    /// Seeds one dictionary type. Idempotent:
    /// if sentinel item exists for the current tenant, skip.
    /// </summary>
    Task SeedAsync(MdmDbContext db, ILogger logger, CancellationToken ct);
}
```

#### B1.4 Test plan

| Test | Type | What it verifies |
|---|---|---|
| `SeedAsync_WhenSentinelMissing_CreatesAllItems` | Unit (in-memory) | 5 items created in `DictionaryItem` for `DOC_STATUS` |
| `SeedAsync_WhenSentinelExists_Skips` | Unit | No items created; no exception |
| `SeedAsync_WhenFileNotFound_LogsWarning` | Unit | Logs warning; no exception |
| `SeedAsync_WhenEnvOverrideSet_UsesOverride` | Unit | Reads from env-var path |
| `SeedAsync_WithInvalidJson_Throws` | Unit | Throws `MdmValidationException` |
| `SeedAsync_WithNonSafeToSeedSystemItem_SkipsItem` | Unit | Item with `seed_status=PROPOSED` is skipped |
| `EnsureNotSystem_StillProtectsAtAdminAPI` | Unit | `IMdmDictionaryService.UpdateTypeAsync` rejects system records |
| `SeedAsync_Tenants_Are_Isolated` | Integration (PG) | Tenant A's seed does not leak to Tenant B |
| `SeedAsync_RunsOnlyInDevelopment` | Unit (env var) | Hosted service does not run when `ASPNETCORE_ENVIRONMENT=Production` |
| `SeedAsync_Idempotency_Across100Calls` | Integration | 100 sequential calls create exactly 5 items (not 500) |

#### B1.5 Compliance with brief

- ✅ No new migration (existing `20260825014004_AddMdmDictionaryTypesAndItems` suffices)
- ✅ No V1 frozen contract modification
- ✅ All writes go through `MdmDbContext` (no direct SQL)
- ✅ Bounded to Development / Testing environments
- ✅ Idempotent via sentinel
- ✅ Env-overridable per dictionary type
- ✅ Walk-up path resolution (8-hop cap)
- ✅ Reuses `MdmDictionaryService` patterns (tenant scope, concurrency)

#### B1.6 Commit

```
feat(mdm): add V1 dictionary seed runner (B1 backend)
```

### 4.2 B2: Seed Data (this Plan, 0 code)

**Goal ID**: `G3_MDM_DICTIONARY_V1_SEED_B2_DATA_001` (this Goal's B2)
**Size**: ~2 hours (data entry, no code)
**Owner**: MiniMax (audit) + Codex (apply)
**Approver**: Human (commit)

#### B2.1 New files (data only)

| Path | Type | Size | Items |
|---|---|---:|---:|
| `data/bootstrap/reference/system/dict-document-status.json` | JSON | ~3 KB | 5 |
| `data/bootstrap/reference/system/dict-customer-type.json` | JSON | ~2 KB | 4 |
| `data/bootstrap/reference/system/dict-supplier-type.json` | JSON | ~2 KB | 4 |
| `data/bootstrap/reference/system/dict-item-status.json` | JSON | ~2 KB | 4 |
| `data/bootstrap/reference/system/dict-employee-status.json` | JSON | ~2 KB | 4 |
| `data/bootstrap/reference/system/dict-payment-method.json` | JSON | ~3 KB | 5 |
| `data/bootstrap/reference/system/dict-transport-mode.json` | JSON | ~3 KB | 6 |
| `data/bootstrap/reference/system/dict-settlement-term.json` | JSON | ~3 KB | 5 |
| `data/bootstrap/reference/system/dict-enterprise-type.json` | JSON | ~3 KB | 5 |
| `data/bootstrap/reference/tenant-template/business-partner-type.json` | (deprecated) | — | merged into CUST_TYPE + SUPP_TYPE |
| `data/bootstrap/reference/tenant-template/payment-method.json` | (deprecated) | — | split into PM_METHOD + SM_TERM |
| `data/bootstrap/reference/tenant-template/position.json` | (moved to `system/dict-position.json`) | — | deferred (not in this V1) |

**Note**: The 3 existing `tenant-template/*.json` files are
**deprecated** in B2 (merged or split). The deprecation is
**file-level only** (no breaking change to the consuming code,
since none exists yet — the B1 runner reads the 9 new system files).

#### B2.2 Decision: existing JSON files

| Existing file | Action | Rationale |
|---|---|---|
| `tenant-template/business-partner-type.json` | Split into `CUST_TYPE` (CUSTOMER + LOGISTICS) + `SUPP_TYPE` (SUPPLIER + SUBCONTRACTOR); delete original | The brief's 9 dicts distinguish CUST vs SUPP; mixing them in one file is anti-pattern |
| `tenant-template/payment-method.json` | Split into `PM_METHOD` (PM_*) + `SM_TERM` (PT_*); delete original | Per file's own note (line 14-15: "PaymentMethod (channel) and PaymentTerm (duration/aging) need to be split") |
| `tenant-template/position.json` | Move to `system/dict-position.json` (NOT in this V1; deferred to a future Goal) | Position is "员工岗位" (job position), not "员工状态" (employee status); brief does not include Position in the 9 dicts |
| `system/uom.json` | Keep (already used by `MdmSeed.cs`) | — |
| `system/{country,currency,education,ethnic-group}.json` | Keep (deferred to future Goals) | — |
| `system/semantic-data-type.json` | Keep (not a Dictionary; MDM-000D semantic types) | — |

#### B2.3 Commit

```
chore(mdm): add V1 system dictionary seed JSON (B2 data)
```

### 4.3 B3: Verification (Operator + MiniMax)

**Goal ID**: `G3_MDM_DICTIONARY_V1_SEED_B3_VERIFY_001`
**Size**: ~1 day
**Owner**: MiniMax (audit) + Human (Operator PG)
**Approver**: Human (commit)

#### B3.1 Operator runtime evidence (per `G2_DOCNO_002_B3_RUNTIME_FINAL_REPORT` pattern)

```powershell
cd D:\guli\projects\gulierp-next

# 1. Start API
$env:PGPASSWORD = "gulidata123"
$env:ConnectionStrings__GuliERP = "Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulidata;Password=gulidata123;Include Error Detail=true;Pooling=false"
$env:ASPNETCORE_ENVIRONMENT = "Development"

# 2. Run unit tests
dotnet test tests/GuliERP.Mdm.Tests/GuliERP.Mdm.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~MdmDictionarySeedFacts"

# 3. Run integration tests (Operator PG)
dotnet test tests/GuliERP.Mdm.IntegrationTests/GuliERP.Mdm.IntegrationTests.csproj -c Release --no-build --filter "FullyQualifiedName~DictionarySeedIntegrationFacts"

# 4. Verify DB state (GULI tenant)
psql -c "SELECT t.\"Code\", t.\"Name\", t.\"IsSystem\" FROM mdm.gulierp_dictionary_type t WHERE t.\"TenantId\" = 83727350616817890 ORDER BY t.\"Code\";"
# Expected: 9 rows (DOC_STATUS / CUST_TYPE / SUPP_TYPE / ITEM_STATUS / EMP_STATUS / PM_METHOD / TM_MODE / SM_TERM / ENT_TYPE), all IsSystem=true

psql -c "SELECT t.\"Code\" AS Type, COUNT(i.\"Id\") AS Items FROM mdm.gulierp_dictionary_type t LEFT JOIN mdm.gulierp_dictionary_item i ON i.\"DictionaryTypeId\" = t.\"Id\" WHERE t.\"TenantId\" = 83727350616817890 GROUP BY t.\"Code\" ORDER BY t.\"Code\";"
# Expected: 9 rows, total items = 42 (5+4+4+4+4+5+6+5+5)

# 5. Verify idempotency: re-run, expect 0 new rows
# (re-trigger app startup or call MdmDictionarySeedService.SeedAllAsync() manually)

# 6. Verify operator cannot update system dicts
curl -X PUT http://127.0.0.1:5000/api/v1/mdm/dictionary/types/<DS_DRAFT_id> -H "..." -d "{...}"
# Expected: 403 (system-protected)
```

#### B3.2 MiniMax acceptance criteria

| # | Criterion | Measurable |
|---|---|---|
| 1 | Build | `dotnet build GuliERP.slnx -c Release --no-restore` exit 0; 0 errors, 0 warnings |
| 2 | Unit tests | `MdmDictionarySeedFacts` ≥ 10/10 PASS |
| 3 | Integration tests | `DictionarySeedIntegrationFacts` ≥ 5/5 PASS (Operator PG) |
| 4 | No regression | Foundation 44/44 + Identity 84/84 + MDM 221/221 + Sales 9/9 + Bootstrap 64/64 all unchanged from baseline |
| 5 | DB state | 9 DictionaryType + 42 DictionaryItem in `mdm` schema for GULI tenant |
| 6 | Idempotency | 2nd seed run = 0 new rows |
| 7 | System protection | PUT /api/v1/mdm/dictionary/types/{DS_DRAFT_id} → 403 |
| 8 | Production safety | ASPNETCORE_ENVIRONMENT=Production → HostedService does NOT run (verified by checking that seed did not auto-execute) |
| 9 | Tenant isolation | Tenant A's seed does not leak to Tenant B |
| 10 | Honest disclosure | Verification report includes: any TODO / known limitation / deferred work |

**Final Gate**: `G3_MDM_DICTIONARY_V1_SEED_VERIFIED`

#### B3.3 Verification report

`docs/verification/G3_MDM_DICTIONARY_V1_SEED_VERIFICATION_REPORT.md` (template):

- Test results (numbers)
- Build status
- DB state (9 types + 42 items)
- Idempotency proof
- System protection proof
- Production safety proof
- Honest disclosures
- Final Gate
- Recommended next action (commit + push; open next Goal in the G3 roadmap, e.g., `G3_MDM_MASTERDATA_V1_SEED_001`)

#### B3.4 Commit

```
test(mdm): add dictionary V1 seed verification (B3 verify)
```

### 4.4 Total Goal count (B1 + B2 + B3)

| Sub-Goal | Size | Owner | Commit |
|---|---|---|---|
| B1 Backend | ~3 days | Codex + MiniMax | `feat(mdm): add V1 dictionary seed runner` |
| B2 Data | ~2 hours | MiniMax + Codex | `chore(mdm): add V1 system dictionary seed JSON` |
| B3 Verify | ~1 day | MiniMax + Human | `test(mdm): add dictionary V1 seed verification` |
| **Total** | **~4-5 days** | — | **3 atomic commits** |

### 4.5 Dependencies

```
B1 (Backend) ─┐
              ├─→ B3 (Verify) ─→ Final Gate
B2 (Data)   ─┘
```

B1 and B2 are **independent** (can run in parallel).
B3 depends on both B1 (executable) and B2 (data to seed).

### 4.6 Cross-Goal links

- Depends on: `G3_MDM_FOUNDATION_AUDIT_001` (D1 + D2 + D3 in §6 priority ranking) — CLOSED at HEAD `9684985`
- Enables: `G3_MDM_MASTERDATA_V1_SEED_001` (HIGH #2; needs DOC_STATUS to set `Item.Status`)
- Enables: `G3_MDM_DOCUMENT_TYPE_PAY_REC_001` (MEDIUM #4; needs PM_METHOD + SM_TERM to seed PaymentVoucher profiles)
- Enables: `G3_MDM_BUSINESSPARTNER_VENDOR_001` (MEDIUM #5; needs CUST_TYPE + SUPP_TYPE to seed Vendor profile)
- Enables: `G3_MDM_ITEM_PRICING_001` (MEDIUM #6; needs ITEM_STATUS)
- Enables: `G3_MDM_EMPLOYEE_PROFILE_001` (LOW #8; needs EMP_STATUS)

---

## 5. Files Affected Summary

### 5.1 New files (15)

| Path | Type | Lines | Status |
|---|---|---:|---|
| `data/bootstrap/reference/system/dict-document-status.json` | JSON | ~80 | NEW (B2) |
| `data/bootstrap/reference/system/dict-customer-type.json` | JSON | ~60 | NEW (B2) |
| `data/bootstrap/reference/system/dict-supplier-type.json` | JSON | ~60 | NEW (B2) |
| `data/bootstrap/reference/system/dict-item-status.json` | JSON | ~60 | NEW (B2) |
| `data/bootstrap/reference/system/dict-employee-status.json` | JSON | ~60 | NEW (B2) |
| `data/bootstrap/reference/system/dict-payment-method.json` | JSON | ~70 | NEW (B2) |
| `data/bootstrap/reference/system/dict-transport-mode.json` | JSON | ~80 | NEW (B2) |
| `data/bootstrap/reference/system/dict-settlement-term.json` | JSON | ~70 | NEW (B2) |
| `data/bootstrap/reference/system/dict-enterprise-type.json` | JSON | ~70 | NEW (B2) |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/IDictionarySeedRunner.cs` | interface | ~30 | NEW (B1) |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmDictionarySeed.cs` | static | ~200 | NEW (B1) |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/DictionarySeedHostedService.cs` | class | ~50 | NEW (B1) |
| `modules/mdm/GuliERP.Mdm.Application/IMdmDictionarySeedService.cs` | interface | ~20 | NEW (B1) |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmDictionarySeedService.cs` | class | ~40 | NEW (B1) |
| `tests/GuliERP.Mdm.Tests/MdmDictionarySeedFacts.cs` | test | ~200 | NEW (B1) |
| `tests/GuliERP.Mdm.IntegrationTests/DictionarySeedIntegrationFacts.cs` | test | ~100 | NEW (B3) |
| `docs/verification/G3_MDM_DICTIONARY_V1_SEED_VERIFICATION_REPORT.md` | doc | ~25 KB | NEW (B3) |

**Total**: 17 new files, ~1,300 lines of code + JSON + docs.

### 5.2 Modified files (4)

| Path | Change |
|---|---|
| `modules/mdm/GuliERP.Mdm.Infrastructure/DependencyInjection.cs` | Register seed service + hosted service (+10 lines) |
| `modules/mdm/GuliERP.Mdm.Application/MdmErrorCodes.cs` | Add 3 error codes (+3 lines) |
| `apps/api/GuliERP.Api/Program.cs` | Register seed service + hosted service (+5 lines) |
| `data/bootstrap/reference/tenant-template/business-partner-type.json` | **DELETED** in B2 (split into CUST_TYPE + SUPP_TYPE) |
| `data/bootstrap/reference/tenant-template/payment-method.json` | **DELETED** in B2 (split into PM_METHOD + SM_TERM) |
| `data/bootstrap/reference/tenant-template/position.json` | **MOVED** to `system/dict-position.json` (deferred) |

### 5.3 NOT modified (per brief)

- ❌ `modules/document-kernel/` — no change
- ❌ `modules/sales/` — no change
- ❌ `modules/identity/` — no change
- ❌ `modules/foundation/` — no change
- ❌ Existing migrations (no new migration)
- ❌ Existing tests (only new tests added)
- ❌ DB schema (no migration; existing `20260825014004_AddMdmDictionaryTypesAndItems` is sufficient)

---

## 6. Compliance with brief

| Brief principle | Compliance |
|---|---|
| 不修改代码 | ✅ B2 (this Plan) has 0 code change; B1 + B3 in sub-Goals |
| 不修改数据库 | ✅ All writes via `MdmDbContext.Add` + `SaveChangesAsync`; no `Database.ExecuteSqlRaw` |
| 不添加 migration | ✅ Existing `20260825014004_AddMdmDictionaryTypesAndItems` is sufficient |
| commit | ✅ 0 commits (this Plan) |
| push | ✅ 0 pushes (this Plan) |
| 输出 `docs/planning/G3_MDM_DICTIONARY_V1_SEED_PLAN.md` | ✅ Written |
| 9 system dicts (DOC_STATUS, CUST_TYPE, SUPP_TYPE, ITEM_STATUS, EMP_STATUS, PM_METHOD, TM_MODE, SM_TERM, ENT_TYPE) | ✅ All 9 designed with full items |
| Each dict has DictionaryCode / Name / Items | ✅ §2.2-2.10 |
| 保持现有 MdmSeed.cs 模式 | ✅ §3.4-3.5 |
| JSON 结构支持: 幂等 / 环境覆盖 / sentinel | ✅ §3.4 (sentinel) + §3.4 (env override) + §3.4 (walk-up) + §3.5 (idempotency contract) |
| 实施计划 B1/B2/B3 | ✅ §4 |
| 最终输出 `G3_MDM_DICTIONARY_V1_SEED_READY` | ✅ §7 Sign-off |

---

## 7. Honest Disclosures

### 7.1 What this Plan does NOT do

- ❌ Does not write any code (B1 sub-Goal)
- ❌ Does not create the 9 JSON files (B2 sub-Goal)
- ❌ Does not run any test (B3 sub-Goal)
- ❌ Does not modify the existing `MdmSeed.cs` (only extends the pattern in a new `MdmDictionarySeed.cs`)
- ❌ Does not query the live DB (the empty state is based on prior `GULIERP_NEXT_OPERATOR_RUNTIME_VERIFICATION_001_REPORT.md`)
- ❌ Does not commit / push (per brief)
- ❌ Does not execute B1 / B2 / B3 — those are sub-Goals the user must ratify and execute

### 7.2 What's verified

- ✅ 9 dicts designed with full Code / Name (zh + en) / Items
- ✅ 42 items designed (5 + 4 + 4 + 4 + 4 + 5 + 6 + 5 + 5)
- ✅ JSON schema designed (v1) reusing the existing `tenant-template/*.json` shape
- ✅ Idempotency + env-override + sentinel pattern designed (extending `MdmSeed.cs`)
- ✅ B1 / B2 / B3 sub-Goals designed with file lists, test plans, commit messages
- ✅ 9 system dict types and 42 items align with the brief's 9 must-have types

### 7.3 What's NOT verified (deferred)

- ⚠️ The actual JSON file sizes (estimated 60-80 lines per file, ~2-3 KB)
- ⚠️ The actual B1 code line counts (estimated 200-300 lines for `MdmDictionarySeed.cs`)
- ⚠️ The actual test line counts (estimated 100-200 lines per test file)
- ⚠️ The runtime behavior of `MdmDictionarySeed.cs` against the JSON files (no actual run in this Plan)
- ⚠️ The actual DB state of `mdm.gulierp_dictionary_type` / `mdm.gulierp_dictionary_item` (assumed empty based on prior reports)
- ⚠️ The system-dict vs business-dict tenant scope decision (§3.6) — user must ratify

### 7.4 Open questions for user

1. **§3.6 Tenant scope**: Approach A (per-tenant, like UoM) is recommended. User should ratify or request Approach B (global, requires schema change + migration).
2. **§3.5 Idempotency**: Sentinel-based skip. User should ratify or request an alternative (e.g., version-stamped re-seed).
3. **§4.2.2 Existing JSON file disposition**: 3 existing `tenant-template/*.json` files are deprecated in B2 (split into the 9 system files). User should ratify or request preserving them as historical references.
4. **§2.5 Sentinel choices**: 9 sentinels chosen (one per type, the first/default item). User should ratify or request different sentinels.
5. **§2.7-2.10 `term_days` field**: Used for `SM_TERM` only (5 values: 30 / 60 / 90 / 0 / -1). The `DictionaryItem` entity does NOT have a `term_days` field; B1 would need to either (a) extend the entity + migration, or (b) store `term_days` in the `Description` field as JSON, or (c) skip this field for V1. Recommendation: **option (c)** skip for V1, defer to a future Goal.

### 7.5 Limits of this Plan

- This Plan is a **design only** document
- The actual B1 / B2 / B3 execution requires separate Goals with
  separate plans and commit messages
- The runtime evidence (Operator PG) is in B3, not in this Plan
- The system dict vs business dict distinction is conceptual;
  the V1 `IsSystem=true` flag is the only technical control
- The 9 dict names (DOC_STATUS, CUST_TYPE, etc.) are **proposed**;
  the user may rename to match tenant-specific conventions

---

## 8. Sign-off

**Gate**: `G3_MDM_DICTIONARY_V1_SEED_READY` — proposal stage

- ✅ TASK 1 — Current seed mechanism (MdmSeed.cs pattern + 3 existing tenant-template JSONs)
- ✅ TASK 2 — 9 system dictionaries + 42 items designed (DOC_STATUS / CUST_TYPE / SUPP_TYPE / ITEM_STATUS / EMP_STATUS / PM_METHOD / TM_MODE / SM_TERM / ENT_TYPE)
- ✅ TASK 3 — JSON seed schema v1 designed (idempotent / env-override / sentinel / walk-up)
- ✅ TASK 4 — B1 (Backend) / B2 (Data) / B3 (Verify) implementation plan with file lists, test plans, commit messages
- ✅ 9 dicts × ~5 items each = 42 items total
- ✅ 3 existing `tenant-template/*.json` files dispositioned (split / move / deprecate)
- ✅ Idempotency + env-override + sentinel pattern designed (extends `MdmSeed.cs`)
- ✅ No migration needed (existing `20260825014004_AddMdmDictionaryTypesAndItems` is sufficient)
- ✅ No V1 frozen contract modification
- ✅ Per-tenant vs global seed decision flagged for user ratification (§3.6)
- ✅ Cross-Goal links identified (5 downstream Goals unblocked)
- ✅ NO source / DB / migration change (per brief)
- ✅ NO commit / push (per brief)

**Author**: Mavis (M3 / mavis), acting as GuliERP AI Factory Planning + Review Agent
**Date**: 2026-08-25 (Asia/Shanghai)
**Status**: `G3_MDM_DICTIONARY_V1_SEED_READY` — proposal awaiting user ratification
**Next user action**: ratify the plan (especially §3.6 tenant scope), then open B1 sub-Goal
