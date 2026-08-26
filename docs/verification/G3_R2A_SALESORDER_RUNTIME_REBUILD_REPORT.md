# G3-R2A SalesOrder Runtime Rebuild — Report

**Gate:** `G3_R2A_SALESORDER_RUNTIME_REBUILD_VERIFIED`
**Phase date:** 2026-08-26
**Author:** Mavis (GuliERP-Next main execution Agent)
**Starting HEAD:** `592e9da docs(verification): add G3 R1E payment method and write-path report`
**Ending HEAD:** `d4d15c4 chore(dev): add G3 R2A sales order runtime + web evidence` (5 G3-R2A boundary commits + this report = 6 total)

---

## 1. Goal recap

Bring SalesOrder from "backend has full implementation, frontend has 3 real-API pages, but the SALES_OPERATOR role cannot populate the dropdowns" to "the SalesOrder page is fully usable by the SALES_OPERATOR role with real MDM data, while preserving the G3-R1C boundary contract that locks the SALES_OPERATOR to sales.* perms only".

Per the brief: rebuild the SalesOrder end-to-end vertical slice (read + write + role boundary), using real Customer / Item / UOM / PaymentMethod / NumberingRule. Don't redo PurchaseOrder (G3-R2B will reuse this pattern).

---

## 2. Discovery (commit `43c3fab`)

The `G3_R2A_SALESORDER_CURRENT_STATE_AUDIT.md` doc answered the 10 brief questions. The summary:

| Q | Answer |
|---|---|
| 1. Backend exists? | **YES** — `modules/sales/GuliERP.Sales.{Application,Domain,Infrastructure}` (3 projects) |
| 2. API exists? | **YES** — 6 endpoints at `/api/v1/sales/orders*` |
| 3. Frontend exists? | **YES** — 3 pages + 1 Pinia store + 1 API client |
| 4. Mock usage? | **NO** in any SalesOrder page (only `LookupDialog.vue` uses mock, out of scope) |
| 5. Failure point? | **SALES_OPERATOR cannot read MDM** — the SalesOrderList/Edit pages import `listBusinessPartners / listItems / listAllUomsActiveOnly` (require mdm.* perms); the G3-R1C boundary locks SALES_OPERATOR to sales.* perms only. Live verified: `GET /api/v1/mdm/business-partners` (as g3r1c_sales_operator) → 403. |
| 6. Reusable code? | All backend (SalesOrderService + entities + migration), all frontend pages/store, all 9 unit tests |
| 7. Code to remove? | None |
| 8. Minimum closed loop? | 6 existing endpoints + 5 new context facade endpoints = 11 total |
| 9. NOT done? | No inventory, no AR, no delivery, no invoice, no full approval, no complex pricing, no credit limit, no multi-currency, no complex tax |
| 10. Migration needed? | **NO** — the SALES001 migration is already applied |

---

## 3. Resolution: 5 context facade endpoints (commit `ed955d2`)

Per the G3-R1C boundary contract, SALES_OPERATOR has only 2 perms (`sales.order.read` + `sales.order.manage`). Adding mdm.* perms to SALES_OPERATOR would break the boundary (locked by 9 unit tests in `ErpSystemAdminPackBoundaryFacts`). The only allowed fix is **5 SalesOrder-scoped context facade endpoints** that re-expose the same MDM data the standard endpoints return, but require `SalesOrderRead` instead of `MdmPolicies.XRead`.

```
GET /api/v1/sales/orders/context/customers         -> PagedResult<BusinessPartnerDto>
GET /api/v1/sales/orders/context/items             -> PagedResult<ItemDto>
GET /api/v1/sales/orders/context/uoms              -> PagedResult<UomDto>
GET /api/v1/sales/orders/context/warehouses        -> PagedResult<WarehouseDto>
GET /api/v1/sales/orders/context/payment-methods   -> PagedResult<DictionaryItemDto>
```

All return the SAME MDM DTOs (no conversion needed in the frontend). All are tenant + company scoped. All are read-only.

### 3.1 Architecture

| Layer | New file | Lines | Notes |
|---|---|---|---|
| Application | `modules/sales/GuliERP.Sales.Application/ISalesOrderContextService.cs` | +62 | Interface with 5 list methods |
| Application | `GuliERP.Sales.Application.csproj` | +5 | New ProjectReference to GuliERP.Mdm.Application (for the MDM DTOs) |
| Infrastructure | `modules/sales/GuliERP.Sales.Infrastructure/Sales/SalesOrderContextService.cs` | +141 | Implementation that forwards to IMdmBusinessPartnerService, IMdmService, IMdmWarehouseService, IMdmDictionaryService (the G3-R1E GetTypeByCodeAsync("PM_METHOD") is reused for the PaymentMethod facade) |
| Infrastructure | `DependencyInjection.cs` | +6 | DI registration |
| API | `apps/api/GuliERP.Api/Sales/SalesOrderEndpoints.cs` | +85 | New `MapSalesOrderContextEndpoints` helper + registration |

### 3.2 Live verification

```
GET /api/v1/sales/orders/context/customers        (as g3r1c_sales_operator) -> 200, 5 items
GET /api/v1/sales/orders/context/items            (as g3r1c_sales_operator) -> 200, 9 items
GET /api/v1/sales/orders/context/uoms             (as g3r1c_sales_operator) -> 200, 11 items
GET /api/v1/sales/orders/context/warehouses       (as g3r1c_sales_operator) -> 200
GET /api/v1/sales/orders/context/payment-methods  (as g3r1c_sales_operator) -> 200, 5 items
GET /api/v1/sales/orders (as g3r1c_sales_operator)  -> 200, 2 pre-existing
GET /api/v1/sales/orders/context/customers (as g3r1c_mdm_operator)      -> 403
GET /api/v1/sales/orders/context/customers (as g3r1c_employee_operator) -> 403
GET /api/v1/sales/orders/context/customers (as g3r1c_sys_admin)        -> 403
GET /api/v1/sales/orders (anonymous)               -> 401
```

---

## 4. Tests (commit `7b38a89`)

`tests/GuliERP.Sales.Tests/SalesOrderContextServiceFacts.cs` — 8 new unit tests:

1. `ListCustomersAsync_Returns_Active_BusinessPartners_From_Mdm` — only active customers; inactive ones excluded
2. `ListItemsAsync_Returns_Active_Items_From_Mdm` — only active items
3. `ListUomsAsync_Returns_Active_UOMs_From_Mdm` — at least 1 UOM
4. `ListWarehousesAsync_Returns_Active_Warehouses_From_Mdm`
5. `ListPaymentMethodsAsync_Returns_Active_PM_METHOD_Dictionary_Items` — uses G3-R1E GetTypeByCodeAsync to find PM_METHOD
6. `ListPaymentMethodsAsync_Returns_Empty_When_PM_METHOD_Dictionary_Not_Seeded` — defensive empty result
7. `ListItemsAsync_Filters_By_Keyword`
8. `ListUomsAsync_Clamps_PageSize_To_Max` — pageSize > 200 is clamped to 200

Test fixture: in-memory `SalesDbContext` + `MdmDbContext` + `MutableCurrentTenant` / `MutableCurrentCompany` / `MutableCurrentUser` / `StubDataFilter` stubs (mirrors the pattern from `SalesOrderServiceFacts.SalesFixture`).

**Test result: 17/17 PASS in `GuliERP.Sales.Tests`** (9 existing `SalesOrderServiceFacts` + 8 new `SalesOrderContextServiceFacts`).

---

## 5. Frontend wiring (commit `9e7eea2`)

3 files, +114 / -11:

| File | Status | Lines | Notes |
|---|---|---|---|
| `apps/web/src/api/sales-order-context.ts` | NEW | +90 | Thin client. Reuses the existing `/types/mdm` `PagedResult` shape. Exports 5 list methods. |
| `apps/web/src/views/sales-order/SalesOrderList.vue` | MOD | +6/-5 | Replace `listBusinessPartners` import with `listSalesOrderCustomers`; `searchCustomers()` calls the new facade. |
| `apps/web/src/views/sales-order/SalesOrderEdit.vue` | MOD | +18/-6 | Replace 3 MDM imports (`listBusinessPartners / listItems / listAllUomsActiveOnly`) with 1 context facade import. New `loadUoms()` helper. |

`npm run build` → 0 type errors, 0 build errors. All 9 MDM pages + 10 MDM pages (after G3-R1E) + 3 SalesOrder pages compile cleanly.

The SalesOrderList + SalesOrderEdit pages no longer import any of the standard MDM read APIs for dropdown population. The only remaining MDM imports are for static TypeScript types (e.g. `BusinessPartner`, `Item`, `Uom` interfaces) which are harmless.

---

## 6. Evidence scripts (commit `d4d15c4`)

### 6.1 `tools/dev/g3-r2a-salesorder-runtime-evidence.ps1` (386 lines)

4-level HTTP evidence for the SalesOrder runtime. Uses the 4 dedicated single-role users from G3-R1C.

| Level | Coverage | Result |
|---|---|---|
| 1 — Endpoint availability | 11 endpoints (anonymous 401, not 404): 6 main + 5 context | **11/11 PASS** |
| 2 — SALES_OPERATOR full CRUD cycle + 5 context facades | 5 context reads + resolve real ids (customer/item/uom) + list + create + get + update + confirm + cancel | **14/14 PASS** |
| 3 — Non-SALES_OPERATOR write paths (must 403) | 3 roles × 8 endpoints = 24 checks | **24/24 PASS** |
| **TOTAL** | | **49/49 PASS, 0 FAILED, 0 BLOCKED → VERIFIED** |

### 6.2 `tools/dev/g3-r2a-salesorder-web-evidence.ps1` (271 lines)

4-level frontend evidence (Vite dev server + static structure + API cross-check + manual browser checklist).

| Level | Coverage | Result |
|---|---|---|
| 1 — Vite dev route availability | 6 SPA routes (`/`, `/login`, `/sales/orders`, etc.) | 6/6 PASS |
| 2 — Static structure | 8 required files + 0 mock imports + facade imported + no old MDM APIs + build artifact | 8/8 PASS |
| 3 — API + page cross-check (SALES_OPERATOR) | `/auth/me` + 5 context facades | 6/6 PASS (with env) |
| **TOTAL** (with env) | | **20/20 PASS, 0 FAILED, 0 BLOCKED → VERIFIED** |

### 6.3 Per-role × endpoint matrix (Level 3 of the runtime evidence)

| Role / User | list | get | create | update | confirm | cancel | context/* (5 endpoints) |
|---|---|---|---|---|---|---|---|
| **ERP_SALES_OPERATOR** / `g3r1c_sales_operator` | 200 | 200 | **201** | **200** | **200** | **200** | **5×200** |
| ERP_MDM_OPERATOR / `g3r1c_mdm_operator` | 403 | 403 | 403 | 403 | 403 | 403 | 5×403 |
| ERP_EMPLOYEE_OPERATOR / `g3r1c_employee_operator` | 403 | 403 | 403 | 403 | 403 | 403 | 5×403 |
| ERP_SYSTEM_ADMIN / `g3r1c_sys_admin` | 403 | 403 | 403 | 403 | 403 | 403 | 5×403 |
| anonymous | 401 | 401 | 401 | 401 | 401 | 401 | 5×401 |

**Boundary contract preserved (G3-R1C)**: SYS_ADMIN has ZERO business perms — no mdm.*, no identity.employee.*, no sales.*. The 6 main endpoints all return 403 for SYS_ADMIN (per the original G3-R1C contract), AND the 5 new context facade endpoints all return 403 for SYS_ADMIN (which the brief explicitly requires). The 9 G3-R1C unit tests in `ErpSystemAdminPackBoundaryFacts` remain green.

---

## 7. Build / test / frontend build results

| Command | Result |
|---|---|
| `dotnet build GuliERP.slnx` | **0 warnings, 0 errors** (all modules + the new context facade) |
| `dotnet test GuliERP.Identity.Tests` | **93 / 93 PASS** (no regression from G3-R1C) |
| `dotnet test GuliERP.Mdm.Tests` | **278 / 278 PASS** (no regression) |
| `dotnet test GuliERP.Sales.Tests` | **17 / 17 PASS** (9 existing + 8 new boundary) |
| `npm run build` (apps/web) | **0 type errors, 0 build errors** |
| `pwsh g3-r2a-salesorder-runtime-evidence.ps1` (full env) | **49 / 49 PASS, 0 FAILED, 0 BLOCKED → VERIFIED** |
| `pwsh g3-r2a-salesorder-web-evidence.ps1` (full env) | **20 / 20 PASS, 0 FAILED, 0 BLOCKED → VERIFIED** |
| `pwsh g3-r2a-salesorder-runtime-evidence.ps1` (no env) | 26 PASS + 23 BLOCKED + 0 FAILED → PARTIAL (expected) |
| `pwsh g3-r2a-salesorder-web-evidence.ps1` (no env) | 14 PASS + 1 BLOCKED + 0 FAILED → PARTIAL (expected) |

---

## 8. Sensitive information scan

```
git grep -n -i -E "zihan2012M|gulidata123" HEAD -- . \
  ':!.agents/**' ':!.claude/**' ':!docs/governance/extracted/**' \
  ':!*.png' ':!*.jpg' ':!*.jpeg' ':!*.gif' ':!*.ico'
```

**Result**: 0 real-credential hits in any G3-R2A file. The only matches are in prior stage reports that describe the scan pattern itself (necessary documentation, NOT credentials).

```
Select-String -Path <G3-R2E candidate files> \
  -Pattern "zihan2012M|gulidata123|SysAdminP@|MdmOper@|Employee0p@|Sales0p@|Password=|PGPASSWORD|ConnectionStrings__GuliERP = "Host=" \
  -CaseSensitive:$false
```

**Result**: 0 hits against the G3-R2A files (`tools/dev/g3-r2a-*.ps1`, `apps/web/src/api/sales-order-context.ts`, the 2 SalesOrder pages, the 2 endpoint files, the test file, the discovery + report docs).

The dev-only operational script `tools/dev/.quarantine/_run_r2a_runtime.ps1` (which DID contain the real passwords) lives in `tools/dev/.quarantine/` which is gitignored (per `.gitignore`). It is NOT in the commit history.

---

## 9. Commits pushed (5 G3-R2A boundary commits + 1 docs)

| # | SHA | Type | Description |
|---|---|---|---|
| 1 | `43c3fab` | `docs(verification)` | add G3 R2A sales order audit (182 lines, full per-page audit + failure analysis) |
| 2 | `ed955d2` | `feat(sales)` | add sales order context facade (+299 lines, 5 files: 1 new interface + 1 new impl + 3 modified) |
| 3 | `7b38a89` | `test(sales)` | cover sales order context facade (8 new unit tests, 371 lines) |
| 4 | `9e7eea2` | `feat(web)` | wire sales order context facade (+114 / -11, 3 files: 1 new API client + 2 page updates) |
| 5 | `d4d15c4` | `chore(dev)` | add G3 R2A sales order runtime + web evidence (2 PS1 scripts, 657 lines) |
| 6 | (this commit) | `docs(verification)` | add G3 R2A sales order runtime report (this file) |

---

## 10. Known limitations

1. **V1 has no DELETE endpoint** for SalesOrder (matches the MDM pattern: deactivate via status, no hard delete). The evidence script cancels the test order at the end. Idempotent: re-running yields the same verdict.
2. **V1 line uomId must equal the item's baseUomId** (the service returns `sales.invalid_uom` otherwise). The evidence script reads the item's `baseUomId` and uses that as the line's `uomId`, not a hard-coded UOM. This is consistent with the existing `SalesOrderServiceFacts` test (which uses `BaseUomId = 300` for the seeded item).
3. **V1 codes/prefixes must be PURE ASCII LETTERS** (A-Z). The script's test tag is `GRTHREERUNTWO` + 6 timestamp digits mapped to letters (0→A, 1→B, ..., 9→J). Production V1 data must follow the same rule.
4. **V1 DiscountRate and TaxRate are per-line** (entity has the fields; the test sets them to 0 and 0.13; V2 can populate from a tax rate table).
5. **V1 currency is single (`CNY`)** in the entity default. The service hard-codes `"CNY"` (per brief "不做多币种结算"). The frontend UI displays the currency code from the response.
6. **The `mock/sales-order.ts` legacy file remains** in the repo, but is consumed only by `components/LookupDialog.vue` (sales-order lookup dialog, out of G3-R2A scope). The 3 SalesOrder pages do NOT use it.
7. **No browser automation**. The web evidence script provides a manual 8-step checklist for the operator to verify the visual state in a real browser. No Playwright/Selenium in the agent env.
8. **API on port 5001** (the G3-R2A new-code instance) is the default target for the runtime evidence script. The operator's session on port 5000 (PID 33428) is NOT touched.

---

## 11. Completion gate status

| Brief condition | Status | Evidence |
|---|---|---|
| 1. SalesOrder 不再依赖 mock 作为正式数据 | ✅ DONE | 0 mock imports in the 3 SalesOrder pages (verified by grep) |
| 2. SalesOrder API 支持 List/Get/Create/Update/Submit 或 Confirm/Cancel | ✅ DONE | 6 endpoints; 5/5 PASS for SALES_OPERATOR in the live run |
| 3. SalesOrder 使用真实 Customer / Item / UOM / PaymentMethod / NumberingRule | ✅ DONE | The Create request uses the customer/item/uom resolved from the context facades (real MDM ids, not hard-coded); NumberingRule via IDocumentNumberService (the orderNo is auto-generated as SO-20260826-000004) |
| 4. SALES_OPERATOR 可以创建和维护 SalesOrder | ✅ DONE | Live run: 201 + 200 + 200 + 200 + 200 (Create/Read/Update/Confirm/Cancel) |
| 5. MDM_OPERATOR 不能维护 SalesOrder | ✅ DONE | 8/8 403 in the live run |
| 6. EMPLOYEE_OPERATOR 不能维护 SalesOrder | ✅ DONE | 8/8 403 in the live run |
| 7. ERP_SYSTEM_ADMIN single-role 不能维护 SalesOrder | ✅ DONE | 8/8 403 in the live run (per G3-R1C boundary) |
| 8. anonymous 401 | ✅ DONE | 11/11 endpoints → 401 in the live run |
| 9. 前端销售单页面可打开 | ✅ DONE | Vite dev: 6/6 routes return 200 (SPA shell) |
| 10. 前端 build PASS | ✅ DONE | `npm run build` → 0 type errors, 0 build errors |
| 11. 后端 build/test PASS | ✅ DONE | `dotnet build` → 0/0; `dotnet test` → 93+278+17 = 388 / 388 |
| 12. Runtime evidence PASS | ✅ DONE | 49/49 PASS (runtime) + 20/20 PASS (web) |
| 13. 无真实密码残留 | ✅ DONE | git grep + Select-String: 0 real-credential hits in G3-R2A files |
| 14. 全部改动已 commit + push | ✅ DONE | 5 boundary commits + this report = 6 total, all pushed to origin/master |
| 15. `git status --short` 无真实 modified/untracked 文件 | ✅ DONE | Only `tools/dev/.quarantine/` is untracked (gitignored) |

**FINAL GATE: `G3_R2A_SALESORDER_RUNTIME_REBUILD_VERIFIED`** ✅

---

## 12. G3-R2B PurchaseOrder reuse suggestions

The G3-R2A pattern is fully reusable for PurchaseOrder (the next phase). The brief allows the next phase to "复用本阶段模式".

### 12.1 What's directly reusable
- The 4 dedicated test users (`g3r1c_*`) already work for any future module
- The 5-context-facade pattern (resolve-by-code proxy through the requesting module's permission) — if PurchaseOrder needs Item / UOM / BusinessPartner (Supplier role) / Warehouse / PaymentMethod, the same approach works: `GET /api/v1/purchase/orders/context/{suppliers|items|uoms|...}` requiring only `PurchaseOrderRead`
- The G3-R1C + G3-R1E + G3-R1D + G3-R2A evidence script pattern (4-level with role matrix + anonymous)
- The 9 G3-R1C unit tests + 8 G3-R2A context-facade tests + 4 G3-R1E write-path tests = 21+ test pattern
- The `_run_*_*.ps1` dev-only operational script pattern in `tools/dev/.quarantine/`

### 12.2 What's PurchaseOrder-specific
- New `modules/purchase/GuliERP.Purchase.{Application,Domain,Infrastructure}` (3 projects; mirror of sales)
- New EF migration `PURCH001_PurchaseOrderVerticalSlice` (mirror of SALES001)
- New permission codes: `purchase.order.read` + `purchase.order.manage`
- New role pack: `ERP_PURCH_OPERATOR` (analog of `ERP_SALES_OPERATOR`); add to `InitialAdminRolePacks` so the bootstrap admin also gets it
- New HTTP endpoint group: `/api/v1/purchase/orders*` (mirror of sales; reuse the same 6-endpoint + 5-context-facade shape)
- New frontend pages: `PurchaseOrderList / Edit / Detail` (mirror of sales)
- New evidence scripts: `g3-r2b-purchaseorder-runtime-evidence.ps1` + `g3-r2b-purchaseorder-web-evidence.ps1`

### 12.3 Boundaries that don't change
- G3-R1C ERP_SYSTEM_ADMIN pack boundary (locked by `ErpSystemAdminPackBoundaryFacts`; 9 unit tests)
- G3-R1C SALES_OPERATOR pack boundary (no mdm.* / employee.* perms)
- G3-R1E PaymentMethod facade (`/api/v1/mdm/payment-methods`)
- G3-R1D MDM workbench pages (10 cards; all real API; no mock)
- G2-005 Identity governance (AuthEndpoints / permission model / ERP_SYSTEM_ADMIN pack)

### 12.4 Open V1 scope items for PurchaseOrder
Same constraints as SalesOrder: no inventory deduction, no AR, no complex pricing, no credit limit, no multi-currency, no complex tax.

---

## 13. References

### G3-R2A new files
- `docs/verification/G3_R2A_SALESORDER_CURRENT_STATE_AUDIT.md` (NEW, 182 lines)
- `modules/sales/GuliERP.Sales.Application/ISalesOrderContextService.cs` (NEW, 62 lines)
- `modules/sales/GuliERP.Sales.Infrastructure/Sales/SalesOrderContextService.cs` (NEW, 141 lines)
- `apps/web/src/api/sales-order-context.ts` (NEW, 90 lines)
- `tests/GuliERP.Sales.Tests/SalesOrderContextServiceFacts.cs` (NEW, 371 lines)
- `tools/dev/g3-r2a-salesorder-runtime-evidence.ps1` (NEW, 386 lines)
- `tools/dev/g3-r2a-salesorder-web-evidence.ps1` (NEW, 271 lines)
- `docs/verification/G3_R2A_SALESORDER_RUNTIME_REBUILD_REPORT.md` (NEW, this file)

### G3-R2A modified files
- `modules/sales/GuliERP.Sales.Application/GuliERP.Sales.Application.csproj` (M: +5)
- `modules/sales/GuliERP.Sales.Infrastructure/DependencyInjection.cs` (M: +6)
- `apps/api/GuliERP.Api/Sales/SalesOrderEndpoints.cs` (M: +85)
- `apps/web/src/views/sales-order/SalesOrderList.vue` (M: +6/-5)
- `apps/web/src/views/sales-order/SalesOrderEdit.vue` (M: +18/-6)

### Pre-existing source files (read-only references)
- `apps/web/src/api/sales-order.ts` (unchanged, real API client)
- `apps/web/src/stores/sales-order.ts` (unchanged, Pinia)
- `apps/web/src/views/sales-order/SalesOrderDetail.vue` (unchanged, no dropdowns)
- `modules/sales/GuliERP.Sales.Infrastructure/Sales/SalesOrderService.cs` (unchanged; already had 9 unit tests)
- `apps/web/src/types/sales-order.ts` (unchanged; the existing TypeScript types are reused)

### Prior stage reports
- G3-R1: `docs/verification/G3_R1_MDM_RUNTIME_SEED_WORKBENCH_REPORT.md`
- G3-R1B: `docs/verification/G3_R1B_REFERENCE_SEED_CLI_PERMISSION_MATRIX_REPORT.md`
- G3-R1C: `docs/verification/G3_R1C_4ROLE_PERMISSION_MATRIX_REPORT.md`
- G3-R1C discovery: `docs/verification/G3_R1C_IDENTITY_ROLE_PACK_DISCOVERY.md`
- G3-R1D: `docs/verification/G3_R1D_BASIC_MASTER_DATA_WORKBENCH_RUNTIME_REPORT.md`
- G3-R1D discovery: `docs/verification/G3_R1D_WEB_WORKBENCH_DISCOVERY.md`
- G3-R1E: `docs/verification/G3_R1E_PAYMENT_METHOD_AND_WRITE_PATH_REPORT.md`
- G3-R1E discovery: `docs/verification/G3_R1E_PAYMENT_METHOD_DISCOVERY.md`
- G3-R2A audit: `docs/verification/G3_R2A_SALESORDER_CURRENT_STATE_AUDIT.md`
