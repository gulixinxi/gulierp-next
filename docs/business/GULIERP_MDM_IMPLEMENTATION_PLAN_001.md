# GULIERP_MDM_IMPLEMENTATION_PLAN_001

> Goal: design the implementation path that closes the V1
> master-data gap (per
> `GULIERP_MDM_COMPLETION_GAP_ANALYSIS_001`). P0 closes the
> contract enforcement and adds the Employee write surface; P1
> scopes the Brand / Price / Tax / PaymentTerm items as
> future design goals (NOT field additions to existing V1
> entities). This is **a design document** — no code, no
> entity, no migration, no API, no Vue page changes ship with
> this PR. The WorkItems it names open their own design
> goals.
> Authority: `docs/governance/META_GULI_GOVERNANCE_V1.md` +
> `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` +
> `docs/governance/GOAL_REGISTRY.md` +
> `docs/business/GULIERP_EXISTING_BUSINESS_ASSET_AUDIT.md` +
> `docs/business/GULIERP_MASTER_DATA_MODEL_V1.md` +
> `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md` +
> `docs/business/GULIERP_MDM_COMPLETION_GAP_ANALYSIS_001.md` +
> `docs/business/GULIERP_BUSINESS_ROADMAP_001.md`.

Date: 2026-08-24
Status: **MDM_IMPLEMENTATION_PLAN_001_FROZEN**

---

## 1. Scope

This plan covers **how to close the gaps** identified by
`GULIERP_MDM_COMPLETION_GAP_ANALYSIS_001`. It does **not** ship
code. The WorkItems it names open their own design goals in
`docs/governance/GOAL_REGISTRY.md` and each one ships its own
audit + design + implementation + test + report cycle.

### 1.1 In scope

- The 2 P0 gaps (Code validation pipeline; Employee write
  surface).
- The 4 P1 items the Brief calls out (Brand / Price / Tax /
  PaymentTerm), scoped as new design goals.
- Sequencing, dependencies, and risk per WorkItem.

### 1.2 Out of scope

- SalesOrder documents (explicit brief exclusion). The SalesOrder
  state machine and snapshot pattern are not touched here.
- The 11 P2 gaps (Barcode, multi-address, multi-contact, internal
  reference number, bank account, etc.) — deferred to their
  own design goals per the V1 + V2 roadmap.
- The 1 P3 gap (universal workflow engine) — V3+ at the
  earliest.
- The auto-coding engine (V1 manual; V2/V3) — recorded as P2.

---

## 2. Sequencing principles

The plan is built on six sequencing principles, derived from
`GULIERP_BUSINESS_ROADMAP_001` §2 and the V1 frozen contract:

1. **Code first, then data.** The frozen V1 Code Rule Standard
   is not enforced today; every new entity that ships a code
   field inherits the same risk. The code validation pipeline
   is the **highest-leverage fix** — it protects every entity
   simultaneously. P0-1 lands first.
2. **V1 entities first, V1.5 new entities second.** P0-1 and
   P0-2 land the missing V1 surface. P1-1..P1-4 (Brand / Price /
   Tax / PaymentTerm) are V1.5+ new entities / new fields on
   documents — they open their own design goals.
3. **One design goal per WorkItem.** Every row in §3 is a
   separate design goal with its own contract update +
   implementation + test + report. No WorkItem bundles two
   unrelated changes.
4. **No per-tenant customization, no flexible columns, no
   workflow engine.** Re-stated from
   `GULIERP_MODULE_INDEPENDENCE_RULE.md`. P1 items respect
   this — Brand / Price / Tax / PaymentTerm are typed fields
   or new entities, never "spare columns."
5. **No V1 contract relaxation.** If a P1 item seems to need
   a V1 entity change, the answer is **add a new entity**, not
   modify the V1 entity.
6. **Page-theme audit on every new page.** The new
   `EmployeeList.vue` (P0-2) must use the shared
   `mdm-page.css` from
   `GULIERP_PAGE_THEME_AUDIT_001` Phase 1.

---

## 3. WorkItem matrix

| #      | WorkItem                                     | Priority | Effort   | Depends on       | Opens design goal                | Closes gap from GAP_ANALYSIS §5 |
|--------|----------------------------------------------|----------|----------|------------------|----------------------------------|---------------------------------|
| P0-1   | Code validation pipeline (4 steps)         | **P0**   | 2–3 d    | none             | `GULIERP_MDM_001_CODE_PIPELINE`  | Gap #1                          |
| P0-2a  | Employee write service + DTOs               | **P0**   | 2 d      | P0-1 (shared validation) | `GULIERP_HR_001_EMPLOYEE_WRITE_V1` | Gap #2 (part 1)             |
| P0-2b  | Employee Vue page + theme audit             | **P0**   | 1–2 d    | P0-2a            | (same as P0-2a)                  | Gap #2 (part 2)             |
| P0-2c  | Employee write tests (unit + integration)   | **P0**   | 1 d      | P0-2a / P0-2b    | (same as P0-2a)                  | Gap #2 (part 3)             |
| P1-1   | TaxScheme + TaxRate entity (V1.5)           | P1       | 3–4 d    | P0-1 (shared validation) | `GULIERP_TAX_001_V15`        | Gap #13                       |
| P1-2   | Brand on Item (or as separate Brand entity) | P1       | 2–3 d    | P0-1             | `GULIERP_BRAND_001_V15`          | Gap #14                       |
| P1-3   | Price on document line (no Item attribute)  | P1       | 2 d      | none (V1.5 doc)   | `GULIERP_SALES_005_PRICING_V15` (or `GULIERP_PURCHASE_002_PRICING_V15`) | Gap #7 (P1 doc)   |
| P1-4   | PaymentTerm entity (V1.5 if document needs it; else V2+ Finance) | P1 | 2–3 d | P0-1 | `GULIERP_PAYMENT_001_V15` (or defer) | Gap #15                  |

> **Sequencing note:** P0-1 must ship BEFORE P0-2a (the Employee
> write service uses the same code validation pipeline). P0-2a
> can ship independently of P0-2b/P0-2c if the API surface is
> ready and the page is small. P1-1..P1-4 are independent of
> P0-2 but each needs P0-1 (shared code pipeline).

---

## 4. P0-1 — Code validation pipeline (4 steps)

### 4.1 Goal

`GULIERP_MDM_001_CODE_PIPELINE` — bring the existing
`MdmService` / `MdmMasterData002Services` in line with the
frozen `GULIERP_CODE_RULE_STANDARD_V1` §8.

### 4.2 Scope (changes to ship)

- **Step 1 (format regex):** in `MdmService.CreateXxxAsync`
  and `MdmService.UpdateXxxAsync`, validate the code
  (or the Code-related field) against
  `^[A-Z][A-Z0-9_]{1,39}$`. The Create path also requires
  length 2..40 (per the spec; the regex captures
  `[A-Z][A-Z0-9_]{1,39}` which is exactly length 2..40).
- **Step 2 (reserved-name check):** add a `ReservedMasterDataCodes`
  static set on the Application layer containing
  `SYSTEM / SYS / RESERVED / EMP-SYSTEM / WH-DEFAULT /
  LOC-RECEIVING / LOC-SHIPPING / ROLE_PLATFORM_ADMIN /
  ROLE_TENANT_ADMIN / ROLE_COMPANY_ADMIN / ROLE_NORMAL_USER`.
  The Create / Update path rejects any code in this set
  with a new `MdmErrorCode.ReservedCode`.
- **Step 4 (no-document-number pattern):** add a regex
  `^(?!\d{8})(?!SO[-_]|PO[-_]|GR[-_]|GI[-_]|TR[-_]|SI[-_]|PI[-_]|MO[-_]|QI[-_]).*`
  OR equivalent positive checks. The Create / Update path
  rejects any code matching the document-number pattern
  (e.g. `20240101`, `SO-001`, `PO_ABC_001`) with a new
  `MdmErrorCode.ResemblesDocumentNumber`.
- **Step 3 (uniqueness):** already implemented at the DB
  unique index + service check. No change.
- **Canonicalization (existing):** trim + uppercase, applied
  BEFORE the 4-step validation. No change to order.
- **MdmErrorCodes.cs:** add the two new error codes
  (`ReservedCode`, `ResemblesDocumentNumber`).
- **MdmValidationException.cs:** no change (it carries the
  MdmErrorCode).
- **EF migrations:** none. The change is service-only.
- **Entity fields:** no change. The validation lives in the
  App service, not the entity.
- **Vue pages:** the form drawer already trims the input; the
  server returns a 400 with the new MdmErrorCode. The
  page-level error message is updated to show the operator
  the specific reason ("reserved" or "looks like a document
  number").
- **No DB migration** (no schema change; no data backfill
  needed; no grandfathered data fix).

### 4.3 Tests (this WorkItem)

- **Unit:** in `MdmValidationTests.cs` (new file or
  `MdmEntityContractTests.cs`), one test per reserved code ×
  per entity. One test per document-number pattern per
  entity. One test per format violation (lowercase, leading
  digit, double underscore, too long, too short, whitespace).
- **Integration:** in `MdmItemFacts` / `MdmUomFacts` /
  `MdmBusinessPartnerWarehouseLocationFacts`, one happy path
  test confirming the new validations are wired.
- **Total new tests:** ~30 (5 format × 6 entities + 3
  reserved + 3 document-number patterns + integration).
- **Coverage impact:** `Mdm.Tests` goes from 65/67 to
  ~95/97. The 2 inherited flaky path tests remain.

### 4.4 Risk

- **Grandfather risk:** existing tenants may have codes
  that violate the new rules. **Mitigation:** the validation
  fires on Create / Update only, not on read. Existing data
  is untouched. A future "cleanup migration" can deprecate
  bad codes; that is its own WorkItem.
- **Operator pushback:** an operator who has been using
  `20240101` for a customer will see the create / update
  fail. **Mitigation:** a one-time Tenant rollout
  communication + the seed data is updated to use spec-
  compliant codes.

### 4.5 Out of scope

- The auto-coding engine (V2/V3). P0-1 is the manual
  validation pipeline; auto-coding is its own design goal.
- Internal reference number (V2+). P0-1 is the master-data
  code; document-internal reference is a different field.

---

## 5. P0-2 — Employee write surface

### 5.1 Goal

`GULIERP_HR_001_EMPLOYEE_WRITE_V1` — bring the existing
Identity Employee entity up to the V1 master-data contract
(read + write + Vue page + tests). Three sub-WorkItems
(P0-2a / P0-2b / P0-2c) ship in one design goal.

### 5.2 P0-2a — Service + DTOs

- **Service interface:** add to
  `modules/identity/GuliERP.Identity.Application/Employee/`
  (new sub-folder). The interface
  `IEmployeeWriteService` exposes:
  - `Task<EmployeeDto> CreateAsync(CreateEmployeeRequest, ct)`.
  - `Task<EmployeeDto?> UpdateAsync(long id, UpdateEmployeeRequest, ct)`.
  - `Task SetStatusAsync(long id, EmployeeStatus, int expectedConcurrency, ct)`.
  - `Task<EmployeeDto?> GetByIdAsync(long id, ct)` (the
    write side uses the same DTO as the read side).
  - `Task<PagedResult<EmployeeDto>> ListByCompanyAsync(long companyId, long? departmentId, string? keyword, EmployeeStatus?, int page, int pageSize, ct)`
    (the read side migrates from the directory service to
    the write service for unified pagination).
- **DTOs:** `EmployeeDto(Id, TenantId, CompanyId, DepartmentId?,
  UserId?, EmployeeNo, Name, Email?, Phone?, Status,
  CreatedAt, CreatedBy?, ModifiedAt, ModifiedBy?, ConcurrencyVersion)`.
  Note: the existing `EmployeeDirectoryEntryDto` is
  a subset; the new DTO is the full V1 contract. The
  directory service keeps the entry DTO for backward
  compatibility with the existing read path.
- **Validation pipeline:** the new service uses the same
  4-step code pipeline as P0-1 (via a shared
  `IMasterDataCodeValidator` service in the Foundation
  module). EmployeeNo is Company-scoped unique.
- **Tenant + Company scope:** the service applies
  `Where(e => e.TenantId == currentTenant.Id && e.CompanyId == currentCompany.Id)`
  on every read / write. Cross-Company access is denied
  (matches BP / Warehouse pattern).
- **EF migration:** none. The Employee entity already has
  the V1 fields. The DTOs are new. The service is new.
- **No DB migration. No data backfill.** The bootstrap-admin
  employee (seed) is the only existing row; it stays.

### 5.3 P0-2b — Vue page

- **New file:** `apps/web/src/views/identity/EmployeeList.vue`
  (Identity is the owning module, not MDM).
  - Pattern: same as the 6 MDM list pages
    (`MdmListToolbar` / `MdmStatusBadge` / `MdmFormDrawer` /
    `MdmDetailDrawer` / `MdmPagination` / `MdmEmptyState` /
    `MdmTableRowActions`).
  - Filters: 部门 (`departmentId`), 状态 (`status`), 关键字
    (`keyword` for EmployeeNo / Name / Email / Phone).
  - Form drawer: EmployeeNo (read-only on update), Name,
    Email, Phone, DepartmentId, UserId. Status is
    set via the row action cluster.
  - Detail drawer: full V1 contract via `el-descriptions`.
- **Page-theme-audit compliance:** the new page uses the
  shared `design-system/components/mdm-page.css` (5
  common rules) + 0 page-specific accents.
- **Side effect:** add the Employee module to the
  `shellNavigation` (`apps/web/src/layout/navigation.ts`)
  under the "系统" (System) module, not "主数据" (Master
  Data). Employee is a People entity, not a Business
  vocabulary.
- **Route:** `/system/employees` (mounted under the System
  module rail). The route's meta is empty; the page filters
  by `currentCompanyId` (from the auth store).

### 5.4 P0-2c — Tests

- **Unit (Identity.Tests):** create / update / setStatus
  per scenario + 4-step code pipeline per EmployeeNo +
  scope guard + status lifecycle.
- **Integration (Identity.IntegrationTests):** end-to-end
  create-list-update-setStatus, cross-Company access denied,
  EmployeeNo uniqueness within Company.
- **Front-end:** no front-end test surface today
  (GULIERP_PAGE_THEME_AUDIT_001 ships the design-system
  CSS, not the page tests). The new Employee page does
  not introduce a front-end test framework.

### 5.5 Risk

- **Auth vs HR separation:** the Employee entity has an
  optional `UserId` link. The new write service must NOT
  create / modify the `GuliErpUser` row. Employee create
  is a People action; User create is an Auth action. The
  link is set by an admin manually in a future
  "system settings" page (V1.5+). For P0-2, `UserId` is
  read-only in the form drawer; only an admin DB script
  can set it.
- **Bootstrap-admin employee:** the seed row (created by
  the Identity-bootstrap migration) is the row the new
  service updates if a real admin does it. The seed
  itself is untouched.
- **Privacy:** the new DTO includes `Email` and `Phone`.
  The form should not log these. Per the V1 frozen
  contract, audit fields store `CreatedBy / ModifiedBy`
  (user id, not name); the DTO body never includes the
  password or any auth secret.

### 5.6 Out of scope

- HR depth (employment contract, position, manager-of,
  hire date, payroll). V2+ per the V1 model.
- Contact as a separate first-class entity. V1 keeps
  Contact as a single triple on BusinessPartner.
- The User ↔ Employee "system settings" page (V1.5+).

---

## 6. P1 — Brand / Price / Tax / PaymentTerm (4 design goals)

The Brief lists these as P1. The Brief is correct that they
belong in V1.5+; the Brief is **explicit** that this Phase 1
is audit + design, NOT implementation. The four P1 items each
open a **separate design goal** in a future PR.

### 6.1 P1-1 — TaxScheme + TaxRate (new entities)

- **Why:** the V1 model has `BusinessPartner.TaxNumber` (free
  text, satisfied). V1.5+ documents (PurchaseOrder /
  SalesOrder) need a tax rate on each line. The rate is
  NOT on the line (per the V1 model: "document-line pricing
  lives on the document"); the rate is on a
  `TaxScheme → TaxRate` table.
- **Entities (new):**
  - `TaxScheme(Id, TenantId, Code, Name, CountryCode, Status,
    Audit, ConcurrencyVersion)` — Tenant-scoped
    (`(TenantId, Code)` unique).
  - `TaxRate(Id, TenantId, TaxSchemeId, Code, Rate, EffectiveFrom,
    EffectiveTo?, Status, Audit, ConcurrencyVersion)` —
    Tenant-scoped (`(TaxSchemeId, Code)` unique, or
    `(TaxSchemeId, EffectiveFrom)` unique — TBD in the
    design goal).
- **Use:** `PurchaseOrderLine.TaxSchemeId + TaxRateId`
  (FK). `SalesOrderLine` mirrors.
- **Trigger:** the first PurchaseOrder design goal
  (V1.5-W2 in the roadmap).
- **Out of scope:** multi-jurisdiction tax engines (US
  sales tax, EU VAT MOSS, China 增值税 + 附加税). V1.5 ships
  one rate per line. V2+ adds cascading + reverse charge.

### 6.2 P1-2 — Brand (Item attribute or separate entity)

- **Why:** the V1 model does NOT list Brand. The Brief puts
  it in P1. Brand is a manufacturing / retail concern;
  the V1 model defers it implicitly (no field on Item, no
  entity). The Brief overrides the V1 model by adding
  Brand to P1.
- **Decision (open until the design goal):** Brand on
  Item as a free-text `Brand?` field, OR a separate
  `Brand(Id, TenantId, Code, Name, Status, Audit,
  ConcurrencyVersion)` entity.
- **Recommendation:** separate `Brand` entity (consistency
  with `Uom` / `ItemCategory` / `BusinessPartner`). The
  reference is `Item.BrandId: long?` (nullable; NULL = no
  brand).
- **Trigger:** the first V1.5 document that needs Brand
  (likely `SalesOrder` if the operator sells multiple
  brands, or `Item` master data cleanup).
- **Out of scope:** brand-specific pricing (V2+). Brand in
  V1.5 is descriptive only.

### 6.3 P1-3 — Price on document line

- **Why:** the V1 model says "document-line pricing lives on
  the document, not on Item" (§4.1 explicit exclusion).
  The Brief lists Price in P1. The Brief's interpretation
  is the V1 model: Price is NOT on Item. Price is on
  `PurchaseOrderLine.UnitPrice` and
  `SalesOrderLine.UnitPrice` (and a `TaxInclusive` flag +
  `Discount` percentage).
- **Decision (open until the design goal):** Price is
  captured at order entry, NOT looked up from an Item
  attribute. There is NO `Item.UnitPrice` in V1.5.
- **Trigger:** the first document that records a price
  (`PurchaseOrder` V1.5-W2 or `SalesOrder` V1.5-W1).
- **Out of scope:** price lists / customer-specific pricing /
  volume discount / promotion engine. V2+ (per the V1
  model §4.1).

### 6.4 P1-4 — PaymentTerm entity (or V2+)

- **Why:** the V1 model says "Default payment term (V2+,
  Finance)." The Brief lists PaymentTerm in P1. The Brief
  overrides the V1 model by pulling PaymentTerm forward
  to V1.5.
- **Decision (open until the design goal):** the V1.5
  documents (`PurchaseOrder` / `SalesOrder`) need
  payment-term-on-document. If a `PaymentTerm` entity
  exists, the document references it (`Net 30` /
  `Net 60` / `Cash on Delivery`). If not, the document
  carries a free-text term.
- **Recommendation:** ship the entity in V1.5. It's a
  small entity (Code, Name, DaysUntilDue, Status, Audit,
  ConcurrencyVersion). Reference it from the document.
- **Trigger:** the first document that needs AR / AP due
  date (PurchaseOrder for AP, SalesOrder for AR).
- **Out of scope:** variable due dates per line, partial
  payments, dunning. V2+ Finance.

---

## 7. Risk and dependency rollup

| Risk / dependency                            | Severity | Mitigation                                    |
|----------------------------------------------|----------|-----------------------------------------------|
| V1 contract relaxation by P1 items          | HIGH     | P1 items add new entities / new fields, NOT relax V1 |
| Existing bad data fails the new pipeline     | MEDIUM   | Validation fires on Create / Update only; existing data is grandfathered |
| Bootstrap-admin Employee is the only row     | LOW      | The seed row is the target of the new write service; the seed is untouched |
| Page-theme-audit on EmployeeList.vue         | LOW      | Reuse `mdm-page.css`; no new accents         |
| Brief mentions "P1 Brand / Price / Tax / PaymentTerm" as fields | MEDIUM | The plan scopes them as new entities / new fields on documents, NOT as field additions to existing V1 entities |
| V1.5-W1 (SalesOrder completion) is the next code WorkItem | LOW | P0-1 + P0-2 do not block V1.5-W1; V1.5-W1 can land in parallel |

---

## 8. Sequencing diagram (textual)

```
T0 (now)        ── Phase 1 audit + design ── [GULIERP_MDM_COMPLETION_001] (this PR)
                          │
                          ▼
T0 + 2–3 d      P0-1: GULIERP_MDM_001_CODE_PIPELINE
                (4-step validation in App service, ~30 unit tests, no migration)
                          │
        ┌─────────────────┼─────────────────┐
        ▼                 ▼                 ▼
T0 + 5 d    P0-2a: HR write service    P1-1: TaxScheme/TaxRate     (independent)
                (Employee CRUD, DTOs,                 (new entities, referenced from
                 scope guards, 4-step code             PurchaseOrder V1.5-W2)
                 pipeline uses P0-1)                   (depends on P0-1)
        │
        ▼
T0 + 7 d    P0-2b: EmployeeList.vue + page-theme audit
                (uses shared mdm-page.css)
        │
        ▼
T0 + 8 d    P0-2c: Employee write tests (unit + integration)
                │
                ▼
T0 + 8 d    ── P0 milestone COMPLETE ──
                          │
                          ▼
T0 + ~2 w    V1.5-W1: SalesOrder completion (in parallel with P1 items)
                          │
                          ▼
T0 + ~3 w    P1-2 / P1-3 / P1-4 design goals (each is its own WorkItem)
                ├── Brand (new entity or Item attribute)
                ├── Price on document line (SalesOrderLine / PurchaseOrderLine)
                └── PaymentTerm (new entity, referenced from documents)
                          │
                          ▼
T0 + ~6 w    ── V1.5 milestone COMPLETE ──
                (P0 closed + P1 items scoped and at least partially shipped)
                          │
                          ▼
T0 + ~8 w    V1.5-W2: PurchaseOrder vertical slice (new design goal)
                          │
                          ▼
T0 + ~10 w   V1.5-W3: GoodsReceipt (new design goal)
                          │
                          ▼
T0 + ~12 w   ── V1.5 PHASE 1 COMPLETE (per ROADMAP) ──
```

> **Total elapsed time to "MDM is V1-ready":** ~8 working days
> (P0-1 + P0-2a + P0-2b + P0-2c, executed sequentially). P1
> items are designed in parallel; their implementation lands
> per their own WorkItem cadence.

---

## 9. Acceptance criteria (Phase 2 — the future code PR)

The Phase 1 design is complete. The future code WorkItem
opens a new design goal. Its acceptance criteria are:

### 9.1 P0-1 acceptance

- The 4-step validation pipeline is wired in the App
  service.
- The 2 new error codes are added to `MdmErrorCodes.cs`.
- The Vue form drawer surfaces the new error messages.
- All existing unit / integration tests still PASS.
- ~30 new unit tests PASS.
- No new DB migration; no entity field change.
- A `GULIERP_MDM_001_CODE_PIPELINE_REPORT.md` closes the goal.

### 9.2 P0-2 acceptance

- The Employee write service is wired.
- The Employee Vue page is shipped; `mdm-page.css` is
  reused; no page-theme-audit drift.
- All existing Identity tests still PASS.
- New unit + integration tests cover create / update /
  setStatus / scope guard / code pipeline.
- No new DB migration; no entity field change (the
  existing Employee entity already has the V1 fields).
- A `GULIERP_HR_001_EMPLOYEE_WRITE_V1_REPORT.md` closes
  the goal.

### 9.3 P1-1..P1-4 acceptance

- Each P1 item opens its own design goal with its own
  acceptance criteria.
- No P1 item adds a field to a V1 frozen entity.
- Each P1 item ships its own audit + report.

---

## 10. One-line summary

Two P0 WorkItems (4-step code validation pipeline + Employee
write surface + page + tests) close the V1 contract gap in
~8 working days. Four P1 WorkItems (TaxScheme / TaxRate,
Brand, Price on document line, PaymentTerm) are scoped as
future design goals — none of them add a field to a V1 frozen
entity; each opens its own goal. The V1 contract stays frozen.
No new DB migration lands with this Phase 1; the P0 changes
are App-service only. The next code milestone is V1.5-W1
SalesOrder completion, which proceeds in parallel.
