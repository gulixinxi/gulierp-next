# G3-R2B PurchaseOrder Current State Audit

**Phase:** G3-R2B PurchaseOrder Runtime Rebuild
**Audit date:** 2026-08-26
**Author:** Mavis (GuliERP-Next main execution Agent)
**Source commit (HEAD at audit time):** `1a9d830 docs(verification): add G3 R2A sales order runtime report`

---

## 1. Pre-flight confirmation

- `git status --short` empty (only `tools/dev/.quarantine/` untracked, gitignored)
- 0 staged, 0 modified
- HEAD = `1a9d830` (G3-R2A report)
- G3-R2A Gate = `G3_R2A_SALESORDER_RUNTIME_REBUILD_VERIFIED`

Ignored items (all .gitignore'd, NOT in commit history):
- `tools/.quarantine/`, `tools/dev/.quarantine/`, `artifacts/`
- `tests/*/TestResults/`, `.runtime-browser-profile/`

---

## 2. CRITICAL: ERP_SYSTEM_ADMIN permission count audit

The G3-R2A report (`1a9d830`) wrote "9 ERP_SYSTEM_ADMIN perms" in its §1.2.
The user explicitly flagged this as inconsistent with the G3-R1C report which said "8 identity administration perms".

**Source of truth: 8 perms** (confirmed by both the unit test and the constants file).

### 2.1 Source 1: `tests/GuliERP.Identity.Tests/ErpSystemAdminPackBoundaryFacts.cs` (line 76)

```csharp
[Fact]
public void SystemAdmin_Pack_Contains_Exactly_8_Identity_Administration_Permissions()
{
    var perms = EnterpriseBusinessRolePacks.SystemAdmin.Permissions;
    Assert.Equal(8, perms.Count);   // ← EXPLICIT ASSERTION
    ...
}
```

### 2.2 Source 2: `modules/identity/GuliERP.Identity.Application/Authorization/GuliErpPermissions.cs` (lines 30-40)

```csharp
public static readonly string[] EnterpriseSystemAdminPermissions =
{
    IdentityOrganizationRead,    // identity.organization.read
    IdentityOrganizationManage,  // identity.organization.manage
    IdentityUserRead,            // identity.user.read
    IdentityUserManage,          // identity.user.manage
    IdentityRoleRead,            // identity.role.read
    IdentityRoleAssign,          // identity.role.assign
    IdentityCompanyRead,         // identity.company.read
    IdentityCompanySwitch,       // identity.company.switch
};   // ← 8 entries
```

### 2.3 Source 3: `EnterpriseBusinessRolePacks.SystemAdmin` (line 73-80, post G3-R1C feat)

The new `SystemAdmin` record (added in G3-R1C feat) **references** the frozen `EnterpriseSystemAdminPermissions` array:

```csharp
public static readonly EnterpriseBusinessRolePack SystemAdmin = new(
    SystemAdminRoleCode,
    SystemAdminRoleName,
    "...",
    GuliErpPermissions.EnterpriseSystemAdminPermissions);   // ← references 8-element array
```

### 2.4 Conclusion

| Source | Count | Notes |
|---|---|---|
| `GuliErpPermissions.EnterpriseSystemAdminPermissions` (constant) | **8** | primary |
| `EnterpriseBusinessRolePacks.SystemAdmin.Permissions` (record) | **8** | references the constant |
| `ErpSystemAdminPackBoundaryFacts.SystemAdmin_Pack_Contains_Exactly_8_Identity_Administration_Permissions` (test) | **8** | explicit `Assert.Equal(8, ...)` |
| `ErpSystemAdminPackBoundaryFacts` file | 9 test methods total | unrelated to perm count |
| G3-R1C report (`82970e1`) | **8** | correct |
| G3-R1E report (`592e9da`) | — | not mentioned |
| G3-R2A report (`1a9d830`) | 9 | **MISTAKE** — likely confused the 9 unit test count with the 8 perm count |

**CORRECTED report language**: ERP_SYSTEM_ADMIN has exactly 8 identity administration permissions. G3-R2A's "9" was a documentation error, not a source-code change.

The 9 G3-R1C unit tests in `ErpSystemAdminPackBoundaryFacts` are 9 test METHODS (8 boundary checks + 1 discovery check = 9), not 9 perms. The perm count is locked at **8** by `Assert.Equal(8, perms.Count)` in `SystemAdmin_Pack_Contains_Exactly_8_Identity_Administration_Permissions`.

**G3-R2B will NOT change the perm count, the pack, or any boundary test.** Only ADD a new pack for ERP_PURCH_OPERATOR with its own 2 perms.

---

## 3. Question-by-question (from the brief)

| Q | Answer |
|---|---|
| 1. 当前是否已有 Purchase 模块? | **YES** (scaffold only) — `modules/purchase/` exists with only `README.md` ("G0 status: no formal purchase order page or business implementation."). No `.cs` files, no `.csproj`, no test project, no domain/application/infrastructure layers. |
| 2. 当前是否已有 PurchaseOrder 后端代码? | **NO** — modules/purchase is empty (just README). |
| 3. 当前是否已有 PurchaseOrder API? | **NO** — no `PurchaseOrderEndpoints.cs`, no `Program.cs` reference, no route group registered. |
| 4. 当前是否已有 PurchaseOrder 前端? | **NO** — `apps/web/src/views/` has no `purchase/` directory; no `api/purchase*.ts`; no `router/purchase.ts`; no `MasterDataWorkbench` or `SalesOrderList` reference. |
| 5. 当前是否已有 purchase 权限? | **NO** — no `purchase.order.read` or `purchase.order.manage` in `GuliErpPermissions.cs`; no `PurchasePolicies` or `PurchasePermissions` class. |
| 6. 是否已有采购单编号规则? | **NO** — `DocumentType.PurchaseOrder` is not declared in the existing `IDocumentNumberService`; would need to be added. The sales path uses `DocumentType.SalesOrder`. |
| 7. 是否已有 Supplier 来源? | **YES (via BusinessPartner)** — `BusinessPartnerRole.Supplier = 2` and `BusinessPartnerRole.Both = Customer | Supplier` are the existing flags. The dev DB has 5 BusinessPartners; the live API confirmed 5 customers exist (some with role=Both). The G3-R2A facade endpoint `/api/v1/sales/orders/context/customers` returns them; we need an analogous `/api/v1/purchase/orders/context/suppliers` that filters to `role >= 2` (Supplier or Both). |
| 8. 哪些 SalesOrder 模式可复用? | **ALL the patterns** (per the G3-R2A report §12):<br/>- 4 dedicated test users (need to add g3r2b_purch_operator)<br/>- context facade pattern (5 endpoints over the requesting module's permission)<br/>- 4-level evidence script pattern<br/>- The 21+ unit test pattern<br/>- The `_run_*_*.ps1` dev-only operational script pattern in `tools/dev/.quarantine/`<br/>- The `ISalesOrderContextService` → `ISalesOrderContextService` interface design<br/>- The `SalesOrderService` business logic (IdempotencyKey, NumberingRule integration, snapshot pattern) is 80% reusable for `PurchaseOrderService` — only the entity, DTOs, and a small validation function (`sales.invalid_uom` → `purchase.invalid_uom`) differ. |
| 9. 本阶段是否需要 migration? | **YES** — no existing `purchase_order` / `purchase_order_line` tables. Will add 1 EF migration (mirror of `20260822130000_SALES001_RealSalesOrderVerticalSlice`). |
| 10. 本阶段不做事项? | Per brief: no inventory receipt, no purchase receiving, no AP, no payment request, no invoice, no supplier pricing, no complex tax, no multi-currency, no approval flow. V1 status set = {Draft, Confirmed, Cancelled} (no Received / Closed / Invoiced / Paid / PartialReceived). |

---

## 4. Boundary contract that G3-R2B must preserve

| Contract | Locked by | Status |
|---|---|---|
| ERP_SYSTEM_ADMIN has 8 identity perms (no mdm/employee/sales/purchase) | `ErpSystemAdminPackBoundaryFacts` (9 tests) | **must remain green** |
| ERP_MDM_OPERATOR has 16 mdm perms (no identity/employee/sales/purchase) | `ErpSystemAdminPackBoundaryFacts.OperatorPacks_Remain_Independent_From_Each_Other` | **must remain green** |
| ERP_SALES_OPERATOR has 2 sales perms (no mdm/employee/purchase) | same | **must remain green** |
| ERP_EMPLOYEE_OPERATOR has 2 employee perms (no mdm/sales/purchase) | same | **must remain green** |
| InitialAdminRolePacks does NOT include SystemAdmin | `InitialAdminRolePacks_Does_Not_Include_SystemAdmin` | **must remain green** (we'll add Purchase but NOT change the SystemAdmin check) |
| No cross-pack permission overlap | `OperatorPacks_Remain_Independent_From_Each_Other` | **must remain green** |

G3-R2B's `ERP_PURCH_OPERATOR` (2 perms: `purchase.order.read` + `purchase.order.manage`) MUST:
1. NOT be in any of the 4 existing packs' permission lists
2. Be in `InitialAdminRolePacks` (consistent with Mdm/Sales/Employee pattern; admin gets 4 operator packs + SystemAdmin = 5 role assignments, replacing the current 3+1=4)
3. The existing test `InitialAdminRolePacks_Does_Not_Include_SystemAdmin` asserts `Assert.Equal(3, codes.Length)` — this MUST be updated to `Assert.Equal(4, codes.Length)` (we are adding a pack to that array)
4. The new `ERP_PURCH_OPERATOR` pack MUST NOT be added to the G3-R1C 8-perm `EnterpriseSystemAdminPermissions` (which would break `SystemAdmin_Pack_Contains_Exactly_8_Identity_Administration_Permissions`)

The existing 9 unit tests in `ErpSystemAdminPackBoundaryFacts` will need **1 minor update** (the count assertion in `InitialAdminRolePacks_Does_Not_Include_SystemAdmin`). All other 8 boundary checks remain unchanged.

---

## 5. Implementation plan (7 boundary commits)

| # | Task | Boundary commit | Notes |
|---|---|---|---|
| 1 | Write this audit doc | `docs(verification): add G3 R2B purchase order audit` | This file |
| 2 | Add 2 purchase permissions + ERP_PURCH_OPERATOR pack + update InitialAdminRolePacks | `feat(identity): add purchase operator permission pack` | + 1 new pack, + 2 new perms; update 1 existing test (count 3→4) |
| 3 | Add new boundary tests for ERP_PURCH_OPERATOR | `test(identity): cover purchase operator permission boundaries` | 5-6 new tests in `PurchaseOperatorPackBoundaryFacts.cs` |
| 4 | Build modules/purchase: 3 csproj + domain + application + infrastructure + 1 EF migration | `feat(purchase): add purchase order persistence model` | Migration: `20260826000000_PURCH001_PurchaseOrderVerticalSlice` |
| 5 | Add ISalesOrderContextService-equivalent for purchase + 6 main + 5 facade endpoints | `feat(purchase): rebuild purchase order runtime API` | reuse SalesOrderService patterns, 80% similar |
| 6 | Add tests for the new service + 14+ boundary cases | `test(purchase): cover purchase order runtime behavior` | mirror of `SalesOrderServiceFacts` + `SalesOrderContextServiceFacts` |
| 7 | 3 frontend pages + 2 API clients + router + navigation | `feat(web): rebuild purchase order runtime pages` | 100% mirror of SalesOrder web wiring |
| 8 | g3-r2b-purchaseorder-runtime-evidence.ps1 + g3-r2b-purchaseorder-web-evidence.ps1 + (optional) g3-r2b-ensure-purch-test-user.ps1 | `chore(dev): add G3 R2B purchase order runtime evidence` | extend G3-R1C ensure-role-test-users script for the new g3r2b_purch_operator user |
| 9 | Final report | `docs(verification): add G3 R2B purchase order runtime report` | mirror of G3-R2A report |

Estimated total: 8 boundary commits + 1 docs commit = **9 commits** (matches the brief's suggested structure).

---

## 6. G3-R2A-pattern-to-reuse summary

| G3-R2A pattern | G3-R2B equivalent | Status |
|---|---|---|
| 6 main endpoints at `/api/v1/sales/orders*` | 6 main endpoints at `/api/v1/purchase/orders*` | NEW (mirror) |
| 5 context facades at `/api/v1/sales/orders/context/*` | 5 context facades at `/api/v1/purchase/orders/context/*` (suppliers replaces customers) | NEW (mirror) |
| `ISalesOrderContextService` (62 lines) | `IPurchaseOrderContextService` (~62 lines) | NEW (mirror) |
| `SalesOrderContextService` (141 lines) | `PurchaseOrderContextService` (~141 lines) | NEW (mirror) |
| 8 context boundary tests in `SalesOrderContextServiceFacts` | 8 context boundary tests in `PurchaseOrderContextServiceFacts` | NEW (mirror) |
| `g3-r2a-salesorder-runtime-evidence.ps1` (386 lines, 49 PASS) | `g3-r2b-purchaseorder-runtime-evidence.ps1` (~390 lines) | NEW (mirror) |
| `g3-r2a-salesorder-web-evidence.ps1` (271 lines, 20 PASS) | `g3-r2b-purchaseorder-web-evidence.ps1` (~275 lines) | NEW (mirror) |
| `sales-order-context.ts` (90 lines) | `purchase-order-context.ts` (~90 lines) | NEW (mirror) |
| `sales-order.ts` (32 lines) | `purchase-order.ts` (~32 lines) | NEW (mirror) |
| 3 frontend pages (List/Edit/Detail) | 3 frontend pages (List/Edit/Detail) | NEW (mirror) |
| `sales.invalid_uom` (line uomId MUST = item.baseUomId) | `purchase.invalid_uom` (same invariant) | REUSE |
| `DocumentType.SalesOrder` (NumberingRule) | `DocumentType.PurchaseOrder` (NEW enum value) | ADD to existing enum |
| `tests/GuliERP.Sales.Tests/` | `tests/GuliERP.Purchase.Tests/` (NEW csproj) | NEW (mirror) |
| `tools/dev/g3-r2a-salesorder-*.ps1` | `tools/dev/g3-r2b-purchaseorder-*.ps1` | NEW (mirror) |

---

## 7. Open questions for WorkItem 2+ (which the implementation will resolve)

| Q | Decision |
|---|---|
| Add `ERP_PURCH_OPERATOR` to `InitialAdminRolePacks`? | **YES** (consistent with Mdm/Sales/Employee; admin should be able to do all 4 operations out of the box) |
| Update the `Assert.Equal(3, codes.Length)` in `InitialAdminRolePacks_Does_Not_Include_SystemAdmin`? | **YES** to 4 (reflect the new ERP_PURCH_OPERATOR pack); the test's INTENT ("SystemAdmin is NOT in this list") is unchanged. |
| New permission codes? | `purchase.order.read` + `purchase.order.manage` (mirror of sales) |
| New role pack code? | `ERP_PURCH_OPERATOR` (mirror of `ERP_SALES_OPERATOR`) |
| New policies? | `PurchasePolicies.PurchaseOrderRead` + `PurchasePolicies.PurchaseOrderManage` (mirror of sales) |
| New NumberingRule document type? | `DocumentType.PurchaseOrder` (NEW enum value, added to `IDocumentNumberService` enum; same pattern as `SalesOrder`) |
| Frontend route? | `/purchase/orders` + `/purchase/orders/new/edit` + `/purchase/orders/{id}/edit` + `/purchase/orders/{id}` (mirror of sales) |
| OrderNo prefix? | `PO-YYYYMMDD-XXXXX` (per brief "优先使用 PO 前缀") |
| Currency for purchase? | Single `CNY` (per brief "不做多币种结算"); same constraint as SalesOrder V1 |
| PurchaseOrder.Status set? | `Draft` / `Confirmed` / `Cancelled` (per brief; no Received/Closed/Invoiced/Paid/PartialReceived in V1) |
| Migration name? | `20260826000000_PURCH001_PurchaseOrderVerticalSlice` (mirror of SALES001; uses G3-R2B date for ordering) |

---

## 8. Summary

PurchaseOrder is **NOT implemented** at all. The brief's G3-R2B work is essentially a from-scratch mirror of the G3-R2A pattern: 8 new files (3 backend + 2 test + 3 frontend + 1 migration) + the `ERP_PURCH_OPERATOR` pack + the 5 context facade endpoints + 2 evidence scripts + 1 final report. The G3-R2A pattern is fully reusable.

The boundary contract to preserve: ERP_SYSTEM_ADMIN stays at **8 perms** (G3-R2A's "9" was a documentation error). 1 existing test will need a count update (3→4 in `InitialAdminRolePacks_Does_Not_Include_SystemAdmin`). The 9 G3-R1C `ErpSystemAdminPackBoundaryFacts` remain otherwise green.

G3-R2B is now ready to proceed. Estimated 8 boundary commits + 1 docs commit.
