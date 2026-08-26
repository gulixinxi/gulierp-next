# G3-R2A SalesOrder Current State Audit

**Phase:** G3-R2A SalesOrder Runtime Rebuild
**Audit date:** 2026-08-26
**Author:** Mavis (GuliERP-Next main execution Agent)
**Source commit (HEAD at audit time):** `592e9da docs(verification): add G3 R1E payment method and write-path report`

---

## 1. Inventory by layer

### 1.1 Backend (modules/sales) — **EXISTS, substantial**

| Path | Status | Lines | Notes |
|---|---|---|---|
| `modules/sales/GuliERP.Sales.Domain/GuliERP.Sales.Domain.csproj` | EXISTS | project | |
| `modules/sales/GuliERP.Sales.Domain/Entities/SalesOrder.cs` | EXISTS | 30 | Header entity: Id, TenantId, CompanyId, OrderNo, CustomerId + 2 snapshots, OrderDate, RequestedDeliveryDate, CurrencyCode, Status, Remarks, TotalNet/Tax/Amount, audit fields, ConcurrencyVersion, Lines |
| `modules/sales/GuliERP.Sales.Domain/Entities/SalesOrderLine.cs` | EXISTS | (exists) | Line entity: Id, SalesOrderId, LineNo, ItemId + 2 snapshots, UomId + 2 snapshots, Quantity, UnitPrice, DiscountRate, TaxRate, Net/Tax/TotalAmount, Remarks |
| `modules/sales/GuliERP.Sales.Domain/Enums/SalesOrderStatus.cs` | EXISTS | (exists) | Draft / Confirmed / Cancelled (Status = Draft default) |
| `modules/sales/GuliERP.Sales.Application/ISalesOrderService.cs` | EXISTS | 11 | 6 methods: List / GetById / CreateDraft / UpdateDraft / Confirm / Cancel |
| `modules/sales/GuliERP.Sales.Application/SalesOrderDtos.cs` | EXISTS | 92 | 6 DTOs: ListItem / Detail / Line / Create / Update / ListQuery |
| `modules/sales/GuliERP.Sales.Application/SalesPolicies.cs` | EXISTS | (exists) | `SalesOrderRead`, `SalesOrderManage` (already in identity permissions) |
| `modules/sales/GuliERP.Sales.Application/SalesPermissions.cs` | EXISTS | (exists) | `sales.order.read`, `sales.order.manage` |
| `modules/sales/GuliERP.Sales.Application/SalesErrorCodes.cs` | EXISTS | (exists) | `InvalidCustomer`, `InvalidItem`, etc. |
| `modules/sales/GuliERP.Sales.Application/SalesValidationException.cs` | EXISTS | (exists) | |
| `modules/sales/GuliERP.Sales.Infrastructure/...` | EXISTS | project | |
| `modules/sales/GuliERP.Sales.Infrastructure/DependencyInjection.cs` | EXISTS | (exists) | Registers `SalesDbContext` + `ISalesOrderService` |
| `modules/sales/GuliERP.Sales.Infrastructure/Persistence/SalesDbContext.cs` | EXISTS | (exists) | |
| `modules/sales/GuliERP.Sales.Infrastructure/Persistence/Configurations/SalesOrderConfiguration.cs` | EXISTS | (exists) | |
| `modules/sales/GuliERP.Sales.Infrastructure/Persistence/Configurations/SalesOrderLineConfiguration.cs` | EXISTS | (exists) | |
| `modules/sales/GuliERP.Sales.Infrastructure/Sales/SalesOrderService.cs` | EXISTS | (large) | Full implementation. Uses `IDocumentNumberService` for order number generation (NumberingRule integration), `MdmDbContext` to resolve Customer + Item + UOM cross-module, `ICurrentTenant` + `ICurrentCompany` for scope, snapshot pattern (denormalize Customer/Item/UOM name+code at create time). |
| `modules/sales/GuliERP.Sales.Infrastructure/Migrations/20260822130000_SALES001_RealSalesOrderVerticalSlice.cs` | EXISTS, **ALREADY APPLIED** | migration | Creates `sales.gulierp_sales_order` + `sales.gulierp_sales_order_line` tables in the `sales` schema. |
| `apps/api/GuliERP.Api/Sales/SalesOrderEndpoints.cs` | EXISTS | 117 | 6 endpoints registered, all with `[Authorize]` + Idempotency-Key support on Create + RFC 7807 problem details on validation errors |
| `apps/api/GuliERP.Api/Program.cs` line 320 | `app.MapSalesOrderEndpoints();` — **REGISTERED** | | |
| `tests/GuliERP.Sales.Tests/SalesOrderServiceFacts.cs` | EXISTS, **9/9 PASS** | | Domain + service tests |

### 1.2 Frontend (apps/web) — **EXISTS, real API, no mock**

| Path | Status | Notes |
|---|---|---|
| `apps/web/src/api/sales-order.ts` | EXISTS, real API | `listSalesOrders / getSalesOrder / createSalesOrder / updateSalesOrder / confirmSalesOrder / cancelSalesOrder`. All real `apiGet / apiPost / apiPut`. |
| `apps/web/src/stores/sales-order.ts` | EXISTS, Pinia | Wraps the API client in a store. |
| `apps/web/src/views/sales-order/SalesOrderList.vue` | EXISTS | Calls `useSalesOrderStore().fetchList()` + `listBusinessPartners()` for customer filter. |
| `apps/web/src/views/sales-order/SalesOrderEdit.vue` | EXISTS | Calls `listBusinessPartners` + `listItems` + `listAllUomsActiveOnly` for dropdown population. |
| `apps/web/src/views/sales-order/SalesOrderDetail.vue` | EXISTS | Read-only view. |
| `apps/web/src/mock/sales-order.ts` | EXISTS, **LEGACY** | Only consumed by `components/LookupDialog.vue` (out of G3-R2A scope; G3-R1D already documented this). |

### 1.3 Tests — **9/9 PASS**

`tests/GuliERP.Sales.Tests/SalesOrderServiceFacts.cs` covers:
1. `CreateDraft_Persists_Real_Mdm_Snapshots_And_Amounts` — ✅ brief item 1 (Create success)
2. `CreateDraft_Rejects_Inactive_Customer` — ✅ brief item 2 (Create rejects missing customer)
3. `CreateDraft_Rejects_Supplier_Only_BusinessPartner` — ✅ (Create rejects wrong customer role)
4. `CreateDraft_Rejects_Inactive_Item` — ✅ brief item 3 (Create rejects missing item)
5. `CreateDraft_Rejects_Invalid_Line_Quantity_And_Price` — ✅
6. `UpdateDraft_Replaces_Lines_And_Increments_Concurrency` — ✅ brief item 5 (Update success + concurrency)
7. `Confirmed_Order_Is_Not_Editable` — ✅ brief item 6 (Update rejects stale ConcurrencyVersion after Confirm)
8. `Confirm_And_Cancel_Apply_Status_Transitions` — ✅ brief items 7+8 (Submit + Cancel)
9. `List_And_Detail_Are_Tenant_And_Company_Scoped` — ✅ brief item 9 (Tenant + Company scope enforcement)

**Coverage of brief's 12 items**: 8/12 covered by existing tests. The 4 missing are permission / HTTP-level (brief items 9-12) and will be covered by the G3-R2A runtime evidence script.

---

## 2. The actual G3-R2A failure point (discovered during audit)

While the **SalesOrder backend + frontend + tests** are substantially complete, the **runtime frontend-to-API integration has a critical gap**: the `SALES_OPERATOR` (per the G3-R1C boundary contract) has ONLY 2 perms — `sales.order.read` + `sales.order.manage`. The SalesOrderList and SalesOrderEdit pages import the **MDM** read APIs for dropdown population:

```vue
// apps/web/src/views/sales-order/SalesOrderList.vue
import { listBusinessPartners } from '../../api/mdm/business-partner';

// apps/web/src/views/sales-order/SalesOrderEdit.vue
import { listBusinessPartners } from '../../api/mdm/business-partner';
import { listItems } from '../../api/mdm/item';
import { listAllUomsActiveOnly } from '../../api/mdm/uom';
```

**Live verification on port 5001 (G3-R1E new-code API, fresh build, dev DB):**
- `GET /api/v1/mdm/business-partners?pageSize=3&role=1` (as g3r1c_sales_operator) → **403 authorization_forbidden** (correct per G3-R1C, but breaks the UI)
- `GET /api/v1/mdm/items?pageSize=3` (as g3r1c_sales_operator) → **403** (same)
- `GET /api/v1/mdm/uoms?pageSize=3` (as g3r1c_sales_operator) → **403** (same)
- `GET /api/v1/sales/orders?pageSize=1` (as g3r1c_sales_operator) → **200** (SalesOrder read works)
- `POST /api/v1/sales/orders` (as g3r1c_sales_operator) → **not yet tested but should work** (the service uses MdmDbContext directly)

**This is the brief's WorkItem 6 acceptance gap**: even though SalesOrder is "real API", the SalesOrder page cannot be USED by the SALES_OPERATOR role because the dropdowns are blocked.

---

## 3. Resolution strategy (per brief's hard limits)

Per the G3-R2A brief's hard limits:
- ❌ **No changing the Identity permission model** ("不改 Identity 权限模型")
- ❌ **No changing the ERP_SYSTEM_ADMIN boundary** (locked by 9 unit tests in `ErpSystemAdminPackBoundaryFacts`)
- ❌ **No changing the existing role packs** (InitialAdminRolePacks = [Mdm, Sales, Employee]; each pack has its own perms only)

**The only allowed path: add SalesOrder-scoped context facades** that proxy the same MDM data but require `SalesPolicies.SalesOrderRead` (which SALES_OPERATOR has). This:
1. Preserves the G3-R1C boundary contract (SALES_OPERATOR's pack still has only sales.* perms; ERP_SYSTEM_ADMIN still has only identity.* perms; MDM_OPERATOR still has only mdm.* perms)
2. Lets SALES_OPERATOR populate the dropdowns (via the new context facade)
3. Doesn't require a new identity endpoint
4. Doesn't change the existing mdm.* services (they still require MdmPolicies.XRead; the new facade is a thin wrapper)

### 3.1 Facade endpoints to add (under `/api/v1/sales/orders/context/*`)

| Endpoint | Requires | Returns | Purpose |
|---|---|---|---|
| `GET /api/v1/sales/orders/context/customers?keyword=...&pageSize=...&page=...` | `SalesOrderRead` | PagedResult<BusinessPartnerListItem> | Customer dropdown |
| `GET /api/v1/sales/orders/context/items?keyword=...&pageSize=...&page=...` | `SalesOrderRead` | PagedResult<ItemListItem> | Item dropdown |
| `GET /api/v1/sales/orders/context/uoms?pageSize=200&status=1` | `SalesOrderRead` | PagedResult<UomListItem> | UOM dropdown (per-item) |
| `GET /api/v1/sales/orders/context/warehouses?pageSize=200` | `SalesOrderRead` | PagedResult<WarehouseListItem> | Warehouse dropdown |
| `GET /api/v1/sales/orders/context/payment-methods?pageSize=200` | `SalesOrderRead` | PagedResult<PaymentMethodItem> | PaymentMethod dropdown |

All 5 facades:
- Internally call the existing `IMdmService` / `IMdmMasterData002Services` / `IMdmDictionaryService` (read-only)
- Return the **same** DTOs that the standard MDM endpoints return (UI sees a single shape)
- Require only `SalesOrderRead` (which SALES_OPERATOR has)
- Read-only (no POST/PUT/DELETE)
- Tenant + Company scoped (via ICurrentTenant + ICurrentCompany)

### 3.2 Frontend changes

`SalesOrderList.vue` and `SalesOrderEdit.vue` change:
- `import { listBusinessPartners } from '../../api/mdm/business-partner'` → `import { listSalesOrderCustomers } from '../../api/sales-order-context'`
- Same shape for items, uoms, warehouses, paymentMethods

---

## 4. Question-by-question (from the brief)

| Q | Answer |
|---|---|
| 1. 当前 SalesOrder 后端是否存在? | **YES** — `modules/sales/GuliERP.Sales.{Application,Domain,Infrastructure}` (3 projects) + `SalesOrderService` + 1 EF migration + `SalesDbContext`. |
| 2. 当前 SalesOrder API 是否存在? | **YES** — 6 endpoints at `/api/v1/sales/orders*` registered in `Program.cs:320`. |
| 3. 当前 SalesOrder 前端是否存在? | **YES** — 3 pages (`List` / `Edit` / `Detail`) + 1 Pinia store + 1 API client. |
| 4. 当前是否仍使用 mock? | **PARTIAL** — the SalesOrder page itself uses real API. The legacy `mock/sales-order.ts` is still in the repo but only consumed by `LookupDialog.vue` (out of scope per G3-R1D §5). |
| 5. 当前失败点是什么? | **The SALES_OPERATOR cannot read MDM (BusinessPartner / Item / UOM / Warehouse / PaymentMethod)** because the G3-R1C boundary locks SALES_OPERATOR to sales.* perms only. The SalesOrderList + Edit pages import MDM read APIs, so dropdowns are blocked with 403. |
| 6. 哪些代码可复用? | All 9 unit tests, the entire `SalesOrderService` + `SalesOrder`/`SalesOrderLine` entities, the API client, the 3 pages, the Pinia store, the 6 existing endpoints. |
| 7. 哪些代码必须废弃? | Nothing. The existing backend is correct; only the frontend's import of MDM APIs is wrong (should use the new context facade). |
| 8. 本阶段最小闭环模型? | **6 endpoints** (existing) **+ 5 context facade endpoints** (new) = **11 endpoints total**. Frontend uses 6 for CRUD + 5 for dropdown population. All in 1 page group `/api/v1/sales/orders` (+ `/context` subgroup). |
| 9. 本阶段不做事项? | Per brief: no inventory, no AR, no delivery, no invoice, no full approval flow, no complex pricing, no credit limit, no multi-currency, no complex tax. We have: `DiscountRate + TaxRate` per-line (already in the entity, just default 0 for V1), `CurrencyCode = "CNY"` (single-currency, hard-coded default, can be made configurable later), `Status ∈ {Draft, Confirmed, Cancelled}` (no Submitted; Confirm = Submitted). |
| 10. 是否需要 DB migration? | **NO** — the SALES001 migration `20260822130000_SALES001_RealSalesOrderVerticalSlice` is already applied. The 5 new facade endpoints don't need new tables (they read from existing mdm.* tables). |

---

## 5. Implementation plan

| # | Task | Boundary commit |
|---|---|---|
| 1 | Add `ISalesOrderContextService` (5 list methods) + impl + 5 facade endpoints + `Program.cs` registration + new policy `SalesOrderContextRead` (= `SalesOrderRead` reuse) | `feat(sales): add sales order context facade` |
| 2 | Add 5 tests in `tests/GuliERP.Sales.Tests/SalesOrderContextServiceFacts.cs` (or extend `SalesOrderServiceFacts.cs`) covering: SALES_OPERATOR 200, MDM_OPERATOR 403, anonymous 401 | `test(sales): cover sales order context facade permissions` |
| 3 | Update `apps/web/src/api/sales-order-context.ts` (NEW) + `apps/web/src/views/sales-order/SalesOrderList.vue` + `SalesOrderEdit.vue` to use the new endpoints instead of MDM APIs | `feat(web): wire sales order context facade` |
| 4 | `tools/dev/g3-r2a-salesorder-runtime-evidence.ps1` (G3-R2A WorkItem 6 — 6-endpoint CRUD + 5-facade lookup + 4 roles + 1 anonymous + 1 cancel cleanup) | `chore(dev): add G3 R2A sales order runtime evidence` |
| 5 | `tools/dev/g3-r2a-salesorder-web-evidence.ps1` (G3-R2A WorkItem 7 — Vite route check + no-mock verification) | `chore(dev): add G3 R2A sales order web evidence` |
| 6 | `docs/verification/G3_R2A_SALESORDER_RUNTIME_REBUILD_REPORT.md` (full report) | `docs(verification): add G3 R2A sales order runtime report` |

(Plus the `docs(verification): add G3 R2A sales order audit` commit which is THIS file.)

Total: 7 boundary commits. The brief suggested 6 (`audit + feat-api + test + feat-web + evidence + report`). I'm splitting `feat-api` into `feat(sales): context facade` (1 commit) and `test(sales): cover facade` (1 commit), and `chore(dev): evidence` is split into `runtime-evidence` + `web-evidence`. Net 7 commits.

---

## 6. Open issues for the G3-R2A implementation

| ID | Severity | Issue | Resolution |
|---|---|---|---|
| N-1 | high | SALES_OPERATOR cannot read MDM | **fix via 5 context facade endpoints** (this audit) |
| N-2 | low | `mock/sales-order.ts` is still in the repo (only consumed by LookupDialog) | leave in place per G3-R1D §5 (out of scope) |
| N-3 | low | Currency is hard-coded `"CNY"` in `SalesOrderService.CreateDraftAsync` | OK for V1 (per brief "不做多币种结算") |
| N-4 | low | `DiscountRate` and `TaxRate` per-line | OK for V1 (entity has the fields; UI can set them to 0; V2 can populate) |
| N-5 | low | No DELETE endpoint (V1 only has Draft → Confirmed, Draft → Cancelled) | OK for V1 (per brief "不做复杂审批流") |

---

## 7. Summary

SalesOrder is **substantially implemented already**:
- Backend: 3 projects, 6 endpoints, full CRUD, real MDM integration, real NumberingRule integration, 1 EF migration applied, 9/9 unit tests pass
- Frontend: 3 pages, 1 store, 1 API client, NO mock usage in any SalesOrder page

**The only G3-R2A failure**: the frontend pages import MDM read APIs that the SALES_OPERATOR role cannot access (per the G3-R1C boundary contract). The fix is 5 new SalesOrder-scoped context facade endpoints that proxy the same MDM data but require `SalesOrderRead` instead of `MdmPolicies.XRead`. The boundary contract is preserved (no Identity model change); the G3-R1C unit tests for ERP_SYSTEM_ADMIN are unaffected.

The G3-R2A scope is now clear and bounded: 6 boundary commits + this audit doc + 1 final report. No DB migration. No new entity. No new permission.
