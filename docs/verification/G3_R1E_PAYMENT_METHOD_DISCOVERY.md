# G3-R1E PaymentMethod Discovery

**Phase:** G3-R1E PaymentMethod + Master Data Write-path Runtime Acceptance
**Discovery date:** 2026-08-26
**Author:** Mavis (GuliERP-Next main execution Agent)
**Source commit (HEAD at discovery time):** `3a504ca docs(verification): add G3 R1D workbench runtime report`

---

## 1. Question-by-question answer

### Q1. Does PaymentMethod already have an entity in the system?

**Answer: NO standalone entity, YES first-class DictionaryType.**

PaymentMethod is a **V1 system DictionaryType** (code = `PM_METHOD`, name = "付款方式") declared in `modules/mdm/GuliERP.Mdm.Application/DictionarySeedDescriptorRegistry.cs` (line 53-57):

```csharp
new DictionarySeedDescriptor
{
    DictionaryTypeCode = "PM_METHOD",
    Name               = "付款方式",
},
```

It is the **6th of 9 V1 system dictionaries** (DOC_STATUS, CUST_TYPE, SUPP_TYPE, ITEM_STATUS, EMP_STATUS, **PM_METHOD**, TM_MODE, SM_TERM, ENT_TYPE). The 9-V1 set is FROZEN at the B1 plan.

No separate `PaymentMethod` entity, no `PaymentMethod` table, no EF migration. PaymentMethod is a **row in `identity.dictionary_type`** with code `PM_METHOD` and its items live in the standard `dictionary_item` table.

### Q2. Is PaymentMethod already expressible via Dictionary?

**Answer: YES, in TWO formats:**

#### V15 format (preferred — already used by the seed runner)

`data/bootstrap/reference/mdm/dictionary/PM_METHOD.json` (5 items, all `seed_status: SAFE_TO_SEED_SYSTEM`):
- `PM_CASH` — 现金 (Cash) [is_default=true]
- `PM_BANK_TRANSFER` — 银行转账 (Bank Transfer)
- `PM_CHECK` — 支票 (Check)
- `PM_CREDIT_CARD` — 信用卡 (Credit Card)
- `PM_ONLINE` — 在线支付 (Online: Alipay / WeChat)

`meta.description`:
> 付款方式 — 客户/供应商在结算时使用的支付工具。本字典只覆盖方法(PM_*),账期(SM_*)见 SM_TERM。

This is **the canonical, clean separation**: PaymentMethod (PM_*) and PaymentTerm (SM_*) are TWO separate DictionaryTypes. The V1 tenant-template `payment-method.json` (under `data/bootstrap/reference/tenant-template/`) is a MIXED pre-V15 legacy file and the meta notes explicitly say:
> "GuliERP V1 未来应区分 PaymentMethod (channel/option) 和 PaymentTerm (duration/aging). 本文件 5 项混合两者,需要后续拆开。"

#### V1 tenant-template format (legacy — MIXED, do NOT use directly)

`data/bootstrap/reference/tenant-template/payment-method.json` mixes methods + terms (5 items):
- `PM_PREPAID_DELIVERY` — 款到发货 (method)
- `PM_COD` — 货到付款 (method)
- `PT_NET_30` — 月结30天 (term, `term_days: 30`)
- `PT_NET_60` — 月结60天 (term, `term_days: 60`)
- `PT_NET_90` — 月结90天 (term, `term_days: 90`)

The tenant-template data lives in `data/bootstrap/reference/tenant-template/`, which is NOT the same as the `data/bootstrap/reference/mdm/dictionary/` seed directory used by the V1 dictionary CLI. The tenant-template data would be loaded by a separate (future) tenant bootstrap mechanism, not the current V1 dictionary seed runner.

### Q3. Is there an existing API for PaymentMethod?

**Answer: YES (via Dictionary) and will add a facade in this phase.**

Currently, PaymentMethod items are accessible via the standard Dictionary API:
- `GET /api/v1/mdm/dictionary-types?keyword=PM_METHOD` → returns the `PM_METHOD` DictionaryType (with `id`, `code`, `name`, `description`, ...)
- `GET /api/v1/mdm/dictionary-types/{typeId}/items?keyword=...&status=...&page=1&pageSize=20` → returns the 5 items

Both endpoints require `MdmPolicies.DictionaryRead` (which `ERP_MDM_OPERATOR` and `ERP_SYSTEM_ADMIN` have via the 16 mdm perms — but the latter is per G3-R1C's boundary contract). The standard endpoints also expose full CRUD: POST/PUT/PATCH on types, POST/PUT on items.

**G3-R1E will add a thin facade endpoint** at `GET /api/v1/mdm/payment-methods` (and `GET /api/v1/mdm/payment-methods/{id}`) that internally calls `IMdmDictionaryService.ListItemsAsync(typeId, ...)` for the `PM_METHOD` DictionaryType. This satisfies the brief's explicit allowance for a "facade endpoint" and gives the frontend a clean, semantically-named API.

### Q4. Is there an existing frontend page for PaymentMethod?

**Answer: NO.** The workbench currently has PaymentMethod listed as a **DEFERRED card** (G3-R1D commit `5d40e6b`):

```ts
{
  title: '付款方式 (PaymentMethod)',
  reason: '后端尚无 PaymentMethod 域/实体/端点;当前销售单使用 mock/sales-order.ts 的 paymentTerms 占位',
},
```

The "no API" reason is now outdated (Dictionary API exists). G3-R1E will:
1. Update the workbench card to point to `/mdm/payment-methods` with status "真实 API" (real API).
2. Add the page.
3. Update the navigation.

### Q5. Is PaymentMethod referenced by sales-order mock?

**Answer: YES (legacy only), out of G3-R1E scope.**

`apps/web/src/mock/sales-order.ts` (line ~84) has:
```ts
export const paymentTerms: PaymentTermOption[] = [
  { code: 'M0', name: '货到付款', days: 0 },
  { code: 'M15', name: '15 天月结', days: 15 },
  { code: 'M30', name: '30 天月结', days: 30 },
  { code: 'M60', name: '60 天月结', days: 60 },
  { code: 'T30', name: '票期 30 天', days: 30 }
];
```

This is consumed ONLY by `components/LookupDialog.vue` (sales-order scope, out of G3-R1E per brief "不重做销售单"). The mock data does NOT need to change in G3-R1E.

### Q6. Should the minimum implementation be a standalone entity or reuse Dictionary?

**Answer: REUSE Dictionary via a facade endpoint.**

Per the brief's §WorkItem 2 优先实现方式:
> 1. 复用 MDM DictionaryType / DictionaryItem。
> 2. PaymentMethod API 可以是 facade endpoint...
> 3. 如果已有统一 dictionary endpoint，可以只在前端通过 dictionary code 查询，但报告中必须说清楚。

The architecture is already aligned:
- The V1 dictionary registry (DictionarySeedDescriptorRegistry.V1) lists `PM_METHOD` as the canonical payment-method dictionary.
- The seed runner (`IMdmDictionarySeedService`) loads `PM_METHOD.json` from the seed directory.
- The CRUD service (`IMdmDictionaryService`) handles full read/write.
- Adding a new standalone entity would:
  1. Require a new EF migration (banned by brief "不引入新框架" + "不提交新 migration").
  2. Duplicate the existing Dictionary facade for an entity with 1 type + 5 items.
  3. Force a downstream SalesOrder model change to reference the new entity (banned by brief "不改 SalesOrder 模型").

**Decision: facade endpoint over Dictionary API. No new entity. No new migration. No new service.** A small endpoint that wraps `IMdmDictionaryService.ListItemsAsync` for the `PM_METHOD` DictionaryType.

---

## 2. Proposed G3-R1E implementation plan

### 2.1 Backend (WorkItem 2)

1. **Add facade endpoint** `GET /api/v1/mdm/payment-methods` in `apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs`:
   - Resolves the `PM_METHOD` DictionaryType by code via the existing service
   - Calls `IMdmDictionaryService.ListItemsAsync(typeId, query, ct)` with the query parameters
   - Returns `Results.Ok(items)` with the standard paged-result DTO
   - Requires `MdmPolicies.DictionaryRead`

2. **Add facade endpoint** `GET /api/v1/mdm/payment-methods/{id:long}`:
   - Calls `IMdmDictionaryService.GetItemByIdAsync(typeId, id, ct)`
   - Returns 200 with item DTO or 404
   - Requires `MdmPolicies.DictionaryRead`

3. **No new entity, no new service, no new migration.** Just a 30-line endpoint addition.

4. **Seed verification**: ensure `PM_METHOD` is in the DB. Either:
   - The dev DB already has it (from G3-R1B seed run), OR
   - Run `seed-mdm-dictionary --seed-path data/bootstrap/reference/mdm/dictionary` to seed it (idempotent, uses sentinel item code `PM_CASH`).

### 2.2 Frontend (WorkItem 3)

1. **Add API client** `apps/web/src/api/mdm/payment-method.ts`:
   - `listPaymentMethods(params)` → `GET /api/v1/mdm/payment-methods`
   - `getPaymentMethod(id)` → `GET /api/v1/mdm/payment-methods/{id}`

2. **Add page** `apps/web/src/views/mdm/PaymentMethodList.vue`:
   - Reuse the existing `MdmListToolbar` + `el-table` + `MdmEmptyState` + `ApiError` handling pattern (the same pattern as `UomList.vue`).
   - Columns: code, name (zh), name (en), is_default, sort_order, status.
   - Read-only V1 (no create/edit; the standard Dictionary endpoint has write, but per brief we keep the facade read-only and route writes through the Dictionary page).

3. **Add route** `/mdm/payment-methods` in `apps/web/src/router/mdm.ts`.

4. **Update workbench** `MasterDataWorkbench.vue`:
   - Move PaymentMethod from `deferredModules` to `primaryModules`.
   - Mark as "真实 API" with the same card layout.

5. **Update navigation** `apps/web/src/layout/navigation.ts`:
   - Add "付款方式" entry under the "主数据" module.

### 2.3 Write-path evidence (WorkItem 4)

New script `tools/dev/g3-r1e-master-data-write-path-evidence.ps1` that:
- Logs in as each of the 4 dedicated users from G3-R1C
- For each of 3+ master data entities (Dictionary/UOM/NumberingRule/PaymentMethod), tests:
  - **MDM_OPERATOR**: create + update + status change → 201/200/200; read → 200; cleanup → 204
  - **EMPLOYEE_OPERATOR**: write → 403; read → 200/404
  - **SALES_OPERATOR**: write → 403; read → 403
  - **SYS_ADMIN**: write → 403; read → 403 (boundary contract)
  - **anonymous**: write → 401; read → 401
- All test data uses `g3r1e_` prefix and is cleaned up at end (or at next run).
- Idempotent: re-running yields the same PASS/FAIL/BLOCKED/CREATED/UPDATED/CLEANED summary.

### 2.4 Build + test (WorkItem 5)

- `dotnet build GuliERP.slnx` → 0/0
- `dotnet test GuliERP.Identity.Tests` → 93/93 (no regression)
- `dotnet test GuliERP.Mdm.Tests` → 278/278 (no regression)
- `npm run build` (apps/web) → 0 type errors, 0 build errors
- `pwsh tools/dev/g3-r1e-master-data-write-path-evidence.ps1` → ALL write-path checks pass

### 2.5 Report (WorkItem 6)

`docs/verification/G3_R1E_PAYMENT_METHOD_AND_WRITE_PATH_REPORT.md` — full G3-R1E verification report (mirroring the G3-R1D report structure).

---

## 3. Boundaries (hard limits per brief)

- ❌ NO new DB entity, NO new migration
- ❌ NO change to Identity permission model
- ❌ NO change to SalesOrder model
- ❌ NO complex PaymentTerm / 账期 engine
- ❌ NO mock data for PaymentMethod on the frontend
- ❌ NO commit of real passwords / `.quarantine/` scripts
- ✅ OK: add facade endpoint over existing Dictionary API
- ✅ OK: add frontend page that consumes the new facade
- ✅ OK: write-path runtime evidence using 4 dedicated users from G3-R1C

---

## 4. Open questions for WorkItem 2

| Q | Decision |
|---|---|
| Should the facade support status filter? | **YES** — match the standard Dictionary item API contract. |
| Should the facade support paging? | **YES** — match the standard `?page=&pageSize=` contract. |
| Should the facade expose `is_default` and `sort_order`? | **YES** — these are needed for the frontend UI badge. Both fields are part of the existing `DictionaryItem` DTO. |
| Should write operations go through the facade? | **NO** — the brief says "deferred for write" for PaymentMethod, and the standard Dictionary write endpoint already covers MDM_OPERATOR's write needs (via the existing `/dictionary-items` POST/PUT). The facade is READ-ONLY. The G3-R1E write-path evidence will use the standard Dictionary POST/PUT for the create/update tests. |
| What if `PM_METHOD` is not yet in the dev DB? | Run `seed-mdm-dictionary --seed-path data/bootstrap/reference/mdm/dictionary` (idempotent). The seed run was part of G3-R1B; the G3-R1E work will verify the seed state and re-run if needed. |

---

## 5. Summary

PaymentMethod is **already a first-class DictionaryType** in the V1 system. The architecture is correctly designed. The G3-R1E work is a **facade** (a few lines in `MdmEndpoints.cs`) + a **frontend page** (a few hundred lines in `apps/web`) + a **write-path evidence script** (mirroring the G3-R1D evidence pattern but for POST/PUT operations). No entity, no migration, no permission model change. The brief's "PaymentMethod 最小 runtime 实现" is a clean fit for the existing Dictionary infrastructure.

**WorkItem 1 discovery: COMPLETE.** Ready to proceed to WorkItem 2 (facade endpoint) and WorkItem 3 (frontend page).
